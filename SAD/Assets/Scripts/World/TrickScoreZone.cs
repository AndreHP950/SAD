using UnityEngine;

[DefaultExecutionOrder(0)]
public class TrickScoreZone : MonoBehaviour
{
    [Header("Hitbox (OBB)")]
    public Vector3 boxCenter = Vector3.zero;
    public Vector3 boxSize = new Vector3(3f, 2f, 4f);

    [Header("Pontuação")]
    public int scoreAmount = 100;
    [Tooltip("Arraste o ScoreController da cena aqui. Se vazio, tenta localizar automaticamente.")]
    public ScoreController scoreController;

    [Header("Rearme")]
    [Tooltip("Se true, pontua apenas uma vez e desativa. Se false, rearma após o delay.")]
    public bool onlyOnce = false;
    [Tooltip("Tempo (segundos) para rearme após pontuar (quando onlyOnce = false).")]
    public float rearmDelay = 2f;

    // Interno
    Transform player;
    bool wasInside = false;
    bool armed = true;
    float armReadyTime = 0f;

    void Awake()
    {
        var go = GameObject.FindWithTag("Player");
        if (go != null) player = go.transform;

        EnsureScoreControllerBound();
    }

    void Update()
    {
        if (player == null) return;

        // Rearme por tempo (usa unscaled para funcionar mesmo em pausa)
        if (!armed && Time.unscaledTime >= armReadyTime)
            armed = true;

        bool inside = IsInsideOBB(player.position);

        // Borda de subida: entrou na caixa
        if (armed && inside && !wasInside)
        {
            AddScore();

            if (onlyOnce)
            {
                armed = false;
                enabled = false; // desativa este componente de vez
            }
            else
            {
                armed = false;
                armReadyTime = Time.unscaledTime + rearmDelay;
            }
        }

        wasInside = inside;
    }

    void AddScore()
    {
        if (scoreAmount == 0) return;

        if (scoreController == null)
            EnsureScoreControllerBound();

        if (scoreController != null)
        {
            scoreController.ChangeScore(scoreAmount);
        }
        else
        {
            Debug.LogWarning("[TrickScoreZone] ScoreController não encontrado. Arraste a referência no Inspector.");
        }
    }

    void EnsureScoreControllerBound()
    {
        if (scoreController != null) return;
        // Tenta localizar automaticamente um ScoreController na cena
#if UNITY_2023_1_OR_NEWER
        scoreController = Object.FindAnyObjectByType<ScoreController>();
#else
        scoreController = Object.FindObjectOfType<ScoreController>();
#endif
    }

    bool IsInsideOBB(Vector3 worldPoint)
    {
        // Converte para espaço local do objeto (respeita posição/rotação/escala)
        Vector3 local = transform.InverseTransformPoint(worldPoint) - boxCenter;
        Vector3 half = boxSize * 0.5f;
        return Mathf.Abs(local.x) <= half.x
            && Mathf.Abs(local.y) <= half.y
            && Mathf.Abs(local.z) <= half.z;
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
        var prevColor = Gizmos.color;
        var prevMatrix = Gizmos.matrix;

        // Usa a matriz completa do objeto (inclui rotação e escala)
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.25f);
        Gizmos.DrawCube(boxCenter, boxSize);
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(boxCenter, boxSize);

        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }
}