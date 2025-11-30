using TMPro;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputManager))]
public class PlayerMovementThirdPerson : MonoBehaviour
{
    // Enum para definir o modo de movimento
    public enum MovementMode { Normal, Minigame }
    private MovementMode currentMode = MovementMode.Normal;

    [Header("Movement")]
    public float speed = 5f;
    public float acceleration = 10f;
    public float deceleration = 8f;

    [Header("Rotation")]
    public float rotation = 10f;

    [Header("Minigame Precise Mode")]
    [Tooltip("Velocidade do jogador durante o minigame.")]
    public float minigameSpeed = 10f;
    [Tooltip("Velocidade de rotação do jogador durante o minigame (menor = mais suave e preciso).")]
    public float minigameRotation = 4f;

    [Header("Y Movement")]
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Slope Sliding")]
    public float slideSpeed = 2f;
    public float slopeRayLength = 1.5f;
    private bool isSliding;

    [Header("Empurrar Objetos (Rigidbodies)")]
    public LayerMask pushableLayers = ~0;
    public float minSpeedToPush = 0.8f;
    public float pushPower = 0.8f;
    public float massScaleK = 1f;
    public float massScaleExponent = 1f;
    public float massScaleMin = 0.2f;
    public float massScaleMax = 3f;

    [Header("Power-up: Speed")]
    [Tooltip("Raiz do VFX (por exemplo, GameObject com UIParticle no Canvas).")]
    public GameObject speedLinesUI;
    private float speedBoostMultiplier = 1f;
    private float speedBoostTimer = 0f;

    [Header("Referências")]
    public Transform cameraTransform;
    public TextMeshProUGUI speedText;
    [Tooltip("Pivô da câmera para o modo FPV (primeira pessoa) durante minigames.")]
    public Transform fpvCameraPivot;

    // Componentes e Estado
    CharacterController characterController;
    public PlayerInputManager playerInputManager;
    private PlayerAnimationController animationController; // Referência para o controlador de animação
    public bool isMoving = true;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Vector3 lastPos;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInputManager = GetComponent<PlayerInputManager>();
        animationController = GetComponentInChildren<PlayerAnimationController>(); // Encontra o controlador nos filhos

        // Tenta encontrar as referências dinamicamente se não estiverem setadas
        if (speedLinesUI == null && UIManager.instance != null)
        {
            var uiParticle = UIManager.instance.transform.Find("UIParticle");
            if (uiParticle != null) speedLinesUI = uiParticle.gameObject;
        }
        if (fpvCameraPivot == null)
        {
            fpvCameraPivot = transform.Find("FPVCameraPivot");
        }
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        lastPos = transform.position;

        // Desativa o efeito de velocidade no início
        if (speedLinesUI != null)
            speedLinesUI.SetActive(false);
    }

    private void Update()
    {
        HandlePowerUpTimer();
        Movement();
        UpdateVelocity();
    }

    private void HandlePowerUpTimer()
    {
        if (speedBoostTimer > 0f)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                speedBoostMultiplier = 1f;
                if (speedLinesUI != null) speedLinesUI.SetActive(false);
            }
        }
    }

    private void Movement()
    {
        if (isMoving)
        {
            float horizontal = playerInputManager.GetHorizontal();
            float vertical = playerInputManager.GetVertical();

            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 inputDir = (camForward * vertical + camRight * horizontal).normalized;

            // Usa a velocidade correta para o modo atual e aplica o power-up
            float baseSpeed = currentMode == MovementMode.Minigame ? minigameSpeed : speed;
            float currentMaxSpeed = baseSpeed * speedBoostMultiplier;
            Vector3 targetVelocity = inputDir * currentMaxSpeed;

            float lerpSpeed = (inputDir.magnitude > 0.1f) ? acceleration : deceleration;
            moveVelocity = Vector3.Lerp(moveVelocity, targetVelocity, lerpSpeed * Time.deltaTime);

            if (playerInputManager.GetJump() && characterController.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                // Notifica o controlador de animação para tocar a animação de pulo
                if (animationController != null)
                {
                    animationController.TriggerJumpAnimation();
                }
            }

            if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;

            Vector3 flatVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
            if (flatVel.magnitude > 0.1f)
            {
                // Usa a rotação correta para o modo atual
                float currentRotation = currentMode == MovementMode.Minigame ? minigameRotation : rotation;
                Quaternion targetRotation = Quaternion.LookRotation(flatVel);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotation * Time.deltaTime);
            }

            Vector3 moveDir = transform.forward * flatVel.magnitude;

            Vector3 slideDir;
            isSliding = CheckSlope(out slideDir);

            if (isSliding)
            {
                moveDir += slideDir * slideSpeed;
            }

            Vector3 finalMove = (moveDir + velocity) * Time.deltaTime;
            characterController.Move(finalMove);
        }

    }

    private void UpdateVelocity()
    {
        float speedValue = (transform.position - lastPos).magnitude / Time.deltaTime * 3.6f;
        if (speedText != null)
        {
            speedText.text = ("Speed: " + (int)speedValue + " Km/h");
        }
        lastPos = transform.position;
    }

    private bool CheckSlope(out Vector3 slideDirection)
    {
        slideDirection = Vector3.zero;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.down, out hit, slopeRayLength))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle > characterController.slopeLimit)
            {
                slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                return true;
            }
        }
        return false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if ((pushableLayers.value & (1 << hit.gameObject.layer)) == 0)
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic)
            return;
        if (hit.moveDirection.y < -0.3f) // Não empurrar se estivermos caindo sobre o objeto
            return;

        float currentSpeed = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;
        if (currentSpeed < minSpeedToPush)
            return;

        // Calcula a escala de força baseada na massa do objeto
        float mass = Mathf.Max(rb.mass, 0.01f);
        float massScale = Mathf.Clamp(massScaleK / Mathf.Pow(mass, massScaleExponent), massScaleMin, massScaleMax);

        // Aplica a força
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z).normalized;
        Vector3 force = pushDir * pushPower * currentSpeed * massScale;
        rb.AddForceAtPosition(force, hit.point, ForceMode.VelocityChange);
    }

    public void ActivateSpeedBoost(float duration, float multiplier = 1.3f)
    {
        speedBoostMultiplier = Mathf.Max(speedBoostMultiplier, multiplier);
        speedBoostTimer = Mathf.Max(speedBoostTimer, duration);
        if (speedLinesUI != null) speedLinesUI.SetActive(true);
    }

    public void ForceLookAt(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0; // Mantém a rotação apenas no plano horizontal
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    public float GetHorizontalSpeed()
    {
        Vector3 h = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        return h.magnitude;
    }

    public void SetMovementMode(MovementMode newMode)
    {
        currentMode = newMode;
    }


    public void StopMomentum()
    {
        moveVelocity = Vector3.zero;
        velocity = Vector3.zero;
    }
}