using UnityEngine;

public class FloatingTextUI : MonoBehaviour
{
    public Vector2 target;
    public float moveSpeed = 3f;
    public float fadeSpeed = 1.5f;

    private RectTransform rt;
    private CanvasGroup group;

    private void Start()
    {
        rt = GetComponent<RectTransform>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
    }

    private void Update()
    {
        rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, Vector2.zero, Time.deltaTime * moveSpeed);

        group.alpha -= Time.deltaTime * fadeSpeed;

        if (group.alpha < 0f)
        {
            Destroy(gameObject); 
        }
    }
}
