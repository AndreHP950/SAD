using TMPro;
using UnityEngine;

public class ChangedValueSpawner : MonoBehaviour
{
    public TextMeshProUGUI floatingTextPrefab;
    public RectTransform spawnPoint;

    public void SpawnText(int value)
    {
        TextMeshProUGUI text = Instantiate(floatingTextPrefab, transform);

        RectTransform rt = text.GetComponent<RectTransform>();
        text.text = $"+{value}";

        rt.anchoredPosition = new Vector2(0, -80f);

        FloatingTextUI move = text.gameObject.AddComponent<FloatingTextUI>();
    }
}
