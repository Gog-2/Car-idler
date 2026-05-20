using UnityEngine;

public class LootBox : MonoBehaviour
{
    public int hp = 9;
    public int locationIndex;
    public System.Action<int> OnBoxBroken; // Событие при поломке

    public void TakeDamage()
    {
        hp--;
        // Можно добавить анимацию тряски здесь
        if (hp <= 0)
        {
            OnBoxBroken?.Invoke(locationIndex);
            Destroy(gameObject); // Или возвращаем в пул
        }
    }
}