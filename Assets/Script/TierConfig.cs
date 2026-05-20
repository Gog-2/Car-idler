using UnityEngine;

[CreateAssetMenu(fileName = "NewTier", menuName = "Racing/TierConfig")]
public class TierConfig : ScriptableObject
{
    public Sprite carSprite;
    public float income;
    public float speed;
    
    [Tooltip("Подкрути это значение, если машина едет боком. Обычно 0, 90 или -90")]
    public float rotationOffset = -90f; 
}