using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class LocationData {
    public string locationName;
    public PathProvider path;
    public int maxCars = 30;
    
    public float speedMultiplier = 1.0f; 
    
    [Header("Upgrades")]
    public int speedLevel = 0;
    public int cooldownLevel = 0;

    [Header("LootBox Settings")]
    public GameObject lootBoxPrefab;
    public float currentCooldown = 45f;
    public float nextSpawnTime;
    [Header("UI Upgrades")]
    public TMP_Text carInfo;
    public TMP_Text speedUpgradeText;
    public TMP_Text cooldownUpgradeText;

    [HideInInspector] 
    public List<GameController.CarInstance> carsInLocation = new List<GameController.CarInstance>();
    
    public bool IsFull => carsInLocation.Count >= maxCars;

    public void UpdateCooldown() =>currentCooldown = Mathf.Max(10, 45 - cooldownLevel);

    public void UpdateUpgradeUI(double nextCarCost, double speedCost, double cooldownCost) {
        carInfo.text = $"{carsInLocation.Count}/{maxCars} | {nextCarCost:F0}$";
        speedUpgradeText.text = $"{speedMultiplier:F1}x -> {speedMultiplier + 0.1f:F1}x | {speedCost:F0}$";
        cooldownUpgradeText.text = $"{currentCooldown:F0}s -> {Mathf.Max(10, currentCooldown - 1):F0}s | {cooldownCost:F0}$";
    }
}