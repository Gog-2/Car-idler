using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationData {
    public string locationName;
    public PathProvider path;
    public int maxCars = 30;
    
    public float speedMultiplier = 1.0f; 
    
    [HideInInspector] 
    public List<GameController.CarInstance> carsInLocation = new List<GameController.CarInstance>();
    
    public bool IsFull => carsInLocation.Count >= maxCars;
}