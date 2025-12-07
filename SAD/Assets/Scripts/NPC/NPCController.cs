using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NPCController : MonoBehaviour
{
    [System.Serializable]
    public class Waypoint
    {
        public Vector3 position;
        public float waitTime = 0f; // Tempo de espera opcional no waypoint
    }

    [Header("Waypoints")]
    [Tooltip("Lista de pontos que o NPC vai percorrer. Use posições locais relativas ao NPC.")]
    public List<Waypoint> waypoints = new List<Waypoint>();
    [Tooltip("Se true, usa posições do mundo. Se false, usa posições relativas à posição inicial do NPC.")]
    public bool useWorldPositions = false;

    [Header("Movimento")]
    [Tooltip("Velocidade de caminhada do NPC.")]
    public float walkSpeed = 2f;
    [Tooltip("Velocidade de rotação do NPC.")]
    public float rotationSpeed = 5f;
    [Tooltip("Distância para considerar que chegou no waypoint.")]
    public float arrivalDistance = 0.3f;
    [Tooltip("Altura do NPC em relação ao chão.")]
    public float heightOffset = 0f;
    [Tooltip("Layer do chão para snap.")]
    public LayerMask groundMask = 1;

    [Header("Referências")]
    [Tooltip("Referência ao Animator do modelo 3D. Se vazio, procura nos filhos.")]
    public Animator animator;

    [Header("Animação")]
    [Tooltip("Nome do parâmetro Speed no Animator.")]
    public string speedParameterName = "Speed";
    [Tooltip("Nome do parâmetro IsWalking no Animator (bool).")]
    public string isWalkingParameterName = "IsWalking";
    [Tooltip("Usar parâmetro float (Speed) ou bool (IsWalking).")]
    public bool useSpeedParameter = true;

    [Header("Áudio")]
    [Tooltip("Nome do SFX no AudioManager para este NPC.")]
    public string sfxName = "NPC_Sound";
    [Tooltip("Raio da esfera de detecção do player.")]
    public float soundTriggerRadius = 5f;
    [Tooltip("Cooldown entre tocar o som novamente (segundos).")]
    public float soundCooldown = 10f;
    [Tooltip("Volume do SFX (0-1).")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Debug")]
    public bool showGizmos = true;
    public Color waypointColor = Color.cyan;
    public Color pathColor = Color.yellow;
    public Color triggerRadiusColor = Color.green;

    // Estado interno
    private AudioSource audioSource;
    private Vector3 startPosition;
    private int currentWaypointIndex = 0;
    private bool movingForward = true;
    private bool isWaiting = false;
    private float waitTimer = 0f;
    private float lastSoundTime = -100f;
    private bool playerInRange = false;

    // Cache dos waypoints convertidos para world position
    private List<Vector3> worldWaypoints = new List<Vector3>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Busca o Animator nos filhos se não foi definido
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            Debug.LogWarning($"[NPCController] {name}: Animator não encontrado nos filhos!");
        }

        // Configura o AudioSource para 3D
        if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }
    }

    void Start()
    {
        startPosition = transform.position;

        // Converte waypoints para posições do mundo
        ConvertWaypointsToWorld();

        if (worldWaypoints.Count == 0)
        {
            Debug.LogWarning($"[NPCController] {name}: Nenhum waypoint definido. NPC ficará parado.");
        }
    }

    void Update()
    {
        if (worldWaypoints.Count < 2)
        {
            SetWalking(false);
            return;
        }

        // Verifica se está esperando
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                AdvanceToNextWaypoint();
            }
            SetWalking(false);
            return;
        }

        // Move para o waypoint atual
        MoveTowardsWaypoint();
    }

    void FixedUpdate()
    {
        CheckPlayerInRange();
    }

    private void ConvertWaypointsToWorld()
    {
        worldWaypoints.Clear();

        foreach (var wp in waypoints)
        {
            Vector3 worldPos = useWorldPositions ? wp.position : startPosition + wp.position;
            worldWaypoints.Add(worldPos);
        }
    }

    private void MoveTowardsWaypoint()
    {
        if (currentWaypointIndex < 0 || currentWaypointIndex >= worldWaypoints.Count)
        {
            SetWalking(false);
            return;
        }

        Vector3 targetPos = GetGroundPosition(worldWaypoints[currentWaypointIndex]);
        Vector3 currentPos = transform.position;

        // Direção para o waypoint (ignorando Y para rotação)
        Vector3 direction = targetPos - currentPos;
        direction.y = 0;

        float distance = direction.magnitude;

        // Chegou no waypoint?
        if (distance <= arrivalDistance)
        {
            OnReachedWaypoint();
            return;
        }

        // Rotaciona em direção ao waypoint
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move na direção do forward do NPC
        Vector3 moveDirection = transform.forward * walkSpeed * Time.deltaTime;
        Vector3 newPos = currentPos + moveDirection;

        // Snap ao chão
        newPos = GetGroundPosition(newPos);
        transform.position = newPos;

        SetWalking(true);
    }

    private void OnReachedWaypoint()
    {
        // Verifica se tem tempo de espera
        if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count)
        {
            float waitTime = waypoints[currentWaypointIndex].waitTime;
            if (waitTime > 0f)
            {
                isWaiting = true;
                waitTimer = waitTime;
                SetWalking(false);
                return;
            }
        }

        AdvanceToNextWaypoint();
    }

    private void AdvanceToNextWaypoint()
    {
        // Movimento em ping-pong (1 -> 2 -> 3 -> 2 -> 1 -> 2 -> ...)
        if (movingForward)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= worldWaypoints.Count)
            {
                currentWaypointIndex = worldWaypoints.Count - 2;
                movingForward = false;

                if (currentWaypointIndex < 0)
                    currentWaypointIndex = 0;
            }
        }
        else
        {
            currentWaypointIndex--;
            if (currentWaypointIndex < 0)
            {
                currentWaypointIndex = 1;
                movingForward = true;

                if (currentWaypointIndex >= worldWaypoints.Count)
                    currentWaypointIndex = worldWaypoints.Count - 1;
            }
        }
    }

    private Vector3 GetGroundPosition(Vector3 position)
    {
        Vector3 rayOrigin = position + Vector3.up * 2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 5f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * heightOffset;
        }

        return position;
    }

    private void SetWalking(bool walking)
    {
        if (animator == null) return;

        if (useSpeedParameter)
        {
            animator.SetFloat(speedParameterName, walking ? 1f : 0f);
        }
        else
        {
            animator.SetBool(isWalkingParameterName, walking);
        }
    }

    private void CheckPlayerInRange()
    {
        // Verifica cooldown
        if (Time.time - lastSoundTime < soundCooldown) return;

        // Busca o player por tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= soundTriggerRadius;

        // Se o player acabou de entrar no range, toca o som
        if (playerInRange && !wasInRange)
        {
            PlaySound();
        }
    }

    private void PlaySound()
    {
        if (string.IsNullOrEmpty(sfxName)) return;

        lastSoundTime = Time.time;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(audioSource, sfxName);
        }
    }

    // ========== MÉTODOS PÚBLICOS PARA EDITOR ==========

    public void AddWaypointAtCurrentPosition()
    {
        Waypoint wp = new Waypoint();

        if (useWorldPositions)
        {
            wp.position = transform.position;
        }
        else
        {
            wp.position = Vector3.zero;
        }

        waypoints.Add(wp);
    }

    public void AddWaypoint(Vector3 position, float waitTime = 0f)
    {
        waypoints.Add(new Waypoint { position = position, waitTime = waitTime });
    }

    // ========== GIZMOS ==========

    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Desenha o raio de som
        Gizmos.color = new Color(triggerRadiusColor.r, triggerRadiusColor.g, triggerRadiusColor.b, 0.2f);
        Gizmos.DrawSphere(transform.position, soundTriggerRadius);
        Gizmos.color = triggerRadiusColor;
        Gizmos.DrawWireSphere(transform.position, soundTriggerRadius);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        if (waypoints == null || waypoints.Count == 0) return;

        Vector3 basePos = Application.isPlaying ? startPosition : transform.position;

        // Desenha os waypoints
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 worldPos = useWorldPositions ? waypoints[i].position : basePos + waypoints[i].position;

            // Esfera no waypoint
            Gizmos.color = waypointColor;
            Gizmos.DrawSphere(worldPos, 0.3f);

            // Linha para o próximo waypoint
            if (i < waypoints.Count - 1)
            {
                Vector3 nextWorldPos = useWorldPositions ? waypoints[i + 1].position : basePos + waypoints[i + 1].position;
                Gizmos.color = pathColor;
                Gizmos.DrawLine(worldPos, nextWorldPos);

                // Seta indicando direção
                Vector3 dir = (nextWorldPos - worldPos).normalized;
                Vector3 midPoint = (worldPos + nextWorldPos) / 2f;
                Gizmos.DrawRay(midPoint, dir * 0.5f);
            }
        }

        // Desenha linha de volta (ping-pong) com cor mais clara
        if (waypoints.Count >= 2)
        {
            Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.4f);
            for (int i = waypoints.Count - 1; i > 0; i--)
            {
                Vector3 pos = useWorldPositions ? waypoints[i].position : basePos + waypoints[i].position;
                Vector3 prevPos = useWorldPositions ? waypoints[i - 1].position : basePos + waypoints[i - 1].position;

                // Linha pontilhada de volta
                Vector3 offset = Vector3.up * 0.1f; // Pequeno offset para não sobrepor
                Gizmos.DrawLine(pos + offset, prevPos + offset);
            }
        }
    }
}