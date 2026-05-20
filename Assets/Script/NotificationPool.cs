using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class NotificationPool : MonoBehaviour
{
    [Header("Pool")]
    public NotificationItem[] items;

    private Stack<NotificationItem> pool = new Stack<NotificationItem>();

    void Start() {
        foreach (var item in items) {
            item.gameObject.SetActive(false);
            pool.Push(item);
        }
    }

    public void Show(string text, Color color, float duration) {
        if (pool.Count == 0) return;
        var item = pool.Pop();
        item.gameObject.SetActive(true);
        item.Play(text, color, duration, () => {
            item.gameObject.SetActive(false);
            pool.Push(item);
        }).Forget();
    }
}