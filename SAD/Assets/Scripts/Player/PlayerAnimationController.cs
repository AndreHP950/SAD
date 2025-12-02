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
        Vector3 hv = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
        float normalizedSpeed = playerMovement.speed > 0 ? hv.magnitude / playerMovement.speed : 0f;

        animator.SetFloat(speedHash, normalizedSpeed);
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