using UnityEngine;

[DefaultExecutionOrder(0)]
public class TrickScoreZone : MonoBehaviour
{
    [Header("Zona de Pontuação (Trigger)")]
    [Tooltip("O centro da zona de trigger, relativo à posição do objeto.")]
    public Vector3 triggerCenter = Vector3.zero;
    [Tooltip("O tamanho da zona de trigger.")]
    public Vector3 triggerSize = new Vector3(3f, 2f, 4f);

    [Header("Pontuação")]
    public int scoreAmount = 100;
    [Tooltip("Arraste o ScoreController da cena aqui. Se vazio, tenta localizar automaticamente.")]
    public ScoreController scoreController;

    [Header("Rearme")]
    [Tooltip("Se true, pontua apenas uma vez e desativa. Se false, rearma após o delay.")]
    public bool onlyOnce = false;
    [Tooltip("Tempo (segundos) para rearme após pontuar (quando onlyOnce = false).")]
    public float rearmDelay = 2f;

    [Header("Feedback Visual")]
    [Tooltip("ParticleSystem do VFX para tocar quando o jogador pontua. Deixe vazio se não houver.")]
    public ParticleSystem scoreVFX;

    // Interno
    private bool armed = true;
    private float armReadyTime = 0f;
    private BoxCollider triggerCollider; // Referência para nosso collider de trigger específico

    void Awake()
    {
        SetupTriggerCollider();
        EnsureScoreControllerBound();

        // Garante que o VFX não comece tocando sozinho
        if (scoreVFX != null)
        {
            scoreVFX.gameObject.SetActive(true); // Garante que o objeto está ativo
            scoreVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Para e limpa partículas
        }
    }

    void OnValidate()
    {
        // Isso permite que o gizmo e o collider se atualizem no editor
        // quando você muda os valores de triggerCenter e triggerSize.
        SetupTriggerCollider();
    }

    void SetupTriggerCollider()
    {
        BoxCollider[] colliders = GetComponents<BoxCollider>();
        triggerCollider = null;

        // 1. Tenta encontrar um BoxCollider que já esteja configurado como trigger.
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }

        // 2. Se nenhum for encontrado, adiciona um novo para não mexer no collider físico.
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }

        // 3. Aplica as propriedades do script ao collider de trigger.
        triggerCollider.center = triggerCenter;
        triggerCollider.size = triggerSize;
    }

    void Update()
    {
        // Rearme por tempo (usa unscaled para funcionar mesmo em pausa)
        if (!armed && !onlyOnce && Time.unscaledTime >= armReadyTime)
        {
            armed = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou é o jogador e se a zona está armada
        if (armed && other.CompareTag("Player"))
        {
            AddScore();

            if (onlyOnce)
            {
                armed = false;
                // Desativa apenas o collider de trigger para não ser acionado novamente
                if (triggerCollider != null) triggerCollider.enabled = false;
            }
            else
            {
                armed = false;
                armReadyTime = Time.unscaledTime + rearmDelay;
            }
        }
    }

    void AddScore()
    {
        if (scoreAmount == 0) return;

        // Toca o VFX antes de qualquer outra coisa para feedback imediato
        if (scoreVFX != null)
        {
            scoreVFX.Play();
        }

        if (scoreController == null)
            EnsureScoreControllerBound();

        if (scoreController != null)
        {
            scoreController.ChangeScore(scoreAmount);
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("DeliverySuccess");
        }
        else
        {
            Debug.LogWarning("[TrickScoreZone] ScoreController não encontrado. Arraste a referência no Inspector.");
        }
    }

    void EnsureScoreControllerBound()
    {
        if (scoreController != null) return;
        scoreController = FindFirstObjectByType<ScoreController>();
    }

    void OnDrawGizmos()
    {
        DrawGizmo();
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmo();
    }

    void DrawGizmo()
    {
        // Garante que temos uma referência, mesmo no modo de edição
        if (triggerCollider == null)
        {
            SetupTriggerCollider();
        }

        var prevColor = Gizmos.color;
        var prevMatrix = Gizmos.matrix;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = armed ? new Color(0.2f, 0.9f, 0.3f, 0.25f) : new Color(0.9f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawCube(triggerCenter, triggerSize); // Usa as variáveis do script
        Gizmos.color = armed ? new Color(0.2f, 0.9f, 0.3f, 0.9f) : new Color(0.9f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(triggerCenter, triggerSize);

        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }
}