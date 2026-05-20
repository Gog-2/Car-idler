using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;

public class GameController : MonoBehaviour
{
    [Header("Locations Setup")]
    public LocationData[] locations;
    public int currentLocIndex = 0;

    [Header("Global Settings")]
    public GameObject carPrefab;
    public TierConfig[] tiers;
    public int carsPerTier = 10;

    [Header("Economy")]
    public double totalMoney;
    
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
        BuyCar();
    }
    
    public void BuyCar() {
        LocationData currentLoc = locations[currentLocIndex];

        if (currentLoc.IsFull) {
            Debug.Log("Локация заполнена! Максимум 30 машин.");
            return;
        }
        
        int tierIndex = Mathf.Min(currentLoc.carsInLocation.Count / carsPerTier, tiers.Length - 1);
        TierConfig config = tiers[tierIndex];

        GameObject carObj = GetCarFromPool();
        carObj.transform.position = currentLoc.path.GetPoint(0);

        SpriteRenderer sr = carObj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = config.carSprite;

        CarInstance newCar = new CarInstance {
            transform = carObj.transform,
            spriteRenderer = sr,
            currentWaypointIndex = 0,
            tierIndex = tierIndex,
            locationIndex = currentLocIndex
        };

        currentLoc.carsInLocation.Add(newCar);
    }

    void FixedUpdate() {
        foreach (var loc in locations) {
            float localMultiplier = loc.speedMultiplier;

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
            totalMoney += income;
            Debug.Log($"Общий доход: {income}$ | Баланс: {totalMoney}$");
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