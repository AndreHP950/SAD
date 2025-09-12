/*
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
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    public float baseForwardSpeed = 6f;      // Velocidade mínima (inicial)
    public float maxForwardSpeed = 20f;      // Velocidade máxima sem boost
    public float accelRate = 10f;            // Taxa de aceleração
    public float brakeRate = 12f;            // Taxa de frenagem (usada com Shift sem W)

    [Header("Desaceleração")]
    public float slideDeceleration = 5f;     // Decaimento da aceleração (quando solta o W)

    [Header("Drift/Curva")]
    public float driftThresholdSpeed = 9f;   // Mínima velocidade para drift
    public float driftDampingFactor = 3.5f;  // Quão rápido o momentum se alinha à direção atual durante o drift
    public float driftSpeedReduction = 0.7f; // Multiplicador para reduzir a velocidade durante o drift

    [Header("Boost Drift")]
    public float boostMaxValue = 20f;        // Valor máximo de boost a ser adicionado no drift
    public float driftTimeForMaxBoost = 2f;  // Tempo necessário de drift para atingir boost máximo
    public float boostMaxDuration = 2f;      // Duração máxima do boost
    public float boostDecayRate = 8f;        // Taxa de decaimento do boost (gradual)

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 8f;
    public float airControlFactor = 0.35f;

    [Header("Referências")]
    public Transform cameraPivot;
    public TMP_Text speedText;

    [Header("Câmera")]
    public float mouseSensitivity = 5f;

    // Variáveis internas
    Vector3 momentum = Vector3.zero;          // Vetor de inércia (movimento horizontal)
    float verticalVel = 0f;                   // Velocidade vertical
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

        // Controle da Câmera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -60f, 60f);
        cameraPivot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

        // Atualiza estado de chão e pulo
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVel < 0)
            verticalVel = -1f;
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVel = jumpForce;

        // Entrada de Movimento
        bool accelerating = Input.GetKey(KeyCode.W) && isGrounded;
        bool shifting = Input.GetKey(KeyCode.LeftShift) && isGrounded;
        bool driftActive = accelerating && shifting && (momentum.magnitude > driftThresholdSpeed);
        bool braking = shifting && !accelerating;

        // Lógica de Drift e Boost
        if (driftActive)
        {
            if (!isDrifting)
            {
                isDrifting = true;
                driftTimer = 0f;
                driftButtonHeld = true;
            }
            driftTimer += dt;
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

        // Atualização do Momentum (Força Acumulada)
        if (braking)
        {
            momentum = Vector3.MoveTowards(momentum, Vector3.zero, brakeRate * dt);
        }
        else if (accelerating)
        {
            Vector3 target = transform.forward * maxForwardSpeed;
            momentum = Vector3.MoveTowards(momentum, target, accelRate * dt);
        }
        else
        {
            momentum = Vector3.MoveTowards(momentum, Vector3.zero, slideDeceleration * dt);
        }
        float maxSpeedLimit = maxForwardSpeed + boostMaxValue;
        if (momentum.magnitude > maxSpeedLimit)
            momentum = momentum.normalized * maxSpeedLimit;

        // Gravidade
        if (!isGrounded)
            verticalVel += gravity * dt;

        // Movimento Final
        Vector3 move = momentum + Vector3.up * verticalVel;
        controller.Move(move * dt);
    }

    void FixedUpdate()
    {
        // Atualiza o texto de velocidade em km/h no Canvas
        if (speedText != null)
        {
            Vector3 horizontalMomentum = new Vector3(momentum.x, 0f, momentum.z);
            float realSpeed = horizontalMomentum.magnitude;

            // Aplica redução de velocidade se estiver em drift
            if (isDrifting)
                realSpeed *= driftSpeedReduction;

            float speedKmh = realSpeed * 3.6f;
            speedText.text = $"{speedKmh:F0} Km/h";
        }
    }
}
