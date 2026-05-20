using UnityEngine;

using System.Collections.Generic;

public class LootBox : MonoBehaviour
{
    public int hp = 3; 
    public int locationIndex;
    public System.Action<int> OnBoxBroken; 

    private Dictionary<Car, float> carDamageCooldowns = new Dictionary<Car, float>();
    private float damageCooldown = 1.0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Car car = collision.GetComponent<Car>();
        if (car != null)
        {
            if (!carDamageCooldowns.ContainsKey(car) || Time.time >= carDamageCooldowns[car])
            {
                TakeDamage();
                carDamageCooldowns[car] = Time.time + damageCooldown;
            }
        }
    }

    public void TakeDamage()
    {
        hp--;
        transform.localScale *= 0.9f; 
        
        if (hp <= 0)
        {
            OnBoxBroken?.Invoke(locationIndex);
            Destroy(gameObject); 
        }
    }
}