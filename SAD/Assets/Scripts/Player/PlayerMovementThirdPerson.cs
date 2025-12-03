using TMPro;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputManager))]
public class PlayerMovementThirdPerson : MonoBehaviour
{
    public enum MovementMode { Normal, Minigame }
    private MovementMode currentMode = MovementMode.Normal;

    [Header("Movement")]
    [Tooltip("A velocidade máxima que o personagem pode atingir.")]
    public float speed = 7f;
    [Tooltip("A rapidez com que o personagem atinge a velocidade máxima.")]
    public float acceleration = 15f;
    [Tooltip("A rapidez com que o personagem para ao soltar os controles.")]
    public float deceleration = 20f;

    [Header("Rotation")]
    [Tooltip("A rapidez com que o personagem vira para a direção do movimento. Valores mais altos = mais ágil.")]
    public float rotationSpeed = 15f;

    [Header("Minigame Precise Mode")]
    public float minigameSpeed = 5f;
    public float minigameRotation = 10f;

    [Header("Y Movement")]
    public float gravity = -20f;
    public float jumpHeight = 2.5f;

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
    public GameObject speedLinesUI;
    private float speedBoostMultiplier = 1f;
    private float speedBoostTimer = 0f;

    [Header("Referências")]
    public Transform cameraTransform;
    public TextMeshProUGUI speedText;
    public Transform fpvCameraPivot;

    // Componentes e Estado
    CharacterController characterController;
    public PlayerInputManager playerInputManager;
    private PlayerAnimationController animationController;
    public bool isMoving = true;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Vector3 lastPos;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInputManager = GetComponent<PlayerInputManager>();
        animationController = GetComponentInChildren<PlayerAnimationController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (fpvCameraPivot == null)
        {
            fpvCameraPivot = transform.Find("FPVCameraPivot");
        }
        if (speedLinesUI == null && UIManager.instance != null)
        {
            var uiParticle = UIManager.instance.transform.Find("UIParticle");
            if (uiParticle != null) speedLinesUI = uiParticle.gameObject;
        }

        lastPos = transform.position;
        if (speedLinesUI != null) speedLinesUI.SetActive(false);
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
        if (!isMoving) return;

        // --- 1. Leitura do Input e Direção Relativa à Câmera ---
        float horizontal = playerInputManager.GetHorizontal();
        float vertical = playerInputManager.GetVertical();

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = (camForward * vertical + camRight * horizontal);

        // --- 2. Rotação do Personagem ---
        // O personagem agora vira na direção do INPUT, não da velocidade.
        // Isso dá a resposta imediata que você quer.
        if (inputDir.magnitude > 0.1f)
        {
            float currentRotationSpeed = (currentMode == MovementMode.Minigame) ? minigameRotation : rotationSpeed;
            Quaternion targetRotation = Quaternion.LookRotation(inputDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotationSpeed * Time.deltaTime);
        }

        // --- 3. Cálculo da Velocidade (Inércia) ---
        // A velocidade alvo agora é baseada na direção para a qual o personagem ESTÁ VIRADO.
        float baseSpeed = (currentMode == MovementMode.Minigame) ? minigameSpeed : speed;
        float currentMaxSpeed = baseSpeed * speedBoostMultiplier;

        // O personagem tenta acelerar na direção para a qual está virado, mas apenas se houver input.
        Vector3 targetVelocity = transform.forward * currentMaxSpeed * inputDir.magnitude;

        // A aceleração/desaceleração continua usando Lerp para suavidade.
        float lerpSpeed = (inputDir.magnitude > 0.1f) ? acceleration : deceleration;
        moveVelocity = Vector3.Lerp(moveVelocity, targetVelocity, lerpSpeed * Time.deltaTime);

        // --- 4. Movimento Vertical (Pulo e Gravidade) ---
        if (playerInputManager.GetJump() && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animationController != null)
            {
                animationController.TriggerJumpAnimation();
            }
        }

        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        // --- 5. Aplicação do Movimento ---
        Vector3 finalMove = moveVelocity + velocity;

        // Adiciona deslizamento em rampas se necessário
        Vector3 slideDir;
        isSliding = CheckSlope(out slideDir);
        if (isSliding)
        {
            finalMove += slideDir * slideSpeed;
        }

        characterController.Move(finalMove * Time.deltaTime);
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
        if ((pushableLayers.value & (1 << hit.gameObject.layer)) == 0) return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return;

        float currentSpeed = new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;
        if (currentSpeed < minSpeedToPush) return;

        float mass = Mathf.Max(rb.mass, 0.01f);
        float massScale = Mathf.Clamp(massScaleK / Mathf.Pow(mass, massScaleExponent), massScaleMin, massScaleMax);

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
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }
    }

    public float GetHorizontalSpeed()
    {
        return new Vector3(moveVelocity.x, 0f, moveVelocity.z).magnitude;
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