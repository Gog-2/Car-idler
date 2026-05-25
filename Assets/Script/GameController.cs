using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text moneyText;
    public TMP_Text lapsText;
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
    public int totalLaps;
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
        UpdateLaps(0);
        StartEconomyLoop(_cts.Token).Forget();
        
        // Запускаем корутины для каждой локации
        foreach (var loc in locations) {
            loc.UpdateLocalCurrencyUI(totalMoney, totalLaps);
            loc.UpdateCooldown();
            StartLootBoxSpawnLoop(loc, _cts.Token).Forget();
        }

        BuyCar();
        // Инициализация UI закрытых локаций
        for (int i = 0; i < locations.Length; i++) {
            var loc = locations[i];
            if (loc.unlockCostText != null)
                loc.unlockCostText.text = $"{loc.unlockLapCost} 🔄";
    
            if (!loc.isUnlocked) {
                if (loc.unlockButton != null) loc.unlockButton.SetActive(true);
                if (loc.lockedOverlay != null) loc.lockedOverlay.SetActive(true);
            } else {
                ApplyUnlockVisuals(i);
            }
        }
    }

    private async UniTaskVoid StartLootBoxSpawnLoop(LocationData loc, CancellationToken token) {
        float timeUntilSpawn = loc.currentCooldown;
        
        while (!token.IsCancellationRequested) {
            if (!loc.isUnlocked) {
                if (loc.lootBoxTimerText != null) loc.lootBoxTimerText.text = "";
                await UniTask.Delay(500, cancellationToken: token);
                continue;
            }
            while (timeUntilSpawn > 0 && !token.IsCancellationRequested) {
                await UniTask.Delay(100, cancellationToken: token);
                timeUntilSpawn -= 0.1f;
                loc.lootBoxTimerText.text = $"Лут бокс: {timeUntilSpawn:F1}с";
            }
            SpawnLootBox(loc);
            loc.lootBoxTimerText.text = "Лут бокс: Появился!";
            await UniTask.Delay(1000, cancellationToken: token);
            
            timeUntilSpawn = loc.currentCooldown;
        }
    }
    private void UpdateLaps(int amount) {
        totalLaps += amount;
        if (lapsText != null)
            lapsText.text = $"Кругов: {totalLaps}";
        
        foreach (var loc in locations) {
            loc.UpdateLocalCurrencyUI(totalMoney, totalLaps);
        }
    }

    public void AddLaps(int amount) {
        UpdateLaps(amount);
    }
    
    private void UpdateMoney(double amount) {
        totalMoney += amount;
        if (moneyText != null)
            moneyText.text = $"{totalMoney:F0}$";
        if (floatingTextPool != null && amount > 0)
            floatingTextPool.Show($"+{amount:F0}$");
        foreach (var loc in locations) {
            loc.UpdateLocalCurrencyUI(totalMoney, totalLaps);
        }
    }

    public void AddMoney(double amount) {
        UpdateMoney(amount);
    }
    
    public void BuyCar() {
        BuyCarAtLocation(currentLocIndex);
    }

    public void BuyCarAtLocation(int locIndex) {
        LocationData currentLoc = locations[locIndex];

        if (!currentLoc.isUnlocked) {
            Debug.Log("Локация закрыта!");
            return;
        }
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

    public void RefreshUpgradeUI(int locIndex) {
        var loc = locations[locIndex];
        double speedCost = 100 * Mathf.Pow(1.5f, loc.speedLevel);
        double cooldownCost = 200 * Mathf.Pow(1.8f, loc.cooldownLevel);
        double nextCost = 50 * Mathf.Pow(1.2f, loc.carsInLocation.Count);
        loc.UpdateUpgradeUI(nextCost, speedCost, cooldownCost);
    }

    void SpawnLootBox(LocationData loc) {
        if (loc.lootBoxPrefab == null || loc.path == null || loc.path.PointsCount < 2) return;

        // Выбираем случайный сегмент пути
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
                double bonus = totalMoney * 0.1;
                AddMoney(bonus);
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
        var loc = locations[locIndex];
    
        if (loc.speedLevel >= LocationData.MAX_UPGRADE_LEVEL) {
            Debug.Log("Скорость уже максимальна!");
            return;
        }
    
        double cost = 100 * Mathf.Pow(1.5f, loc.speedLevel);
        if (totalMoney >= cost) {
            totalMoney -= cost;
            loc.speedLevel++;
            Debug.Log($"Скорость на {loc.locationName} улучшена до уровня {loc.speedLevel}!");
            RefreshUpgradeUI(locIndex);
        } else {
            Debug.Log($"Недостаточно денег! Нужно {cost}$");
        }
    }

    public void UpgradeLootBoxCooldown(int locIndex) {
        var loc = locations[locIndex];
    
        if (loc.cooldownLevel >= LocationData.MAX_UPGRADE_LEVEL) {
            Debug.Log("Кулдаун уже минимален!");
            return;
        }
    
        double cost = 200 * Mathf.Pow(1.8f, loc.cooldownLevel);
        if (totalMoney >= cost) {
            totalMoney -= cost;
            loc.cooldownLevel++;
            loc.UpdateCooldown();
            Debug.Log($"Кулдаун лутбокса на {loc.locationName} уменьшен до уровня {loc.cooldownLevel}!");
            RefreshUpgradeUI(locIndex);
        } else {
            Debug.Log($"Недостаточно денег! Нужно {cost}$");
        }
    }

    void FixedUpdate() {
        foreach (var loc in locations) {
            if (!loc.isUnlocked) continue; // пропускаем закрытые
        
            float localMultiplier = loc.SpeedMultiplier * speedBoostMultiplier;

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
                    int prevIndex = car.currentWaypointIndex;
                    car.currentWaypointIndex = (car.currentWaypointIndex + 1) % loc.path.PointsCount;
                    if (prevIndex == loc.path.PointsCount - 1 && car.currentWaypointIndex == 0) {
                        AddLaps(1);
                    }
                }
            }
        }
    }

    private async UniTaskVoid StartEconomyLoop(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            await UniTask.Delay(1000, cancellationToken: token);

            double income = 0;
            foreach (var loc in locations) {
                if (!loc.isUnlocked) continue;
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
    public void UnlockLocation(int locIndex) {
        if (locIndex < 0 || locIndex >= locations.Length) return;
        var loc = locations[locIndex];
    
        if (loc.isUnlocked) {
            Debug.Log("Уже открыта!");
            return;
        }
    
        if (totalLaps < loc.unlockLapCost) {
            Debug.Log($"Недостаточно кругов! Нужно {loc.unlockLapCost}, есть {totalLaps}");
            return;
        }
    
        totalLaps -= loc.unlockLapCost;
        loc.isUnlocked = true;
        UpdateLaps(0); // обновить UI без изменения суммы
        ApplyUnlockVisuals(locIndex);
        Debug.Log($"Локация {loc.locationName} открыта!");
    }

    private void ApplyUnlockVisuals(int locIndex) {
        var loc = locations[locIndex];
        if (loc.unlockButton != null) loc.unlockButton.SetActive(false);
        if (loc.lockedOverlay != null) loc.lockedOverlay.SetActive(false);
    }

    void OnDestroy() => _cts?.Cancel();
}