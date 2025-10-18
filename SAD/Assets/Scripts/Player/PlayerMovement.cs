/*
 * @AndreHP
    Sistema de movimento com inércia, drift e boost estilo arcade.

    - O movimento é baseado em um vetor de momentum, simulando física de AddForce.
    - Ao segurar W, o jogador acelera gradualmente na direção da câmera, acumulando momentum.
    - Se a câmera gira, o momentum não muda instantaneamente, criando efeito de deslizamento/inércia.
    - Ao soltar W, o momentum desacelera suavemente (slideDeceleration).
    - Shift pode ser usado de duas formas:
        - Com W e velocidade acima do threshold: ativa drift, reduz velocidade e realinha momentum mais rápido à direção atual, acumulando boost proporcional ao tempo em drift.
        - Sem W: funciona como freio, reduzindo momentum mais rapidamente.
    - Ao soltar o drift, o boost acumulado é adicionado ao momentum e decai gradualmente.
    - O movimento só é afetado pelo drift/freio se o jogador estiver no chão.
    - O texto de velocidade no Canvas é atualizado em tempo real, mostrando a velocidade atual em km/h.
    - Detecta colisões e reduz momentum quando o player está parado contra obstáculos.
    - Usa "coyote time" para manter drift ao passar por degraus/pequenas elevações.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using SAD.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    public float baseForwardSpeed = 6f;
    public float maxForwardSpeed = 20f;
    public float accelRate = 10f;
    public float brakeRate = 12f;

    [Header("Desaceleração")]
    public float slideDeceleration = 5f;
    public float directionChangeDeceleration = 8f;

    [Header("Drift/Curva")]
    public float driftThresholdSpeed = 7f;
    public float driftDampingFactor = 1f;
    public float driftSpeedReduction = 0.5f;
    public float driftCoyoteTime = 0.2f;

    [Header("Boost Drift")]
    public float boostMaxValue = 20f;
    public float driftTimeForMaxBoost = 3f;
    public float boostMaxDuration = 5f;
    public float boostDecayRate = 3f;

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 8f;
    public float airControlFactor = 0.35f;

    [Header("Colisão")]
    public float collisionDecayRate = 15f;
    public float collisionThreshold = 0.5f;
    public float stuckTimeThreshold = 0.1f;

    public enum PushEffectMode { VelocityChangeMassScaled, ImpulseMassAware }

    [Header("Empurrar Objetos")]
    public LayerMask pushableLayers = ~0;
    public LayerMask unpushableLayers = 0;
    public float minSpeedToPush = 0.8f;
    public PushEffectMode pushMode = PushEffectMode.VelocityChangeMassScaled;
    public float pushPower = 0.8f;
    public float pushUpwardVelocity = 2f;
    public float pushTorque = 3f;
    public float massScaleK = 1f;
    public float massScaleExponent = 1f;
    public float massScaleMin = 0.2f;
    public float massScaleMax = 3f;

    [Header("Referências")]
    public Transform cameraPivot;
    public TMP_Text speedText;

    [Header("Câmera")]
    public float mouseSensitivity = 5f;
    public float touchSensitivity = 0.15f;

    [Header("Mobile")]
    public bool mobileControls = true;

    [Header("Power-up: Speed")]
    [Tooltip("Raiz do VFX (por exemplo, GameObject com UIParticle no Canvas). Será desligado no Awake.")]
    public GameObject speedLinesUI;
    [Tooltip("Multiplicador atual aplicado à velocidade máxima (1 = sem boost).")]
    public float speedBoostMultiplier = 1f;
    float speedBoostTimer = 0f;

    // Estado interno
    Vector3 momentum = Vector3.zero;
    float verticalVel = 0f;
    bool isGrounded;
    bool isDrifting = false;
    float driftTimer = 0f;
    bool driftButtonHeld = false;
    float boostValue = 0f;
    float boostTimer = 0f;
    CharacterController controller;
    float cameraPitch = 0f;

    // Detecção de colisão
    Vector3 lastPosition;
    float stuckTimer = 0f;

    // Coyote time para drift
    float lastGroundedTime = 0f;
    bool wasDriftingBeforeAirborne = false;

    void Awake()
    {
        // Garante VFX desligado ao iniciar
        if (speedLinesUI != null) speedLinesUI.SetActive(false);
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
            cameraPivot = transform;
        Cursor.lockState = CursorLockMode.Locked;
        lastPosition = transform.position;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool isPaused = Time.timeScale < 0.1f;

        // ===== Entrada da câmera (mouse e/ou toque) =====
        if (!isPaused)
        {
            if (mobileControls && Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                        continue;

                    if (t.phase == TouchPhase.Moved)
                    {
                        float yaw = t.deltaPosition.x * touchSensitivity;
                        float pitch = -t.deltaPosition.y * touchSensitivity;
                        transform.Rotate(Vector3.up * yaw);
                        cameraPitch = Mathf.Clamp(cameraPitch + pitch, -60f, 60f);
                        cameraPivot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
                    }
                    break;
                }
            }
            else
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
                transform.Rotate(Vector3.up * mouseX);
                cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -60f, 60f);
                cameraPivot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
            }
        }

        // ===== Atualiza timer do power-up =====
        if (speedBoostTimer > 0f)
        {
            speedBoostTimer -= dt;
            if (speedBoostTimer <= 0f)
            {
                speedBoostMultiplier = 1f;
                if (speedLinesUI != null) speedLinesUI.SetActive(false);
            }
        }

        // ===== Chão e pulo =====
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            verticalVel = -1f;
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVel = jumpForce;
            wasDriftingBeforeAirborne = false;
            if (isDrifting)
            {
                isDrifting = false;
            }
        }

        // ===== Entradas de movimento =====
        bool accelDesktop = Input.GetKey(KeyCode.W);
        bool brakeDesktop = Input.GetKey(KeyCode.LeftShift);

        bool effectivelyGrounded = isGrounded || (Time.time - lastGroundedTime <= driftCoyoteTime);

        bool accelerating = (mobileControls ? MobileInput.AccelerateHeld : accelDesktop) && effectivelyGrounded;
        bool brakingOnly = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop) && effectivelyGrounded && !accelerating;
        bool driftButton = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop);

        bool driftActive = accelerating && driftButton && (momentum.magnitude > driftThresholdSpeed) && effectivelyGrounded;

        // ===== Drift & Boost =====
        if (driftActive)
        {
            if (!isDrifting)
            {
                isDrifting = true;
                driftTimer = 0f;
                driftButtonHeld = true;
                wasDriftingBeforeAirborne = false;
            }
            driftTimer += dt;
            momentum = Vector3.Lerp(momentum, transform.forward * momentum.magnitude, driftDampingFactor * dt);
        }
        else
        {
            bool actuallyReleasedDriftButton = !driftButton;

            if (isDrifting && driftButtonHeld && actuallyReleasedDriftButton)
            {
                float driftRatio = Mathf.Clamp01(driftTimer / driftTimeForMaxBoost);
                float extraBoost = boostMaxValue * driftRatio;
                momentum = momentum.normalized * (momentum.magnitude + extraBoost);
                boostValue = extraBoost;
                boostTimer = boostMaxDuration * driftRatio;
                driftButtonHeld = false;
                wasDriftingBeforeAirborne = false;
            }

            if (actuallyReleasedDriftButton || (Time.time - lastGroundedTime > driftCoyoteTime))
            {
                isDrifting = false;
                driftTimer = 0f;
            }
        }

        // Decaimento do boost
        if (boostTimer > 0f)
        {
            boostTimer -= dt;
            float newBoost = Mathf.MoveTowards(boostValue, 0f, boostDecayRate * dt);
            float deltaBoost = boostValue - newBoost;
            boostValue = newBoost;
            float newMag = momentum.magnitude - deltaBoost;
            if (newMag < 0) newMag = 0;
            momentum = (momentum.magnitude > 0) ? momentum.normalized * newMag : Vector3.zero;
        }

        // ===== Atualização do momentum =====
        if (brakingOnly)
        {
            momentum = Vector3.MoveTowards(momentum, Vector3.zero, brakeRate * dt);
        }
        else if (accelerating)
        {
            float speedCap = maxForwardSpeed * speedBoostMultiplier; // aplica boost de velocidade
            Vector3 target = transform.forward * speedCap;
            if (isDrifting)
                target *= driftSpeedReduction;

            if (momentum.magnitude > 0.1f)
            {
                float dot = Vector3.Dot(momentum.normalized, transform.forward);
                if (dot < 0.3f)
                {
                    float decayFactor = Mathf.Lerp(directionChangeDeceleration, 0f, (dot + 1f) / 2f);
                    momentum = Vector3.MoveTowards(momentum, Vector3.zero, decayFactor * dt);
                }
            }

            momentum = Vector3.MoveTowards(momentum, target, accelRate * dt);
        }
        else
        {
            momentum = Vector3.MoveTowards(momentum, Vector3.zero, slideDeceleration * dt);
        }

        float maxSpeedLimit = (maxForwardSpeed * speedBoostMultiplier) + boostMaxValue;
        if (momentum.magnitude > maxSpeedLimit)
            momentum = momentum.normalized * maxSpeedLimit;

        // ===== Gravidade =====
        if (!isGrounded)
            verticalVel += gravity * dt;

        // ===== Movimento final =====
        Vector3 move = momentum + Vector3.up * verticalVel;

        Vector3 positionBeforeMove = transform.position;
        controller.Move(move * dt);

        // ===== Detecção de colisão/travamento =====
        if (accelerating && momentum.magnitude > collisionThreshold)
        {
            float actualMovement = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(positionBeforeMove.x, 0f, positionBeforeMove.z)
            );

            float expectedMovement = new Vector3(move.x, 0f, move.z).magnitude * dt;
            bool isPossiblyStuck = actualMovement < (expectedMovement * 0.2f) && expectedMovement > 0.01f;

            if (isPossiblyStuck)
            {
                stuckTimer += dt;
                if (stuckTimer >= stuckTimeThreshold)
                {
                    momentum = Vector3.MoveTowards(momentum, Vector3.zero, collisionDecayRate * dt);
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (speedText != null)
        {
            Vector3 horizontalMomentum = new Vector3(momentum.x, 0f, momentum.z);
            float realSpeed = horizontalMomentum.magnitude;
            if (isDrifting)
                realSpeed *= driftSpeedReduction;

            float speedKmh = realSpeed * 3.6f;
            speedText.text = $"{speedKmh:F0} Km/h";
        }
    }

    // ===== Empurrar rigidbodies com CharacterController =====
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if ((unpushableLayers.value & (1 << hit.gameObject.layer)) != 0)
            return;
        if ((pushableLayers.value & (1 << hit.gameObject.layer)) == 0)
            return;

        Rigidbody rb = hit.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic)
            return;
        if (hit.moveDirection.y < -0.3f)
            return;

        Vector3 horizVel = new Vector3(momentum.x, 0f, momentum.z);
        float speed = horizVel.magnitude;
        if (speed < minSpeedToPush)
            return;

        Vector3 dir = horizVel.sqrMagnitude > 0.0001f ? horizVel.normalized : hit.moveDirection;

        if (pushMode == PushEffectMode.VelocityChangeMassScaled)
        {
            float m = Mathf.Max(rb.mass, 0.01f);
            float massScale = Mathf.Clamp(massScaleK / Mathf.Pow(m, massScaleExponent), massScaleMin, massScaleMax);

            Vector3 velChange = dir * (pushPower * speed * massScale);
            velChange.y += pushUpwardVelocity * massScale;
            rb.AddForce(velChange, ForceMode.VelocityChange);

            Vector3 torqueAxis = Vector3.Cross(Vector3.up, dir);
            if (torqueAxis.sqrMagnitude > 0.0001f)
                rb.AddTorque(torqueAxis.normalized * (pushTorque * massScale), ForceMode.VelocityChange);
        }
        else
        {
            Vector3 impulse = dir * (pushPower * speed);
            impulse.y += pushUpwardVelocity;
            rb.AddForce(impulse, ForceMode.Impulse);

            Vector3 torqueAxis = Vector3.Cross(Vector3.up, dir);
            if (torqueAxis.sqrMagnitude > 0.0001f)
                rb.AddTorque(torqueAxis.normalized * pushTorque, ForceMode.Impulse);
        }
    }

    // -------- API pública: Power-up Speed --------
    public void ActivateSpeedBoost(float duration, float multiplier = 1.3f)
    {
        speedBoostMultiplier = Mathf.Max(speedBoostMultiplier, multiplier);
        speedBoostTimer = Mathf.Max(speedBoostTimer, duration);
        if (speedLinesUI != null) speedLinesUI.SetActive(true);
    }
}