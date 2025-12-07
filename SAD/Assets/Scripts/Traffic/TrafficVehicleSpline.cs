using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
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
    [Tooltip("Quanto mais alto, mais rápido gira para alinhar ao tangente da rota.")]
    public float turnLerp = 10f;
    public float acceleration = 8f;
    readonly float comfortableDeceleration = 8f;

    [Header("Sensores")]
    [Tooltip("Layers que podem ser detectadas (Player, Vehicles, Obstacles).")]
    public LayerMask obstacleMask = ~0;
    public SensorShape sensorShape = SensorShape.Box;

    [Tooltip("Ponto opcional para origem do sensor (deixe null para usar offset).")]
    public Transform sensorOrigin;
    [Tooltip("Comprimento do sensor à frente (representa o espaço de segurança).")]
    public float sensorLength = 10f;
    [Tooltip("Largura/altura do sensor (Box) ou raio (Sphere).")]
    public Vector2 sensorSize = new Vector2(1.2f, 0.9f);
    [Tooltip("Offset para frente quando não há sensorOrigin.")]
    public float sensorForwardOffset = 1.2f;
    [Tooltip("Offset para cima do sensor.")]
    public float sensorUpOffset = 0.5f;

    [Header("Interseções/Paradas")]
    public bool obeyIntersections = true;
    public bool obeyStopZones = true;

    [Header("Anti-Deadlock (geral)")]
    public float minSpeedToConsiderMoving = 0.25f;
    public float stuckTimeout = 4f;
    public float courtesyNudgeSpeed = 0.6f;

    [Header("Anti-Travamento em Curvas")]
    [Tooltip("Tempo máximo parado antes de avançar mesmo assim.")]
    public float maxWaitWhenBlockedByVehicle = 2.0f;
    [Tooltip("Velocidade usada para 'forçar a passagem' após o tempo de espera.")]
    public float forceProceedSpeed = 1.0f;

    [Header("Anti-Travamento em Interseções")]
    [Tooltip("Tempo máximo preso em uma interseção antes de forçar passagem (failsafe).")]
    public float intersectionForceTimeout = 10f;
    [Tooltip("Velocidade usada para forçar saída da interseção.")]
    public float intersectionForceSpeed = 2f;

    [Header("Suspensão (Inclinação ao Freiar)")]
    [Tooltip("Ângulo máximo de inclinação para frente ao freiar (em graus).")]
    public float maxBrakePitchAngle = 3f;
    [Tooltip("Velocidade com que a inclinação acontece.")]
    public float pitchLerpSpeed = 8f;

    [Header("VFX: Fumaça de Frenagem")]
    [Tooltip("GameObject do VFX de fumaça/trail para frenagem. Deixe vazio se não quiser usar.")]
    public GameObject brakeSmokVFX;
    [Tooltip("Desaceleração mínima para ativar a fumaça de frenagem.")]
    public float brakeSmokMinDecel = 5f;
    private ParticleSystem brakeSmokParticle;
    private bool brakeSmokEmitting = false;

    [Header("Áudio")]
    [Tooltip("Tempo que o player precisa ficar na frente para a buzina começar.")]
    public float timeToStartHonking = 2.0f;

    [Header("Player Push (Campo Magnético)")]
    [Tooltip("Força horizontal do empurrão no player.")]
    public float playerPushForce = 8f;
    [Tooltip("Força vertical do empurrão (para cima).")]
    public float playerPushUpForce = 4f;
    [Tooltip("Cooldown entre empurrões (evita spam).")]
    public float pushCooldown = 0.5f;

    [Header("Ground Snap")]
    [Tooltip("Layer(s) do terreno/rua.")]
    public LayerMask groundMask = 1 << 0;
    public float rideHeight = 0.35f;
    public float groundRayLength = 3f;
    public bool alignToGroundNormal = false;
    public Transform visualRoot;

    [Header("Parada Física")]
    public float hardBrakeDamping = 2.0f;

    [Header("Layers por nome (para identificar Player)")]
    public string playerLayerName = "Player";

    [Header("Anti-Interpenetração")]
    public LayerMask depenetrationMask;
    public float depenetrationPadding = 0.01f;
    public float depenProbeRadiusScale = 1.1f;

    // Estado
    float distanceOnRoute;
    float currentSpeed;
    float previousSpeed; // Para calcular a desaceleração
    float currentDeceleration; // Desaceleração atual para uso em múltiplos lugares
    float currentPitchAngle = 0f; // Ângulo atual de inclinação
    IntersectionZone currentIntersection;
    bool intersectionGranted;
    bool inStopRoutine;
    float stuckTimer;

    BlockKind blockKind = BlockKind.None;
    float blockTimer = 0f;

    // Timer para forçar saída de interseção
    float intersectionStuckTimer = 0f;
    bool forcingIntersectionExit = false;

    bool touchingSomething;
    bool touchingPlayerOrVehicle;

    Rigidbody rb;
    Collider bodyCol;
    int playerLayer = -1;
    AudioSource hornSource; // AudioSource dedicado 3d
    bool isBrakingAudioPlayed = false;

    // Timers para a nova lógica da buzina
    float stationaryTimer = 0f;
    float nextHonkTimer = 0f;

    // Cooldown do empurrão
    float lastPushTime = -10f;

    readonly Collider[] depenBuffer = new Collider[16];
    readonly RaycastHit[] sensorBuffer = new RaycastHit[16];

    public float DistanceOnRoute => distanceOnRoute;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        bodyCol = GetComponent<Collider>();
        hornSource = GetComponent<AudioSource>(); // Pega o AudioSource para a buzina

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        playerLayer = LayerMask.NameToLayer(playerLayerName);

        if (depenetrationMask == 0)
        {
            int vehicleLayer = gameObject.layer;
            depenetrationMask = (1 << vehicleLayer);
            if (playerLayer >= 0) depenetrationMask |= (1 << playerLayer);
        }
    }

    void Start()
    {
        if (route == null || route.totalLength <= 0f)
        {
            Debug.LogWarning($"{name}: route não configurada ou sem comprimento.");
            enabled = false; return;
        }
        distanceOnRoute = Mathf.Repeat(startDistance, route.totalLength);
        previousSpeed = 0f;
        UpdateTransformImmediate();

        // Inicializa o ParticleSystem da fumaça de frenagem
        if (brakeSmokVFX != null)
        {
            brakeSmokParticle = brakeSmokVFX.GetComponentInChildren<ParticleSystem>();
            if (brakeSmokParticle != null)
            {
                var emission = brakeSmokParticle.emission;
                emission.enabled = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (route == null || route.totalLength <= 0f) return;

        float dt = Time.fixedDeltaTime;
        float desiredSpeed = maxSpeed;

        touchingSomething = false;
        touchingPlayerOrVehicle = false;

        // --- Lógica de forçar saída de interseção (failsafe) ---
        HandleIntersectionStuckTimer(dt);

        // 1) Interseções
        if (obeyIntersections && currentIntersection != null && !intersectionGranted && !forcingIntersectionExit)
        {
            desiredSpeed = 0f;
            SetBlock(BlockKind.Intersection, dt);
        }

        // 2) Stop zones
        if (inStopRoutine)
        {
            desiredSpeed = 0f;
            SetBlock(BlockKind.StopZone, dt);
        }

        // 3) Sensor frontal (distância e reação)
        bool hit = SenseObstacle(out float obsDistRaw, out float obsSpeed, out BlockKind hitKind, out TrafficVehicleSpline hitVehicle);
        float obsDist = obsDistRaw;

        // Espaço dinâmico proporcional à velocidade
        float desiredGap = Mathf.Lerp(sensorLength * 0.5f, sensorLength, currentSpeed / maxSpeed);

        bool isOppositeDirection = false;
        if (hit && hitVehicle != null)
        {
            // Checa se os veículos estão em sentidos opostos
            Vector3 myFwd = transform.forward;
            myFwd.y = 0;
            Vector3 otherFwd = hitVehicle.transform.forward;
            otherFwd.y = 0;
            if (Vector3.Dot(myFwd.normalized, otherFwd.normalized) < -0.5f)
            {
                isOppositeDirection = true;
            }

            // Se estiverem na mesma rota, usa a distância ao longo da spline (mais preciso)
            if (hitVehicle.route == this.route)
            {
                float otherDist = hitVehicle.DistanceOnRoute;
                float along = otherDist - distanceOnRoute;
                if (route.loop)
                    along = Mathf.Repeat(along, route.totalLength);
                else if (along <= 0f)
                    along = float.MaxValue;
                if (along != float.MaxValue)
                    obsDist = Mathf.Max(0f, along);
            }
        }

        if (hit)
        {
            // Se estamos forçando saída da interseção, ignoramos bloqueios de outros veículos
            if (forcingIntersectionExit && hitKind == BlockKind.Vehicle)
            {
                // Continua forçando, mas com velocidade reduzida
                desiredSpeed = Mathf.Min(desiredSpeed, intersectionForceSpeed);
            }
            else
            {
                SetBlock(hitKind, dt);
                float gap = obsDist - desiredGap;

                if (gap <= 0.01f)
                {
                    desiredSpeed = 0f;
                }
                else
                {
                    float safeDecelDist = Mathf.Clamp(gap, 0.5f, sensorLength);
                    float vAllowed = Mathf.Sqrt(2f * comfortableDeceleration * safeDecelDist);
                    if (obsSpeed >= 0f)
                        vAllowed = Mathf.Min(vAllowed, obsSpeed);
                    desiredSpeed = Mathf.Min(desiredSpeed, vAllowed);
                }
            }
        }
        else
        {
            ClearBlock();
        }

        // Lógica de Áudio para Buzina e Freio
        HandleAudio(hitKind, dt);

        // 4) Evita empurrar fisicamente (exceto se forçando saída)
        if (touchingSomething && touchingPlayerOrVehicle && !forcingIntersectionExit)
        {
            desiredSpeed = 0f;
        }

        // 5) Anti-deadlock
        if (currentSpeed < minSpeedToConsiderMoving && desiredSpeed <= 0.05f)
        {
            stuckTimer += dt;
            if (stuckTimer >= stuckTimeout)
            {
                // Deadlock em cruzamento: se eu sou o primeiro da fila, começo a me mover lentamente.
                if (blockKind == BlockKind.Intersection && currentIntersection != null && currentIntersection.IsFirstInQueue(this))
                {
                    desiredSpeed = Mathf.Max(desiredSpeed, courtesyNudgeSpeed);
                }
                // Deadlock com veículo em sentido oposto
                else if (blockKind == BlockKind.Vehicle && isOppositeDirection)
                {
                    desiredSpeed = Mathf.Max(desiredSpeed, courtesyNudgeSpeed);
                }
                // Deadlock com obstáculo genérico
                else if (blockKind != BlockKind.Player && blockKind != BlockKind.Vehicle)
                {
                    desiredSpeed = courtesyNudgeSpeed;
                }
                stuckTimer = 0f;
            }
        }
        else stuckTimer = 0f;

        // 6) Forçar passagem após timeout: Só ativa se o outro veículo estiver em sentido OPOSTO.
        if (blockKind == BlockKind.Vehicle && blockTimer >= maxWaitWhenBlockedByVehicle && isOppositeDirection)
        {
            desiredSpeed = Mathf.Max(desiredSpeed, forceProceedSpeed);
        }

        // 7) Se estamos forçando saída da interseção, garante velocidade mínima
        if (forcingIntersectionExit)
        {
            desiredSpeed = Mathf.Max(desiredSpeed, intersectionForceSpeed);
        }

        // Integração da velocidade
        float accel = (desiredSpeed >= currentSpeed) ? acceleration : comfortableDeceleration;
        // Toca som de freio brusco
        if (desiredSpeed <= 0.001f && currentSpeed > 1.0f)
        {
            accel *= hardBrakeDamping;
        }

        // Guarda velocidade anterior para calcular desaceleração
        previousSpeed = currentSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * dt);

        // Calcula a desaceleração atual (usado para pitch e VFX)
        currentDeceleration = Mathf.Max(0f, (previousSpeed - currentSpeed) / dt);

        // --- Calcula inclinação de frenagem (suspensão) ---
        float targetPitchAngle = CalculateBrakePitch();
        currentPitchAngle = Mathf.Lerp(currentPitchAngle, targetPitchAngle, pitchLerpSpeed * dt);

        // --- Controla VFX de fumaça de frenagem ---
        HandleBrakeSmokeVFX();

        // Avança na rota
        distanceOnRoute += currentSpeed * dt;
        if (route.loop) distanceOnRoute = Mathf.Repeat(distanceOnRoute, route.totalLength);
        else distanceOnRoute = Mathf.Clamp(distanceOnRoute, 0f, route.totalLength);

        // Pose pela spline
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

        Vector3 proposed = Vector3.Lerp(rb.position, finalPos, 1f - Mathf.Exp(-12f * Time.fixedDeltaTime));
        proposed = ResolveDepenetration(proposed, rb.rotation);

        // Rotação base (direção da rota)
        Quaternion baseRot = Quaternion.LookRotation(forward, up);

        // Aplica inclinação de frenagem (pitch) no eixo local X
        // Ângulo POSITIVO = frente para baixo (nariz mergulha)
        Quaternion pitchRot = Quaternion.AngleAxis(currentPitchAngle, baseRot * Vector3.right);
        Quaternion finalRot = pitchRot * baseRot;

        Quaternion targetRot = Quaternion.Slerp(rb.rotation, finalRot, 1f - Mathf.Exp(-turnLerp * Time.fixedDeltaTime));
        rb.MoveRotation(targetRot);
        rb.MovePosition(proposed);

        if (desiredSpeed <= 0.001f)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Mantém leve afastamento quando parado
            if (hitVehicle != null)
            {
                float tooClose = Mathf.Max(0f, desiredGap - obsDist);
                if (tooClose > 0.01f)
                {
                    Vector3 back = -transform.forward * (tooClose * 0.2f);
                    rb.MovePosition(rb.position + back);
                }
            }
        }
    }

    private float CalculateBrakePitch()
    {
        // Normaliza a desaceleração em relação à desaceleração máxima esperada
        float maxDecel = comfortableDeceleration * hardBrakeDamping;
        float decelRatio = Mathf.Clamp01(currentDeceleration / maxDecel);

        // Se está acelerando ou em velocidade constante, volta ao normal
        if (currentDeceleration <= 0.1f)
        {
            return 0f;
        }

        // Quanto mais forte a frenagem, maior a inclinação para frente (ângulo positivo = frente para baixo)
        return maxBrakePitchAngle * decelRatio;
    }

    private void HandleBrakeSmokeVFX()
    {
        if (brakeSmokParticle == null) return;

        // Ativa fumaça se está freando forte o suficiente
        bool shouldEmit = currentDeceleration >= brakeSmokMinDecel;

        if (shouldEmit != brakeSmokEmitting)
        {
            var emission = brakeSmokParticle.emission;
            emission.enabled = shouldEmit;
            brakeSmokEmitting = shouldEmit;
        }
    }

    private void HandleIntersectionStuckTimer(float dt)
    {
        // Se está dentro de uma interseção e parado
        if (currentIntersection != null && currentSpeed < minSpeedToConsiderMoving)
        {
            intersectionStuckTimer += dt;

            // Se passou do timeout, força a saída
            if (intersectionStuckTimer >= intersectionForceTimeout && !forcingIntersectionExit)
            {
                forcingIntersectionExit = true;
                Debug.Log($"{name}: Forçando saída da interseção após {intersectionForceTimeout}s preso.");
            }
        }
        else
        {
            // Se saiu da interseção ou está se movendo, reseta
            if (currentIntersection == null)
            {
                intersectionStuckTimer = 0f;
                forcingIntersectionExit = false;
            }
            else if (currentSpeed >= minSpeedToConsiderMoving)
            {
                // Está se movendo dentro da interseção, reseta o timer mas mantém o estado
                intersectionStuckTimer = 0f;
            }
        }
    }

    private void HandleAudio(BlockKind currentHit, float dt)
    {

        bool isCurrentlyBrakingForPlayer = currentHit == BlockKind.Player && currentSpeed > 1.0f && IsBrakingHard();
        if (isCurrentlyBrakingForPlayer)
        {
            if (!isBrakingAudioPlayed)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(hornSource, "CarBreak");
                isBrakingAudioPlayed = true;
            }
        }
        else
        {
            isBrakingAudioPlayed = false;
        }


        // Se o veículo está parado
        if (currentSpeed < minSpeedToConsiderMoving)
        {
            stationaryTimer += dt;

            // Se ficou parado por mais de 3 segundos
            if (stationaryTimer > 3f)
            {
                nextHonkTimer -= dt;

                if (nextHonkTimer <= 0)
                {
                    // Toca a buzina
                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlaySFX(hornSource, "CarHorn");

                    // Reseta o timer para a próxima buzina com um valor aleatório
                    nextHonkTimer = Random.Range(4f, 6f);
                }
            }
        }
        else
        {
            // Se o veículo começou a se mover, reseta os timers
            stationaryTimer = 0f;
            nextHonkTimer = 0f;
        }
    }

    private bool IsBrakingHard()
    {
        // Considera que está freando se a velocidade desejada for quase zero
        // e a velocidade atual ainda for significativa.
        return maxSpeed > 0 && (currentSpeed / maxSpeed) > 0.1f;
    }

    Vector3 ResolveDepenetration(Vector3 proposedPos, Quaternion proposedRot)
    {
        float probeRadius = bodyCol.bounds.extents.magnitude * depenProbeRadiusScale;
        int count = Physics.OverlapSphereNonAlloc(proposedPos, probeRadius, depenBuffer, depenetrationMask, QueryTriggerInteraction.Ignore);
        if (count <= 0) return proposedPos;

        for (int i = 0; i < count; i++)
        {
            var other = depenBuffer[i];
            if (other == null || other == bodyCol) continue;

            var otherRb = other.attachedRigidbody;
            Vector3 otherPos = otherRb ? otherRb.position : other.transform.position;
            Quaternion otherRot = otherRb ? otherRb.rotation : other.transform.rotation;

            if (Physics.ComputePenetration(bodyCol, proposedPos, proposedRot, other, otherPos, otherRot, out Vector3 dir, out float dist))
            {
                proposedPos += dir * (dist + depenetrationPadding);
            }
        }
        return proposedPos;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Empurra o player quando encosta no carro
        if (collision.collider.gameObject.layer == playerLayer && playerLayer >= 0)
        {
            PushPlayer(collision);
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

            // Continua empurrando o player se ainda estiver em contato (com cooldown)
            if (isPlayerCol)
            {
                PushPlayer(collision);
            }
        }
    }

    /// <summary>
    /// Empurra o player para longe do carro (efeito "campo magnético").
    /// </summary>
    private void PushPlayer(Collision collision)
    {
        // Verifica cooldown
        if (Time.time - lastPushTime < pushCooldown) return;
        lastPushTime = Time.time;

        // Tenta pegar o CharacterController do player
        CharacterController playerCC = collision.collider.GetComponent<CharacterController>();
        if (playerCC == null)
            playerCC = collision.collider.GetComponentInParent<CharacterController>();

        if (playerCC == null) return;

        // Calcula direção do empurrão: do centro do carro para o player
        Vector3 carCenter = transform.position;
        Vector3 playerPos = playerCC.transform.position;
        Vector3 pushDirection = (playerPos - carCenter);
        pushDirection.y = 0; // Ignora Y para direção horizontal
        pushDirection.Normalize();

        // Se a direção for muito pequena (player bem no centro), usa a direção oposta ao forward do carro
        if (pushDirection.sqrMagnitude < 0.01f)
        {
            pushDirection = -transform.forward;
            pushDirection.y = 0;
            pushDirection.Normalize();
        }

        // Calcula a força do empurrão baseada na velocidade do carro
        float speedFactor = Mathf.Clamp01(currentSpeed / maxSpeed) + 0.5f; // Mínimo 0.5, máximo 1.5
        
        // Vetor final do empurrão
        Vector3 pushVector = pushDirection * playerPushForce * speedFactor;
        pushVector.y = playerPushUpForce; // Adiciona componente vertical

        // Aplica o movimento via PlayerMovementThirdPerson se disponível
        PlayerMovementThirdPerson playerMovement = playerCC.GetComponent<PlayerMovementThirdPerson>();
        if (playerMovement != null)
        {
            playerMovement.ApplyExternalForce(pushVector);
        }
        else
        {
            // Fallback: move diretamente o CharacterController
            playerCC.Move(pushVector * Time.fixedDeltaTime);
        }
    }

    bool SenseObstacle(out float distance, out float obstacleSpeed, out BlockKind hitKind, out TrafficVehicleSpline hitVehicle)
    {
        distance = sensorLength;
        obstacleSpeed = -1f;
        hitKind = BlockKind.None;
        hitVehicle = null;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
        fwd.Normalize();

        Vector3 origin = sensorOrigin ? sensorOrigin.position : transform.position + Vector3.up * sensorUpOffset + fwd * sensorForwardOffset;

        int hitsCount;
        if (sensorShape == SensorShape.Box)
        {
            Vector3 halfExt = new Vector3(sensorSize.x * 0.5f, sensorSize.y * 0.5f, 0.01f);
            hitsCount = Physics.BoxCastNonAlloc(origin, halfExt, fwd, sensorBuffer, transform.rotation, sensorLength, obstacleMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            float radius = Mathf.Max(sensorSize.x, sensorSize.y) * 0.5f;
            hitsCount = Physics.SphereCastNonAlloc(origin, radius, fwd, sensorBuffer, sensorLength, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        if (hitsCount <= 0) return false;

        float bestDist = float.MaxValue;
        RaycastHit bestHit = default;
        for (int i = 0; i < hitsCount; i++)
        {
            var h = sensorBuffer[i];
            if (h.collider == null) continue;
            var parentVehicle = h.collider.GetComponentInParent<TrafficVehicleSpline>();
            if (parentVehicle == this) continue;
            Vector3 toOther = h.collider.bounds.center - origin;
            if (Vector3.Dot(toOther, fwd) < 0f) continue;
            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit = h;
            }
        }

        if (bestDist == float.MaxValue) return false;
        distance = bestDist;

        if (bestHit.collider.gameObject.layer == playerLayer && playerLayer >= 0)
        {
            hitKind = BlockKind.Player;
            return true;
        }

        var other = bestHit.collider.GetComponentInParent<TrafficVehicleSpline>();
        if (other != null)
        {
            obstacleSpeed = other.currentSpeed;
            hitKind = BlockKind.Vehicle;
            hitVehicle = other;
            return true;
        }

        hitKind = BlockKind.Other;
        return true;
    }

    void UpdateTransformImmediate()
    {
        float t = route.DistanceToT(distanceOnRoute);
        Vector3 posOnSpline = route.EvaluatePosition(t);
        Vector3 tan = route.EvaluateTangent(t);
        Vector3 forward = new Vector3(tan.x, 0f, tan.z).normalized;

        Vector3 finalPos = posOnSpline;
        if (Physics.Raycast(posOnSpline + Vector3.up * 1.5f, Vector3.down, out RaycastHit gHit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
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

    public void NotifyEnterIntersection(IntersectionZone zone)
    {
        currentIntersection = zone;
        intersectionGranted = zone != null && zone.HasPriority(this);
    }

    public void GrantIntersection(IntersectionZone zone)
    {
        if (zone == currentIntersection)
            intersectionGranted = true;
    }

    public void NotifyExitIntersection(IntersectionZone zone)
    {
        if (zone == currentIntersection)
        {
            currentIntersection = null;
            intersectionGranted = false;
            // Reseta o estado de forçar saída ao sair da interseção
            intersectionStuckTimer = 0f;
            forcingIntersectionExit = false;
        }
    }

    public void RequestStop(float seconds)
    {
        if (!obeyStopZones) return;
        if (inStopRoutine) return;
        StartCoroutine(StopRoutine(seconds));
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

    void OnDrawGizmosSelected()
    {
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 origin = sensorOrigin ? sensorOrigin.position : transform.position + Vector3.up * sensorUpOffset + fwd * sensorForwardOffset;

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
        }
    }

    void ClearBlock()
    {
        blockKind = BlockKind.None;
        blockTimer = 0f;
    }
}