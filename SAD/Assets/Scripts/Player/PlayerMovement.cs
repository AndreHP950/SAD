/*
 * @AndreHP
    Sistema de movimento com in�rcia, drift e boost estilo arcade.

    - O movimento � baseado em um vetor de momentum, simulando f�sica de AddForce.
    - Ao segurar W, o jogador acelera gradualmente na dire��o da c�mera, acumulando momentum.
    - Se a c�mera gira, o momentum n�o muda instantaneamente, criando efeito de deslizamento/in�rcia.
    - Ao soltar W, o momentum desacelera suavemente (slideDeceleration).
    - Shift pode ser usado de duas formas:
        - Com W e velocidade acima do threshold: ativa drift, reduz velocidade e realinha momentum mais r�pido � dire��o atual, acumulando boost proporcional ao tempo em drift.
        - Sem W: funciona como freio, reduzindo momentum mais rapidamente.
    - Ao soltar o drift, o boost acumulado � adicionado ao momentum e decai gradualmente.
    - O movimento s� � afetado pelo drift/freio se o jogador estiver no ch�o.
    - O texto de velocidade no Canvas � atualizado em tempo real, mostrando a velocidade atual em km/h.
    - Detecta colis�es e reduz momentum quando o player est� parado contra obst�culos.
    - Usa "coyote time" para manter drift ao passar por degraus/pequenas eleva��es.
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

    [Header("Desacelera��o")]
    public float slideDeceleration = 5f;
    public float directionChangeDeceleration = 8f;

    [Header("Drift/Curva")]
    public float driftThresholdSpeed = 7f;
    public float driftDampingFactor = 1f;
    public float driftSpeedReduction = 0.5f;
    public float driftCoyoteTime = 0.2f;  // Tempo de toler�ncia para manter drift ao sair brevemente do ch�o

    [Header("Boost Drift")]
    public float boostMaxValue = 20f;
    public float driftTimeForMaxBoost = 3f;
    public float boostMaxDuration = 5f;
    public float boostDecayRate = 3f;

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 8f;
    public float airControlFactor = 0.35f;

    [Header("Colis�o")]
    public float collisionDecayRate = 15f;
    public float collisionThreshold = 0.5f;
    public float stuckTimeThreshold = 0.1f;

    [Header("Refer�ncias")]
    public Transform cameraPivot;
    public TMP_Text speedText;

    [Header("C�mera")]
    public float mouseSensitivity = 5f;
    public float touchSensitivity = 0.15f;

    [Header("Mobile")]
    public bool mobileControls = true;

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

    // Detec��o de colis�o
    Vector3 lastPosition;
    float stuckTimer = 0f;

    // Coyote time para drift
    float lastGroundedTime = 0f;
    bool wasDriftingBeforeAirborne = false;

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

        // ===== Entrada da c�mera (mouse e/ou toque) =====
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

        // ===== Ch�o e pulo =====
        isGrounded = controller.isGrounded;

        // Atualiza o timer de quando esteve no ch�o pela �ltima vez
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            verticalVel = -1f;
        }

        // Pulo cancela o drift imediatamente
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVel = jumpForce;
            wasDriftingBeforeAirborne = false;
            if (isDrifting)
            {
                // Cancela drift ao pular intencionalmente
                isDrifting = false;
            }
        }

        // ===== Entradas de movimento (desktop + mobile) =====
        bool accelDesktop = Input.GetKey(KeyCode.W);
        bool brakeDesktop = Input.GetKey(KeyCode.LeftShift);

        // Considera no ch�o se realmente no ch�o OU dentro do coyote time
        bool effectivelyGrounded = isGrounded || (Time.time - lastGroundedTime <= driftCoyoteTime);

        bool accelerating = (mobileControls ? MobileInput.AccelerateHeld : accelDesktop) && effectivelyGrounded;
        bool brakingOnly = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop) && effectivelyGrounded && !accelerating;
        bool driftButton = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop);

        // Drift s� ativa/mant�m se ainda estiver segurando o bot�o
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
            // S� libera boost se REALMENTE soltou o bot�o de drift
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

            // S� cancela drift se realmente soltou o bot�o OU caiu (tempo expirou)
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

        // ===== Atualiza��o do momentum =====
        if (brakingOnly)
        {
            momentum = Vector3.MoveTowards(momentum, Vector3.zero, brakeRate * dt);
        }
        else if (accelerating)
        {
            Vector3 target = transform.forward * maxForwardSpeed;
            if (isDrifting)
                target *= driftSpeedReduction;

            // Desacelera extra se estiver mudando drasticamente de dire��o
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

        float maxSpeedLimit = maxForwardSpeed + boostMaxValue;
        if (momentum.magnitude > maxSpeedLimit)
            momentum = momentum.normalized * maxSpeedLimit;

        // ===== Gravidade =====
        if (!isGrounded)
            verticalVel += gravity * dt;

        // ===== Movimento final =====
        Vector3 move = momentum + Vector3.up * verticalVel;

        Vector3 positionBeforeMove = transform.position;
        controller.Move(move * dt);

        // ===== Detec��o de colis�o/travamento =====
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
}