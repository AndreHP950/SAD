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
*/

using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using SAD.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    public float baseForwardSpeed = 6f;      // Velocidade m�nima (inicial)
    public float maxForwardSpeed = 20f;      // Velocidade m�xima sem boost
    public float accelRate = 10f;            // Taxa de acelera��o
    public float brakeRate = 12f;            // Taxa de frenagem (usada com Shift sem W)

    [Header("Desacelera��o")]
    public float slideDeceleration = 5f;     // Decaimento da acelera��o (quando solta o W)

    [Header("Drift/Curva")]
    public float driftThresholdSpeed = 9f;   // M�nima velocidade para drift
    public float driftDampingFactor = 3.5f;  // Qu�o r�pido o momentum se alinha � dire��o atual durante o drift
    public float driftSpeedReduction = 0.7f; // Multiplicador para reduzir a velocidade durante o drift

    [Header("Boost Drift")]
    public float boostMaxValue = 20f;        // Valor m�ximo de boost a ser adicionado no drift
    public float driftTimeForMaxBoost = 2f;  // Tempo necess�rio de drift para atingir boost m�ximo
    public float boostMaxDuration = 2f;      // Dura��o m�xima do boost
    public float boostDecayRate = 8f;        // Taxa de decaimento do boost (gradual)

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 8f;
    public float airControlFactor = 0.35f;

    [Header("Refer�ncias")]
    public Transform cameraPivot;
    public TMP_Text speedText;


    [Header("C�mera")]
    public float mouseSensitivity = 5f;     // Sensibilidade para mouse
    public float touchSensitivity = 0.15f;  // Sensibilidade para toque (arrastar para girar)

    [Header("Mobile")]
    public bool mobileControls = true;      // Ative para usar bot�es mobile + rota��o por toque

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

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
            cameraPivot = transform;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ===== Entrada da c�mera (mouse e/ou toque) =====
        if (mobileControls && Input.touchCount > 0)
        {
            // Usa o primeiro toque que n�o est� sobre UI
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
        if (isGrounded && verticalVel < 0)
            verticalVel = -1f;
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVel = jumpForce;

        // ===== Entradas de movimento (desktop + mobile) =====
        bool accelDesktop = Input.GetKey(KeyCode.W);
        bool brakeDesktop = Input.GetKey(KeyCode.LeftShift);

        bool accelerating = (mobileControls ? MobileInput.AccelerateHeld : accelDesktop) && isGrounded;
        bool brakingOnly = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop) && isGrounded && !accelerating;
        bool driftButton = (mobileControls ? MobileInput.BrakeHeld : brakeDesktop); // No mobile, Acelerar+Frear = drift

        bool driftActive = accelerating && driftButton && (momentum.magnitude > driftThresholdSpeed) && isGrounded;

        // ===== Drift & Boost =====
        if (driftActive)
        {
            if (!isDrifting)
            {
                isDrifting = true;
                driftTimer = 0f;
                driftButtonHeld = true;
            }
            driftTimer += dt;

            // Realinha o momentum mais r�pido, mas preserva in�rcia
            momentum = Vector3.Lerp(momentum, transform.forward * momentum.magnitude, driftDampingFactor * dt);
        }
        else
        {
            if (isDrifting && driftButtonHeld)
            {
                float driftRatio = Mathf.Clamp01(driftTimer / driftTimeForMaxBoost);
                float extraBoost = boostMaxValue * driftRatio;
                momentum = momentum.normalized * (momentum.magnitude + extraBoost);
                boostValue = extraBoost;
                boostTimer = boostMaxDuration * driftRatio;
                driftButtonHeld = false;
            }
            isDrifting = false;
            driftTimer = 0f;
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
            // Se estiver em drift, reduz velocidade alvo
            if (isDrifting)
                target *= driftSpeedReduction;

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
        controller.Move(move * dt);
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