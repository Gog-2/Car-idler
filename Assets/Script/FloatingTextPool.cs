using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

public class FloatingTextPool : MonoBehaviour
{
    [Header("Ready Objects")]
    public TMP_Text[] floatingTexts;

    [Header("Spawn Range")]
    public Transform spawnPointA;
    public Transform spawnPointB;

    private int currentIndex = 0;

    public void Show(string text) {
        TMP_Text tmp = floatingTexts[currentIndex];
        currentIndex = (currentIndex + 1) % floatingTexts.Length;

        float x = Random.Range(spawnPointA.position.x, spawnPointB.position.x);
        tmp.transform.position = new Vector3(x, spawnPointA.position.y, spawnPointA.position.z);
        tmp.text = text;

        AnimateFade(tmp).Forget();
    }

    private async UniTaskVoid AnimateFade(TMP_Text tmp) {
        float elapsed = 0f;
        float duration = 1.5f;
        Color c = tmp.color;
        c.a = 1f;
        tmp.color = c;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            tmp.color = c;
            await UniTask.Yield();
        }

        c.a = 0f;
        tmp.color = c;
    }
}