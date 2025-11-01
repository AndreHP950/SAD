using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class ChaseMinigameTrigger : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Raio da área de detecção do minigame")]
    public float detectionRadius = 3f;
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Referências")]
    [Tooltip("Referência opcional ao controlador do minigame. Se nulo, procura na cena.")]
    public MinigameController controller;

    [Tooltip("Define qual personagem pode ativar (0=Gato, 1=Cachorro)")]
    public int requiredCharacter = 0; // 0=Gato (rato), 1=Cachorro (galinha)

    private bool minigameStarted = false;
    private SphereCollider sphereCollider;

    void Reset()
    {
        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        sphereCollider.radius = detectionRadius;
    }

    void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<MinigameController>();

        sphereCollider = GetComponent<SphereCollider>();
        sphereCollider.isTrigger = true;
        UpdateColliderRadius();
    }

    void OnValidate()
    {
        if (sphereCollider != null)
            UpdateColliderRadius();
    }

    void UpdateColliderRadius()
    {
        sphereCollider.radius = detectionRadius;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = minigameStarted ? Color.red : gizmoColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Desenha linha até o alvo se houver
        var target = GetComponentInParent<ChasableAI>();
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (minigameStarted) return;
        if (!other.CompareTag("Player")) return;
        if (controller == null) return;

        // Verifica se é o personagem correto
        if (GameManager.instance != null && GameManager.instance.character == requiredCharacter)
        {
            var target = GetComponentInParent<ChasableAI>();
            if (target != null)
            {
                minigameStarted = true;
                controller.StartChaseMinigame(target);
            }
        }
    }

    // Chamado quando o minigame termina (pelo RatAI)
    public void OnMinigameEnded()
    {
        minigameStarted = false;
    }
}