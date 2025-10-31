using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RatAI : MonoBehaviour
{
    [Header("Configuração do Minigame")]
    public float chaseDuration = 10f;
    public float speed = 6f;
    public bool destroyOnFinish = true;

    [Header("Waypoints")]
    public Transform[] waypoints; // arraste os pontos aqui no Inspector
    public bool loop = false;
    [Tooltip("Distância para considerar que chegou no waypoint")]
    public float waypointReachDistance = 0.5f;

    [Header("Movimento")]
    [Tooltip("Quão suave é a rotação (maior = mais brusco)")]
    public float rotationSpeed = 8f;
    [Tooltip("Altura do rato em relação ao chão")]
    public float heightOffset = 0.1f;
    [Tooltip("Máscara para detectar chão")]
    public LayerMask groundMask;

    // Estado interno
    bool active;
    int currentWaypoint;
    MinigameController owner;

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // Desenha linha entre waypoints
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }

        // Se for loop, liga o último ao primeiro
        if (loop && waypoints.Length > 1 && waypoints[0] != null && waypoints[^1] != null)
            Gizmos.DrawLine(waypoints[^1].position, waypoints[0].position);

        // Desenha esferas nos waypoints
        Gizmos.color = Color.cyan;
        foreach (var wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawWireSphere(wp.position, waypointReachDistance);
        }
    }

    public void StartRunning(MinigameController ownerController)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[RatAI] nenhum waypoint configurado em {name}");
            return;
        }

        owner = ownerController;
        active = true;
        currentWaypoint = 0;

        // Posiciona no primeiro waypoint
        if (waypoints[0] != null)
            transform.position = GetGroundPosition(waypoints[0].position);
    }

    void FixedUpdate()
    {
        if (!active || waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypoint];
        if (target == null) return;

        // Pega próxima posição (com ajuste de altura)
        Vector3 targetPos = GetGroundPosition(target.position);
        Vector3 currentPos = transform.position;

        // Move em direção ao waypoint
        Vector3 moveDir = (targetPos - currentPos);
        float distanceToTarget = moveDir.magnitude;
        moveDir.Normalize();

        // Rotaciona suavemente
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        // Move
        transform.position = Vector3.MoveTowards(currentPos, targetPos, speed * Time.fixedDeltaTime);

        // Chegou no waypoint atual?
        if (distanceToTarget <= waypointReachDistance)
        {
            // Próximo waypoint
            currentWaypoint++;

            // Acabou a rota?
            if (currentWaypoint >= waypoints.Length)
            {
                if (loop)
                    currentWaypoint = 0;
                else
                    RouteFinished();
            }
        }
    }

    Vector3 GetGroundPosition(Vector3 targetPos)
    {
        // Raycast para manter na altura correta em relação ao chão
        if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 4f, groundMask))
            return hit.point + Vector3.up * heightOffset;
        return targetPos;
    }

    void RouteFinished()
    {
        active = false;
        
        // Notifica área de trigger
        var trigger = GetComponentInChildren<RatTrigger>();
        if (trigger != null)
            trigger.OnMinigameEnded();
            
        if (owner != null)
            owner.NotifyRatEscaped(this);

        Destroy(gameObject);
    }

    public void StopRunning()
    {
        active = false;
    }

    public void OnCaught()
    {
        // Notifica área de trigger
        var trigger = GetComponentInChildren<RatTrigger>();
        if (trigger != null)
            trigger.OnMinigameEnded();
            
        Destroy(gameObject);
    }

    public float DistanceToPoint(Vector3 worldPos)
    {
        return Vector3.Distance(transform.position, worldPos);
    }

    public Vector3 GetCurrentTangent()
    {
        if (!active || waypoints == null || currentWaypoint >= waypoints.Length)
            return transform.forward;

        Transform target = waypoints[currentWaypoint];
        if (target == null) return transform.forward;

        Vector3 dir = (target.position - transform.position).normalized;
        return dir;
    }

    public float GetChaseDuration() => chaseDuration;
}