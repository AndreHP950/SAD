using UnityEngine;

[RequireComponent(typeof(Animator))]
public class ChickenAnimationController : MonoBehaviour
{
    private Animator animator;

    // Hashes para performance
    private readonly int isRunningHash = Animator.StringToHash("IsRunning");

    [Header("Nomes dos Estados (devem bater com o Animator)")]
    [SerializeField] private string idleInteractionStateName = "Idle_Interaction";
    [SerializeField] private string idleRunStateName = "Idle_Run";

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        // Começa no estado de idle (ciscando)
        SetRunning(false);
    }

    /// <summary>
    /// Chamado quando o minigame começa para trocar para animação de corrida.
    /// </summary>
    public void StartRunning()
    {
        SetRunning(true);
    }

    /// <summary>
    /// Chamado para voltar ao idle (ciscando).
    /// </summary>
    public void StopRunning()
    {
        SetRunning(false);
    }

    private void SetRunning(bool isRunning)
    {
        if (animator != null)
        {
            animator.SetBool(isRunningHash, isRunning);
        }
    }

    /// <summary>
    /// Verifica se está no estado de corrida.
    /// </summary>
    public bool IsRunning()
    {
        if (animator == null) return false;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        return st.IsName(idleRunStateName);
    }

    /// <summary>
    /// Verifica se está no estado de idle (ciscando).
    /// </summary>
    public bool IsIdling()
    {
        if (animator == null) return false;

        var st = animator.GetCurrentAnimatorStateInfo(0);
        return st.IsName(idleInteractionStateName);
    }
}