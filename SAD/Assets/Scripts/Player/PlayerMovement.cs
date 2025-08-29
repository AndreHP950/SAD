/*
 Sugestao de valores para diferentes veículos (testar e ajustar para cada um):
 Patins
•	baseForwardSpeed: 6
•	maxForwardSpeed: 12
•	accelRate: 10
•	brakeRate: 12
•	lateralSpeed: 6
•	lateralAccel: 10
•	lateralFriction: 8
•	driftThresholdSpeed: 9
•	driftLateralMultiplier: 1.1
•	driftSlowdown: 4
•	gravity: -20
•	jumpForce: 9      // Pulo mais alto, ágil
•	airControlFactor: 0.35
•	turnBlendTime: 0.25
•	turnAngleDegrees: 90
Skate
•	baseForwardSpeed: 7
•	maxForwardSpeed: 14
•	accelRate: 12
•	brakeRate: 10
•	lateralSpeed: 5
•	lateralAccel: 10
•	lateralFriction: 8
•	driftThresholdSpeed: 10
•	driftLateralMultiplier: 1.4
•	driftSlowdown: 4
•	gravity: -20
•	jumpForce: 7.5    // Pulo médio, mais pesado
•	airControlFactor: 0.3
•	turnBlendTime: 0.25
•	turnAngleDegrees: 90
Patinete
•	baseForwardSpeed: 8
•	maxForwardSpeed: 16
•	accelRate: 14
•	brakeRate: 9
•	lateralSpeed: 4
•	lateralAccel: 10
•	lateralFriction: 8
•	driftThresholdSpeed: 11
•	driftLateralMultiplier: 1.2
•	driftSlowdown: 4
•	gravity: -20
•	jumpForce: 6.5    // Pulo mais baixo, mais pesado
•	airControlFactor: 0.25
•	turnBlendTime: 0.25
•	turnAngleDegrees: 90
 */

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidade")]
    public float baseForwardSpeed = 6f;
    public float maxForwardSpeed = 12f;
    public float accelRate = 10f;
    public float brakeRate = 12f;

    [Header("Lateral")]
    public float lateralSpeed = 6f;
    public float lateralAccel = 10f;
    public float lateralFriction = 8f;

    [Header("Drift/Curva")]
    public float turnBlendTime = 0.25f;
    public float driftThresholdSpeed = 9f;
    public float driftLateralMultiplier = 1.1f;
    public float driftSlowdown = 4f;

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 9f;           // Patins: 9, Skate: 7.5, Patinete: 6.5
    public float airControlFactor = 0.35f; // Patins: 0.35, Skate: 0.3, Patinete: 0.25

    [Header("Curva por Botão")]
    public float turnAngleDegrees = 90f;

    [Header("Referências")]
    public Transform forwardReference;

    // Variáveis internas
    float currentSpeed;
    float lateralVel;
    float verticalVel;
    bool isGrounded;

    // Variáveis de curva
    bool isTurning = false;
    float turnTimer = 0f;
    float startYaw;
    float targetYaw;
    float currentYaw;
    float pendingTurn = 0f; // -1 = esquerda, 1 = direita, 2 = 180°

    // Drift
    bool isDrifting = false;

    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (forwardReference == null) forwardReference = transform;
        currentSpeed = 0f; // Começa parado
    }

    void Update()
    {
        // Checa se está no chão
        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVel < 0) verticalVel = -1f;

        // Pulo
        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVel = jumpForce;
        }

        // Input lateral
        float inputX = Input.GetAxis("Horizontal");
        float targetLateral = inputX * lateralSpeed;
        if (isGrounded)
        {
            lateralVel = Mathf.MoveTowards(lateralVel, targetLateral, lateralAccel * Time.deltaTime);
            if (Mathf.Abs(inputX) < 0.1f)
                lateralVel = Mathf.MoveTowards(lateralVel, 0, lateralFriction * Time.deltaTime);
        }
        else
        {
            lateralVel = Mathf.MoveTowards(lateralVel, targetLateral * airControlFactor, lateralAccel * airControlFactor * Time.deltaTime);
        }

        // Input avanço/freio
        float inputZ = Input.GetAxis("Vertical");
        bool braking = Input.GetKey(KeyCode.LeftShift) || inputZ < -0.1f;
        if (braking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeRate * Time.deltaTime);
        }
        else
        {
            float targetSpeed = 0f;
            if (inputZ > 0.1f)
                targetSpeed = Mathf.Lerp(0, maxForwardSpeed, inputZ); // acelera até o máximo conforme segura W

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);
        }
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxForwardSpeed);

        // Drift simples (corrigido)
        if (currentSpeed > driftThresholdSpeed && braking)
        {
            if (!isDrifting)
            {
                lateralVel *= driftLateralMultiplier;
                isDrifting = true;
            }
            currentSpeed = Mathf.MoveTowards(currentSpeed, driftThresholdSpeed, driftSlowdown * Time.deltaTime);
        }
        else
        {
            isDrifting = false;
        }

        // Gravidade
        if (!isGrounded)
            verticalVel += gravity * Time.deltaTime;

        // ----------- CURVAS (Q/E/R) -----------
        // Detecta pedido de curva
        if (!isTurning)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                pendingTurn = -1f; // Esquerda
            else if (Input.GetKeyDown(KeyCode.E))
                pendingTurn = 1f;  // Direita
            else if (Input.GetKeyDown(KeyCode.R))
                pendingTurn = 2f;  // Meia-volta
        }

        // Inicia curva se houver pedido
        if (!isTurning && pendingTurn != 0f)
        {
            isTurning = true;
            turnTimer = 0f;
            startYaw = transform.eulerAngles.y;
            if (pendingTurn == 2f)
                targetYaw = startYaw + 180f;
            else
                targetYaw = startYaw + pendingTurn * turnAngleDegrees;
            pendingTurn = 0f;
        }

        // Blend de curva
        if (isTurning)
        {
            turnTimer += Time.deltaTime;
            float t = Mathf.Clamp01(turnTimer / turnBlendTime);
            currentYaw = Mathf.LerpAngle(startYaw, targetYaw, t);
            transform.rotation = Quaternion.Euler(0, currentYaw, 0);

            if (t >= 1f)
            {
                isTurning = false;
                transform.rotation = Quaternion.Euler(0, targetYaw, 0);
            }
        }
        // ----------- FIM CURVAS -----------

        // Movimento final
        Vector3 move = forwardReference.forward * currentSpeed + forwardReference.right * lateralVel + Vector3.up * verticalVel;
        controller.Move(move * Time.deltaTime);
    }
}
