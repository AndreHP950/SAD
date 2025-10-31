using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class RatTrigger : MonoBehaviour
{
    [Header("Configura��o")]
    [Tooltip("Raio da �rea de detec��o do minigame")]
    public float detectionRadius = 3f;
    public bool showGizmo = true;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Refer�ncias")]
    [Tooltip("Refer�ncia opcional ao controlador do minigame. Se nulo, procura na cena.")]
    public MinigameController controller;

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

        // Desenha linha at� o rato se houver
        var ratAI = GetComponentInParent<RatAI>();
        if (ratAI != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, ratAI.transform.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (minigameStarted) return;
        if (!other.CompareTag("Player")) return;
        if (controller == null) return;

        // S� inicia se player for gato
        if (GameManager.instance != null && GameManager.instance.character == 0)
        {
            var ratAI = GetComponentInParent<RatAI>();
            if (ratAI != null)
            {
                minigameStarted = true;
                controller.StartRatChase(ratAI);
            }
        }
    }

    // Chamado quando o minigame termina (pelo RatAI)
    public void OnMinigameEnded()
    {
        minigameStarted = false;
    }
}