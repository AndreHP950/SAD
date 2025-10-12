using UnityEngine;

public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("Refer�ncias")]
    public Transform pivot;

    [Header("Dist�ncias")]
    public float defaultDistance = 4f;
    public float minDistance = 0.5f;
    public float maxDistance = 6f;
    public float heightOffset = 0.8f;
    public float shoulderOffset = 0f;

    [Header("Colis�o")]
    public float radius = 0.25f;
    public float collisionBuffer = 0.05f;
    public LayerMask collisionMask = ~0;

    [Header("Suaviza��o")]
    public float approachSmoothing = 18f;   // R�pido ao aproximar de obst�culos
    public float retreatSmoothing = 6f;     // LENTO ao afastar de obst�culos (evita movimento brusco)
    public float positionSmoothing = 20f;

    [Header("Velocidade M�xima")]
    public float maxCameraSpeed = 15f;      // Velocidade m�xima da c�mera (unidades/segundo)
    public bool limitSpeed = true;          // Ativa/desativa o limite de velocidade

    [Header("Debug")]
    public bool drawDebug = false;

    float currentDistance;
    Vector3 lastPosition;

    void Awake()
    {
        currentDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        if (pivot == null) return;

        Vector3 pivotPos = pivot.position + Vector3.up * heightOffset + pivot.right * shoulderOffset;
        Vector3 backDir = -pivot.forward;
        float desiredDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);

        bool hitSomething = Physics.SphereCast(
            pivotPos,
            radius,
            backDir,
            out RaycastHit hit,
            desiredDistance + radius,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            float safeDist = Mathf.Max(hit.distance - collisionBuffer, minDistance);
            desiredDistance = Mathf.Min(safeDist, desiredDistance);
        }

        // Suaviza��o assim�trica: r�pido ao aproximar, lento ao afastar
        float smoothing = (desiredDistance < currentDistance) ? approachSmoothing : retreatSmoothing;
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        Vector3 targetPos = pivotPos + backDir * currentDistance;
        Vector3 newPos = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-positionSmoothing * Time.deltaTime));

        // Limita a velocidade m�xima da c�mera
        if (limitSpeed && Time.deltaTime > 0)
        {
            Vector3 displacement = newPos - lastPosition;
            float distance = displacement.magnitude;
            float maxAllowedDistance = maxCameraSpeed * Time.deltaTime;

            if (distance > maxAllowedDistance)
            {
                // Limita o deslocamento � velocidade m�xima
                newPos = lastPosition + displacement.normalized * maxAllowedDistance;
            }
        }

        transform.position = newPos;
        lastPosition = transform.position;
        transform.LookAt(pivotPos);

        if (drawDebug)
        {
            Color c = hitSomething ? Color.red : Color.green;
            Debug.DrawLine(pivotPos, pivotPos + backDir * desiredDistance, c);
        }
    }
}