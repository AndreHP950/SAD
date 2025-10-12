using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class TrafficVehicleSpline : MonoBehaviour
{
    public enum VehicleType { Car, Bus }
    public enum SensorShape { Box, Sphere }
    enum BlockKind { None, Player, Vehicle, Other, Intersection, StopZone }

    [Header("Tipo")]
    public VehicleType type = VehicleType.Car;

    [Header("Rota")]
    public TrafficSplineRoute route;
    [Tooltip("Distância inicial ao longo da rota (em metros).")]
    public float startDistance = 0f;

    [Header("Dinâmica")]
    public float maxSpeed = 10f;
    public float acceleration = 8f;
    public float brakeAcceleration = 14f;
    [Tooltip("Quanto mais alto, mais rápido gira para alinhar ao tangente da rota.")]
    public float turnLerp = 10f;

    [Header("Sensores")]
    [Tooltip("Layers que podem ser detectadas (Player, Vehicles, Obstacles).")]
    public LayerMask obstacleMask = ~0;
    public SensorShape sensorShape = SensorShape.Box;

    [Tooltip("Ponto opcional para origem do sensor (deixe null para usar offset).")]
    public Transform sensorOrigin;
    [Tooltip("Comprimento do sensor à frente.")]
    public float sensorLength = 10f;
    [Tooltip("Largura/altura do sensor (Box) ou raio (Sphere).")]
    public Vector2 sensorSize = new Vector2(1.2f, 0.9f); // x=largura, y=altura (para Box); usa máx para Sphere
    [Tooltip("Offset para frente quando não há sensorOrigin.")]
    public float sensorForwardOffset = 1.2f;
    [Tooltip("Offset para cima do sensor.")]
    public float sensorUpOffset = 0.5f;
    [Tooltip("Margem do para-choque: subtrai essa distância do hit para comparar com safeDistance.")]
    public float frontBuffer = 0.6f;

    [Tooltip("Distância mínima segura para parar antes do obstáculo.")]
    public float safeDistance = 2f;

    [Header("Interseções/Paradas")]
    public bool obeyIntersections = true;
    public bool obeyStopZones = true;

    [Header("Anti-Deadlock (geral)")]
    public float minSpeedToConsiderMoving = 0.25f;
    public float stuckTimeout = 4f;
    public float courtesyNudgeSpeed = 0.6f;

    [Header("Anti-Travamento em Curvas")]
    [Tooltip("Tempo máximo parado atrás de OUTRO VEÍCULO antes de avançar mesmo assim.")]
    public float maxWaitWhenBlockedByVehicle = 2.0f;
    [Tooltip("Velocidade usada para 'forçar a passagem' após o tempo de espera.")]
    public float forceProceedSpeed = 1.0f;

    [Header("Ground Snap")]
    [Tooltip("Layer(s) do terreno/rua.")]
    public LayerMask groundMask = 1 << 0; // Default
    public float rideHeight = 0.35f;
    public float groundRayLength = 3f;
    public bool alignToGroundNormal = false;
    [Tooltip("Opcional: arraste o filho visual (malha). Se nulo, o próprio transform é usado.")]
    public Transform visualRoot;

    [Header("Parada Física")]
    [Tooltip("Multiplica a desaceleração quando parado (ajuda a não 'arrastar').")]
    public float hardBrakeDamping = 2.0f;

    [Header("Layers por nome (para identificar Player)")]
    public string playerLayerName = "Player";

    // Estado
    float distanceOnRoute;
    float currentSpeed;
    IntersectionZone currentIntersection;
    bool intersectionGranted;
    bool inStopRoutine;
    float stuckTimer;

    // Anti-travamento por bloqueio
    BlockKind blockKind = BlockKind.None;
    float blockTimer = 0f;

    // Se o obstáculo detectado é um veículo em mão contrária
    bool lastObstacleOppositeLane = false;

    // Colisão atual (para reforçar parada)
    bool touchingSomething;
    bool touchingPlayerOrVehicle;

    // Cache
    Rigidbody rb;
    Collider bodyCol;
    int playerLayer = -1;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCol = GetComponent<Collider>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        playerLayer = LayerMask.NameToLayer(playerLayerName);
    }

    void Start()
    {
        if (route == null || route.totalLength <= 0f)
        {
            Debug.LogWarning($"{name}: route não configurada ou sem comprimento.");
            enabled = false; return;
        }
        distanceOnRoute = Mathf.Repeat(startDistance, route.totalLength);
        UpdateTransformImmediate();
    }

    void FixedUpdate()
    {
        if (route == null || route.totalLength <= 0f) return;

        float dt = Time.fixedDeltaTime;
        float desiredSpeed = maxSpeed;

        // Reset por frame
        touchingSomething = false;
        touchingPlayerOrVehicle = false;
        lastObstacleOppositeLane = false;

        // 1) Interseções
        if (obeyIntersections && currentIntersection != null && !intersectionGranted)
        {
            desiredSpeed = 0f;
            SetBlock(BlockKind.Intersection, dt);
            // TODO: SFX HORN - parado em interseção sem prioridade (beep curto/intervalado)
        }

        // 2) Stop zones
        if (inStopRoutine)
        {
            desiredSpeed = 0f;
            SetBlock(BlockKind.StopZone, dt);
            // (Normalmente não buzinaria em parada programada)
        }

        // 3) Sensor frontal
        bool hit = SenseObstacle(out float obsDistRaw, out float obsSpeed, out BlockKind hitKind, out bool isOppositeLane);
        float obsDist = obsDistRaw - frontBuffer;

        if (desiredSpeed > 0f && hit)
        {
            SetBlock(hitKind, dt);
            lastObstacleOppositeLane = (hitKind == BlockKind.Vehicle) && isOppositeLane;

            float clamped = Mathf.Clamp(obsDist, 0f, sensorLength);
            float factor = Mathf.InverseLerp(safeDistance, sensorLength, clamped);
            desiredSpeed = Mathf.Min(desiredSpeed, Mathf.Lerp(0f, maxSpeed, Mathf.Clamp01(factor)));
            if (obsSpeed >= 0f) desiredSpeed = Mathf.Min(desiredSpeed, obsSpeed);

            // muito perto de QUALQUER obstáculo → para de vez
            if (obsDist <= safeDistance + 0.05f)
            {
                desiredSpeed = 0f;
                // TODO: SFX HORN - muito perto (Player / Vehicle / Other)
            }
        }
        else if (desiredSpeed > 0f)
        {
            ClearBlock();
        }

        // 4) Se encostando fisicamente em algo, reforça o "zero"
        if (touchingSomething && touchingPlayerOrVehicle)
        {
            desiredSpeed = 0f;
            // TODO: SFX HORN - colisão/encostando (opcional, com cooldown)
        }

        // 5) Anti-deadlock geral (NUNCA avança contra player)
        if (currentSpeed < minSpeedToConsiderMoving && desiredSpeed <= 0.05f)
        {
            stuckTimer += dt;
            if (stuckTimer >= stuckTimeout)
            {
                if (blockKind != BlockKind.Player)
                    desiredSpeed = courtesyNudgeSpeed;
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;

        // 6) Anti-travamento VEÍCULO×VEÍCULO: só força passagem se o obstáculo estiver em mão contrária
        if (blockKind == BlockKind.Vehicle && blockTimer >= maxWaitWhenBlockedByVehicle && lastObstacleOppositeLane)
        {
            desiredSpeed = Mathf.Max(desiredSpeed, forceProceedSpeed);
            // TODO: SFX HORN - forçar passagem após esperar (buzina mais longa)
        }

        // Integração da velocidade
        float accel = (desiredSpeed >= currentSpeed) ? acceleration : brakeAcceleration;
        if (desiredSpeed <= 0.001f) accel *= hardBrakeDamping;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * dt);

        // Avança na rota
        distanceOnRoute += currentSpeed * dt;
        if (route.loop) distanceOnRoute = Mathf.Repeat(distanceOnRoute, route.totalLength);
        else distanceOnRoute = Mathf.Clamp(distanceOnRoute, 0f, route.totalLength);

        // Pose base pela spline
        float t = route.DistanceToT(distanceOnRoute);
        Vector3 posOnSpline = route.EvaluatePosition(t);
        Vector3 tan = route.EvaluateTangent(t);
        Vector3 forward = new Vector3(tan.x, 0f, tan.z);
        if (forward.sqrMagnitude < 1e-4f) forward = transform.forward;
        forward.Normalize();

        // Snap ao chão
        Vector3 finalPos = posOnSpline;
        Vector3 up = Vector3.up;
        if (Physics.Raycast(posOnSpline + Vector3.up * 1.5f, Vector3.down, out RaycastHit gHit,
                            groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            finalPos.y = gHit.point.y + rideHeight;
            if (alignToGroundNormal) up = gHit.normal;
        }

        // Aplicar com Rigidbody
        Quaternion targetRot = Quaternion.Slerp(rb.rotation, Quaternion.LookRotation(forward, up), 1f - Mathf.Exp(-turnLerp * dt));
        rb.MoveRotation(targetRot);

        Vector3 smoothed = Vector3.Lerp(rb.position, finalPos, 1f - Mathf.Exp(-12f * dt));
        rb.MovePosition(smoothed);

        if (desiredSpeed <= 0.001f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        touchingSomething = true;

        bool isPlayerCol = collision.collider.gameObject.layer == playerLayer && playerLayer >= 0;
        bool isVehicleCol = collision.collider.GetComponentInParent<TrafficVehicleSpline>() != null;

        if (isPlayerCol || isVehicleCol)
        {
            touchingPlayerOrVehicle = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ---------- Sensor ----------
    bool SenseObstacle(out float distance, out float obstacleSpeed, out BlockKind hitKind, out bool isOppositeLane)
    {
        distance = sensorLength;
        obstacleSpeed = -1f;
        hitKind = BlockKind.None;
        isOppositeLane = false;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 origin = sensorOrigin
            ? sensorOrigin.position
            : transform.position + Vector3.up * sensorUpOffset + fwd * sensorForwardOffset;

        RaycastHit hit;

        if (sensorShape == SensorShape.Box)
        {
            Vector3 halfExt = new Vector3(sensorSize.x * 0.5f, sensorSize.y * 0.5f, 0.01f);
            bool got = Physics.BoxCast(origin, halfExt, fwd, out hit, transform.rotation, sensorLength,
                                       obstacleMask, QueryTriggerInteraction.Ignore);
            if (!got) return false;
        }
        else
        {
            float radius = Mathf.Max(sensorSize.x, sensorSize.y) * 0.5f;
            bool got = Physics.SphereCast(origin, radius, fwd, out hit, sensorLength,
                                          obstacleMask, QueryTriggerInteraction.Ignore);
            if (!got) return false;
        }

        distance = hit.distance;

        // Player?
        if (hit.collider.gameObject.layer == playerLayer && playerLayer >= 0)
        {
            hitKind = BlockKind.Player;
            return true;
        }

        // Outro veículo?
        var other = hit.collider.GetComponentInParent<TrafficVehicleSpline>();
        if (other != null)
        {
            obstacleSpeed = other.currentSpeed;
            hitKind = BlockKind.Vehicle;

            // *** Checagem de mão contrária ***
            Vector3 otherFwd = other.transform.forward; otherFwd.y = 0f; otherFwd.Normalize();
            float dot = Vector3.Dot(fwd, otherFwd);
            isOppositeLane = (dot < -0.3f); // ajustável: -1=oposto perfeito, 0=perpendicular

            return true;
        }

        // Obstáculo genérico
        hitKind = BlockKind.Other;
        return true;
    }

    void UpdateTransformImmediate()
    {
        float t = route.DistanceToT(distanceOnRoute);
        Vector3 posOnSpline = route.EvaluatePosition(t);
        Vector3 tan = route.EvaluateTangent(t);
        Vector3 forward = new Vector3(tan.x, 0f, tan.z).normalized;

        // snap inicial ao chão
        Vector3 finalPos = posOnSpline;
        if (Physics.Raycast(posOnSpline + Vector3.up * 1.5f, Vector3.down, out RaycastHit gHit,
                            groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
        {
            finalPos.y = gHit.point.y + rideHeight;
        }

        rb.position = finalPos;
        if (forward.sqrMagnitude > 1e-4f)
            rb.rotation = Quaternion.LookRotation(forward, Vector3.up);

        if (visualRoot != null)
        {
            visualRoot.position = finalPos;
            visualRoot.rotation = rb.rotation;
        }
    }

    // ---------- Interseções ----------
    public void NotifyEnterIntersection(IntersectionZone zone)
    {
        currentIntersection = zone;
        intersectionGranted = zone != null && zone.HasPriority(this);
        // TODO: SFX HORN - chegou numa interseção e vai aguardar (curto)
    }

    public void GrantIntersection(IntersectionZone zone)
    {
        if (zone == currentIntersection)
            intersectionGranted = true;
        // (Normalmente não buzina ao ganhar prioridade)
    }

    public void NotifyExitIntersection(IntersectionZone zone)
    {
        if (zone == currentIntersection)
        {
            currentIntersection = null;
            intersectionGranted = false;
        }
    }

    // ---------- Paradas ----------
    public void RequestStop(float seconds)
    {
        if (!obeyStopZones) return;
        if (inStopRoutine) return;
        StartCoroutine(StopRoutine(seconds));
        // (Não buzinaria em parada programada)
    }

    IEnumerator StopRoutine(float seconds)
    {
        inStopRoutine = true;
        float t = seconds;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }
        inStopRoutine = false;
    }

    public float GetCurrentSpeed() => currentSpeed;

    // ---------- Gizmos ----------
    void OnDrawGizmosSelected()
    {
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 origin = sensorOrigin ? sensorOrigin.position
                                      : transform.position + Vector3.up * sensorUpOffset + fwd * sensorForwardOffset;

        Gizmos.color = Color.cyan;
        if (sensorShape == SensorShape.Box)
        {
            Vector3 halfExt = new Vector3(sensorSize.x * 0.5f, sensorSize.y * 0.5f, 0.01f);
            Matrix4x4 m = Matrix4x4.TRS(origin + fwd * (sensorLength * 0.5f), transform.rotation, Vector3.one);
            Gizmos.matrix = m;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(halfExt.x * 2f, halfExt.y * 2f, sensorLength));
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawLine(origin, origin + fwd * sensorLength);
        }
        else
        {
            float radius = Mathf.Max(sensorSize.x, sensorSize.y) * 0.5f;
            Gizmos.DrawWireSphere(origin + fwd * sensorLength, radius);
            Gizmos.DrawLine(origin, origin + fwd * sensorLength);
        }

        Gizmos.color = Color.yellow;
        Vector3 basePos = visualRoot ? visualRoot.position : transform.position;
        Vector3 rayStart = basePos + Vector3.up * 1.5f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundRayLength);
    }

    // ---------- Helpers de bloqueio ----------
    void SetBlock(BlockKind k, float dt)
    {
        if (blockKind == k)
        {
            blockTimer += dt;
        }
        else
        {
            blockKind = k;
            blockTimer = 0f;

            // PRIMEIRA vez que entrou no bloqueio: lugar bom para um beep curto.
            if (k == BlockKind.Player)
            {
                // TODO: SFX HORN - player entrou no sensor (beep curto)
            }
            else if (k == BlockKind.Vehicle)
            {
                // TODO: SFX HORN - veículo entrou no sensor (beep curto)
            }
            else if (k == BlockKind.Other)
            {
                // TODO: SFX HORN - obstáculo entrou no sensor (curto)
            }
            else if (k == BlockKind.Intersection)
            {
                // TODO: SFX HORN - aguardando interseção (opcional, volume menor)
            }
        }
    }

    void ClearBlock()
    {
        blockKind = BlockKind.None;
        blockTimer = 0f;
    }
}
