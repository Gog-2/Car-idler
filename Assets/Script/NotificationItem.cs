using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

public class NotificationItem : MonoBehaviour
{
    public TMP_Text label;
    public TMP_Text timerText;

    public async UniTaskVoid Play(string text, Color color, float duration, Action onDone) {
        label.text = text;
        label.color = color;
        timerText.color = color;

        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float remaining = duration - elapsed;
            timerText.text = $"{remaining:F1}s";
            await UniTask.Yield();
        }

        onDone?.Invoke();
    }
}