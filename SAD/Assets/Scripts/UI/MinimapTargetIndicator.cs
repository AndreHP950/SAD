using UnityEngine;

public class MinimapTargetIndicator : MonoBehaviour
{
    [Header("References")]
    public Camera minimapCamera;
    public RectTransform minimapRect;
    public RectTransform targetIcon;
    public RectTransform edgeIndicator;
    public Transform target;

    [Header("Settings")]
    public float borderPadding = 10f;
    public float mapViewPadding = 60f;
    [Range(0f, 1f)] public float roundness = 0.6f;

    private void Update()
    {
        if (minimapCamera == null || target == null)
        {
            if (targetIcon.gameObject.activeInHierarchy || edgeIndicator.gameObject.activeInHierarchy)
            {
                targetIcon.gameObject.SetActive(false);
                edgeIndicator.gameObject.SetActive(false);
            }
            return;
        }

        Vector3 screenPos = minimapCamera.WorldToViewportPoint(target.position);
        bool isInside = screenPos.z > 0 &&
                        screenPos.x > 0 && screenPos.x < 1 &&
                        screenPos.y > 0 && screenPos.y < 1;

        Vector2 localPos = new Vector2(
            (screenPos.x - 0.5f) * minimapRect.rect.width,
            (screenPos.y - 0.5f) * minimapRect.rect.height
        );

        Vector2 halfSize = minimapRect.rect.size * 0.5f - Vector2.one * (borderPadding + mapViewPadding);

        if (isInside &&
            Mathf.Abs(localPos.x) < halfSize.x &&
            Mathf.Abs(localPos.y) < halfSize.y)
        {
            targetIcon.gameObject.SetActive(true);
            edgeIndicator.gameObject.SetActive(false);

            targetIcon.localPosition = localPos;
            targetIcon.localRotation = Quaternion.identity;
        }
        else
        {
            targetIcon.gameObject.SetActive(false);
            edgeIndicator.gameObject.SetActive(true);

            Vector3 worldDir = target.position - minimapCamera.transform.position;
            Vector2 dir2D = new Vector2(worldDir.x, worldDir.z).normalized;

            float camYRot = minimapCamera.transform.eulerAngles.y * Mathf.Deg2Rad;
            float cos = Mathf.Cos(camYRot);
            float sin = Mathf.Sin(camYRot);

            Vector2 rotatedDir = new Vector2(
                dir2D.x * cos - dir2D.y * sin,
                dir2D.x * sin + dir2D.y * cos
            );

            float t = Mathf.Pow(Mathf.Abs(rotatedDir.x) * Mathf.Abs(rotatedDir.y), roundness);
            float mixX = Mathf.Lerp(halfSize.x, halfSize.x * 0.7f, t);
            float mixY = Mathf.Lerp(halfSize.y, halfSize.y * 0.7f, t);

            Vector2 edgePos = new Vector2(rotatedDir.x * mixX, rotatedDir.y * mixY);
            float angle = Mathf.Atan2(rotatedDir.x, rotatedDir.y) * Mathf.Rad2Deg;

            edgeIndicator.localPosition = edgePos;
            edgeIndicator.localRotation = Quaternion.Euler(0, 0, -angle);
        }
    }
}
