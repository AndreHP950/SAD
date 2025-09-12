/*
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
using UnityEngine.UI;
using TMPro;

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
    public float mouseSensitivity = 5f;

    // Vari�veis internas
    Vector3 momentum = Vector3.zero;          // Vetor de in�rcia (movimento horizontal)
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

        // Controle da C�mera
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -60f, 60f);
        cameraPivot.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);

        // Atualiza estado de ch�o e pulo
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

        // L�gica de Drift e Boost
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

        // Atualiza��o do Momentum (For�a Acumulada)
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

            // Aplica redu��o de velocidade se estiver em drift
            if (isDrifting)
                realSpeed *= driftSpeedReduction;

            float speedKmh = realSpeed * 3.6f;
            speedText.text = $"{speedKmh:F0} Km/h";
        }
    }
}
