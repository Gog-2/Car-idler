using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text moneyText;
    public FloatingTextPool floatingTextPool;
    public NotificationPool notificationPool;
    
    [Header("Locations Setup")]
    public LocationData[] locations;
    public int currentLocIndex = 0;

    [Header("Global Settings")]
    public GameObject carPrefab;
    public TierConfig[] tiers;
    public int carsPerTier = 10;

    [Header("Economy")]
    public double totalMoney;
    
    private float incomeMultiplier = 1f;
    private float speedBoostMultiplier = 1f;
    private Stack<GameObject> pool = new Stack<GameObject>();
    private CancellationTokenSource _cts;

    [System.Serializable]
    public class CarInstance {
        public Transform transform;
        public SpriteRenderer spriteRenderer;
        public int currentWaypointIndex;
        public int tierIndex;
        public int locationIndex;
    }

    void Start() {
        _cts = new CancellationTokenSource();
        StartEconomyLoop(_cts.Token).Forget();
        
        
        foreach (var loc in locations) {
            loc.UpdateCooldown();
            loc.nextSpawnTime = Time.time + loc.currentCooldown;
        }

        BuyCar();
    }
    private void UpdateMoney(double amount) {
        totalMoney += amount;
        if (moneyText != null)
            moneyText.text = $"{totalMoney:F0}$";
        if (floatingTextPool != null && amount > 0)
            floatingTextPool.Show($"+{amount:F0}$");
    }

    public void AddMoney(double amount) {
        UpdateMoney(amount);
    }
    
    public void BuyCar() {
        BuyCarAtLocation(currentLocIndex);
    }

    public void BuyCarAtLocation(int locIndex) {
        LocationData currentLoc = locations[locIndex];

        if (currentLoc.IsFull) {
            Debug.Log("Локация заполнена! Максимум 30 машин.");
            return;
        }

        double cost = 50 * Mathf.Pow(1.2f, currentLoc.carsInLocation.Count);
        if (totalMoney < cost) {
            Debug.Log($"Недостаточно денег! Нужно {cost}$");
            return;
        }

        totalMoney -= cost;
        
        
        int tierIndex = Mathf.Min(currentLoc.carsInLocation.Count / carsPerTier, tiers.Length - 1);
        TierConfig config = tiers[tierIndex];

        GameObject carObj = GetCarFromPool();
        carObj.transform.position = currentLoc.path.GetPoint(0);
        
        Car carComponent = carObj.GetComponent<Car>();
        if (carComponent == null) carComponent = carObj.AddComponent<Car>();
        carComponent.locationIndex = locIndex;
        carComponent.tierIndex = tierIndex;

        SpriteRenderer sr = carObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = config.carSprite;

        CarInstance newCar = new CarInstance {
            transform = carObj.transform,
            spriteRenderer = sr,
            currentWaypointIndex = 0,
            tierIndex = tierIndex,
            locationIndex = locIndex
        };

        currentLoc.carsInLocation.Add(newCar);
        double nextCost = 50 * Mathf.Pow(1.2f, currentLoc.carsInLocation.Count);
        RefreshUpgradeUI(locIndex);
        Debug.Log($"Машина куплена на {currentLoc.locationName} за {cost}$");
    }

    void HandleLootBoxSpawning() {
        foreach (var loc in locations) {
            if (Time.time > loc.nextSpawnTime) {
                SpawnLootBox(loc);
                loc.nextSpawnTime = Time.time + loc.currentCooldown;
            }
        }
    }
    public void RefreshUpgradeUI(int locIndex) {
        var loc = locations[locIndex];
        double speedCost = 100 * Mathf.Pow(1.5f, loc.speedLevel);
        double cooldownCost = 200 * Mathf.Pow(1.8f, loc.cooldownLevel);
        double nextCost = 50 * Mathf.Pow(1.2f, loc.carsInLocation.Count);
        loc.UpdateUpgradeUI(nextCost, speedCost, cooldownCost);
    }

    void SpawnLootBox(LocationData loc) {
        if (loc.lootBoxPrefab == null || loc.path == null || loc.path.PointsCount < 2) return;

        // Выбираем случайный сегмент пути (например, между 3 и 4 точкой)
        int segmentIndex = Random.Range(0, loc.path.PointsCount - 1);
        Vector3 p1 = loc.path.GetPoint(segmentIndex);
        Vector3 p2 = loc.path.GetPoint(segmentIndex + 1);

        // Находим случайную точку на выбранном отрезке
        float t = Random.value;
        Vector3 spawnPos = Vector3.Lerp(p1, p2, t);

        GameObject boxObj = Instantiate(loc.lootBoxPrefab, spawnPos, Quaternion.identity);
        LootBox box = boxObj.GetComponent<LootBox>();
        box.locationIndex = System.Array.IndexOf(locations, loc);
        box.OnBoxBroken += ApplyLootBoxBonus;
    }

    void ApplyLootBoxBonus(int locIndex) {
        int bonusType = Random.Range(0, 3);
        switch (bonusType) {
            case 0:
                totalMoney += totalMoney * 0.1;
                totalMoney = System.Math.Round(totalMoney, 2);
                notificationPool.Show("+10% Баланс!", Color.green, 3f);
                break;
            case 1: // 2x income
                ApplyIncomeBoost(15f).Forget();
                break;
            case 2: // Speed boost
                ApplySpeedBoost(10f).Forget();
                break;
        }
    }

    private async UniTaskVoid ApplyIncomeBoost(float duration) {
        notificationPool.Show("x2 Доход!", Color.yellow, 15f);
        incomeMultiplier = 2f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        incomeMultiplier = 1f;
    }

    private async UniTaskVoid ApplySpeedBoost(float duration) {
        notificationPool.Show("Ускорение!", Color.cyan, 10f);
        speedBoostMultiplier = 1.5f;
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            await UniTask.Yield();
        }
        speedBoostMultiplier = 1f;
    }
    
    public void UpgradeSpeed(int locIndex) {
        double cost = 100 * Mathf.Pow(1.5f, locations[locIndex].speedLevel);
        RefreshUpgradeUI(locIndex);
        if (totalMoney >= cost) {
            totalMoney -= cost;
            locations[locIndex].speedLevel++;
            locations[locIndex].speedMultiplier += 0.1f;
            Debug.Log($"Скорость на {locations[locIndex].locationName} улучшена!");
        }
    }

    public void UpgradeLootBoxCooldown(int locIndex) {
        RefreshUpgradeUI(locIndex);
        double cost = 200 * Mathf.Pow(1.8f, locations[locIndex].cooldownLevel);
        if (totalMoney >= cost) {
            totalMoney -= cost;
            locations[locIndex].cooldownLevel++;
            locations[locIndex].UpdateCooldown();
            Debug.Log($"Кулдаун лутбокса на {locations[locIndex].locationName} уменьшен!");
        }
    }

    void FixedUpdate() {
        HandleLootBoxSpawning();
        foreach (var loc in locations) {
            float localMultiplier = loc.speedMultiplier * speedBoostMultiplier;

            for (int i = 0; i < loc.carsInLocation.Count; i++) {
                var car = loc.carsInLocation[i];
                Vector3 target = loc.path.GetPoint(car.currentWaypointIndex);
                
                float finalSpeed = tiers[car.tierIndex].speed * localMultiplier;
            
                car.transform.position = Vector3.MoveTowards(
                    car.transform.position, 
                    target, 
                    finalSpeed * Time.deltaTime
                );
                
                Vector3 direction = target - car.transform.position;
                if (direction != Vector3.zero) {
                    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    float finalAngle = angle + tiers[car.tierIndex].rotationOffset;
                    car.transform.rotation = Quaternion.Euler(0, 0, finalAngle);
                }

                if (Vector3.Distance(car.transform.position, target) < 0.1f) {
                    car.currentWaypointIndex = (car.currentWaypointIndex + 1) % loc.path.PointsCount;
                }
            }
        }
    }

    private async UniTaskVoid StartEconomyLoop(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            await UniTask.Delay(1000, cancellationToken: token);

            double income = 0;
            foreach (var loc in locations) {
                foreach (var car in loc.carsInLocation) {
                    income += tiers[car.tierIndex].income;
                }
            }

            AddMoney(income * incomeMultiplier);
            Debug.Log($"Общий доход: {income * incomeMultiplier}$ | Баланс: {totalMoney}$");
        }
    }

    private GameObject GetCarFromPool() {
        if (pool.Count > 0) {
            GameObject obj = pool.Pop();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(carPrefab);
    }
    
    public void SetCurrentLocation(int index) {
        if (index >= 0 && index < locations.Length) {
            currentLocIndex = index;
            Debug.Log($"Теперь покупаем машины для: {locations[index].locationName}");
        }
    }

    void OnDestroy() => _cts?.Cancel();
}