using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class LocationData {
    public const int MAX_UPGRADE_LEVEL = 15;
    
    public string locationName;
    public PathProvider path;
    public int maxCars = 30;
    
    [Header("Upgrades")]
    public int speedLevel = 0;
    public int cooldownLevel = 0;

    [Header("LootBox Settings")]
    public GameObject lootBoxPrefab;
    public float currentCooldown = 45f;
    
    [Header("UI Value")]
    public TMP_Text localMoneyText;
    public TMP_Text localLapsText; 
    [Header("UI Upgrades")]
    
    public TMP_Text carCountText;      // "5/30"
    public TMP_Text carCostText;       // "150$"
    public TMP_Text lootBoxTimerText; // Таймер до спавна лутбокса (вернул!)
    
    public TMP_Text speedLevelText;    // "1/15"
    public TMP_Text speedCostText;     // "150$" или "MAX"
    public TMP_Text speedInfoText;     // "1.0x -> 1.1x"
    
    public TMP_Text cooldownLevelText; // "1/15"
    public TMP_Text cooldownCostText;  // "200$" или "MAX"
    public TMP_Text cooldownInfoText;  // "45s -> 43s"
    [Header("Unlock Settings")]
    public bool isUnlocked = true;
    public int unlockLapCost = 100;

    [Header("Unlock UI (only for locked)")]
    public GameObject unlockButton;         // Кнопка "Открыть"
    public TMP_Text unlockCostText;         // "100 кругов"
    public GameObject lockedOverlay;        // Полупрозрачная плашка поверх локации

    [HideInInspector] 
    public List<GameController.CarInstance> carsInLocation = new List<GameController.CarInstance>();
    
    public bool IsFull => carsInLocation.Count >= maxCars;
    
    // Вычисляемое свойство: от 1.0x до 3.0x
    public float SpeedMultiplier => 1.0f + (speedLevel * (2f / MAX_UPGRADE_LEVEL));

    public void UpdateCooldown() {
        // От 45s до 10s за 15 уровней
        currentCooldown = 45f - (cooldownLevel * (35f / MAX_UPGRADE_LEVEL));
        currentCooldown = Mathf.Max(10f, currentCooldown);
    }

    public void UpdateUpgradeUI(double nextCarCost, double speedCost, double cooldownCost) {
        // Car UI (разделено на два поля)
        if (carCountText != null) {
            carCountText.text = $"{carsInLocation.Count}/{maxCars}";
        }
        if (carCostText != null) {
            carCostText.text = $"{nextCarCost:F0}$";
        }
    
        // Speed UI
        if (speedLevelText != null) {
            speedLevelText.text = $"{speedLevel}/{MAX_UPGRADE_LEVEL}";
        }
        if (speedCostText != null) {
            if (speedLevel >= MAX_UPGRADE_LEVEL) {
                speedCostText.text = "MAX";
            } else {
                speedCostText.text = $"{speedCost:F0}$";
            }
        }
        if (speedInfoText != null) {
            float currentMult = SpeedMultiplier;
            float nextMult = speedLevel >= MAX_UPGRADE_LEVEL ? currentMult : 1.0f + ((speedLevel + 1) * (2f / MAX_UPGRADE_LEVEL));
            speedInfoText.text = $"{currentMult:F2}x -> {nextMult:F2}x";
        }
    
        // Cooldown UI
        if (cooldownLevelText != null) {
            cooldownLevelText.text = $"{cooldownLevel}/{MAX_UPGRADE_LEVEL}";
        }
        if (cooldownCostText != null) {
            if (cooldownLevel >= MAX_UPGRADE_LEVEL) {
                cooldownCostText.text = "MAX";
            } else {
                cooldownCostText.text = $"{cooldownCost:F0}$";
            }
        }
        if (cooldownInfoText != null) {
            float currentCd = 45f - (cooldownLevel * (35f / MAX_UPGRADE_LEVEL));
            float nextCd = cooldownLevel >= MAX_UPGRADE_LEVEL ? currentCd : 45f - ((cooldownLevel + 1) * (35f / MAX_UPGRADE_LEVEL));
            currentCd = Mathf.Max(10f, currentCd);
            nextCd = Mathf.Max(10f, nextCd);
            cooldownInfoText.text = $"{currentCd:F1}s -> {nextCd:F1}s";
        }
    }
    public void UpdateLocalCurrencyUI(double money, int laps) {
        if (localMoneyText != null)
            localMoneyText.text = $"{money:F0}$";
        if (localLapsText != null)
            localLapsText.text = $"{laps} 🔄";
    }
}