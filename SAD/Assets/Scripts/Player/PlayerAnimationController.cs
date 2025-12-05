using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private PlayerMovementThirdPerson playerMovement;
    private CharacterController characterController;

    // Hashes
    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int collectRightHash = Animator.StringToHash("CollectRight");
    private readonly int collectLeftHash = Animator.StringToHash("CollectLeft");

    // Nomes dos estados de coleta (precisam bater com o Animator)
    [SerializeField] private string collectRightStateName = "Collect_R";
    [SerializeField] private string collectLeftStateName = "Collect_L";

    // Anti-spam
    [SerializeField] private float collectMinInterval = 0.15f; // em segundos
    private float lastCollectTime = -999f;

    [Header("Idle Detection")]
    [Tooltip("Velocidade abaixo da qual o personagem é considerado parado.")]
    [SerializeField] private float idleThreshold = 0.1f;
    [Tooltip("Suavização da transição do parâmetro Speed.")]
    [SerializeField] private float speedDampTime = 0.1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponentInParent<PlayerMovementThirdPerson>();
        characterController = GetComponentInParent<CharacterController>();

        if (playerMovement == null || characterController == null)
        {
            Debug.LogError("PlayerAnimationController ERRO: Falta PlayerMovementThirdPerson/CharacterController no pai.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // Calcula velocidade horizontal
        Vector3 hv = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
        float currentSpeed = hv.magnitude;

        // Normaliza a velocidade (0 = parado, 1 = velocidade máxima)
        float normalizedSpeed = playerMovement.speed > 0 ? currentSpeed / playerMovement.speed : 0f;

        // Se abaixo do threshold, força para 0 (idle)
        if (currentSpeed < idleThreshold)
        {
            normalizedSpeed = 0f;
        }

        // Usa SetFloat com dampTime para transição suave
        animator.SetFloat(speedHash, normalizedSpeed, speedDampTime, Time.deltaTime);
        animator.SetBool(isGroundedHash, characterController.isGrounded);
    }

    public void TriggerJumpAnimation()
    {
        animator.SetTrigger(jumpHash);
    }

    public void TriggerCollectAnimation(bool isRightSide = true)
    {
        // 1) Não dispare se estiver no estado de coleta
        if (IsInCollectState()) return;

        // 2) Anti-spam
        if (Time.time - lastCollectTime < collectMinInterval) return;
        lastCollectTime = Time.time;

        // 3) Reseta o trigger oposto antes de setar o atual
        if (isRightSide)
        {
            animator.ResetTrigger(collectLeftHash);
            animator.SetTrigger(collectRightHash);
        }
        else
        {
            animator.ResetTrigger(collectRightHash);
            animator.SetTrigger(collectLeftHash);
        }
    }

    private bool IsInCollectState()
    {
        var st = animator.GetCurrentAnimatorStateInfo(0);
        // Ajuste os nomes abaixo para os estados reais do Animator
        if (st.IsName(collectRightStateName) || st.IsName(collectLeftStateName))
            return true;

        // Também verifica estado transitório (CrossFade) no próximo
        var next = animator.GetNextAnimatorStateInfo(0);
        if (next.IsName(collectRightStateName) || next.IsName(collectLeftStateName))
            return true;

        return false;
    }
}