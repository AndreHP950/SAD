using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementTurnNode : MonoBehaviour
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
    public float turnBlendTime = 0.5f;  // Usado como tempo de curva (ajustado dinamicamente)
    public float driftThresholdSpeed = 9f;
    public float driftLateralMultiplier = 1.1f;
    public float driftSlowdown = 4f;

    [Header("Rampa/Gravidade/Pulo")]
    public float gravity = -20f;
    public float jumpForce = 9f;
    public float airControlFactor = 0.35f;

    [Header("Turn Settings")]
    public float minTurnTime = 0.5f; // Tempo mínimo para executar a curva
    public float maxTurnTime = 1.5f; // Tempo máximo para executar a curva

    [Header("Referências")]
    public Transform forwardReference;

    float currentSpeed;
    float lateralVel;
    float verticalVel;
    bool isGrounded;

    // Curva automática
    bool isTurning = false;
    float turnTimer = 0f;
    Vector3 turnStartPos;
    Vector3 turnEndPos;
    Vector3 turnControlPoint;
    Quaternion startRot;
    Quaternion endRot;
    float turnCooldownTimer = 0f;
    float turnArcLength = 0f; // Comprimento do arco
    float turnProgress = 0f;  // 0..1
    float startY; // Altura Y inicial para preservar durante a curva

    bool isDrifting = false;
    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (forwardReference == null)
            forwardReference = transform;
        currentSpeed = 0f;
    }

    void Update()
    {
        if (turnCooldownTimer > 0f)
            turnCooldownTimer -= Time.deltaTime;

        isGrounded = controller.isGrounded;
        if (isGrounded && verticalVel < 0)
            verticalVel = -1f;

        bool canInput = !isTurning;

        if (canInput && isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVel = jumpForce;

        float inputX = canInput ? Input.GetAxis("Horizontal") : 0f;
        float targetLateral = inputX * lateralSpeed;
        if (isGrounded)
        {
            lateralVel = Mathf.MoveTowards(lateralVel, targetLateral, lateralAccel * Time.deltaTime);
            if (Mathf.Abs(inputX) < 0.1f)
                lateralVel = Mathf.MoveTowards(lateralVel, 0, lateralFriction * Time.deltaTime);
        }
        else
        {
            lateralVel = Mathf.MoveTowards(lateralVel, targetLateral * airControlFactor,
                            lateralAccel * airControlFactor * Time.deltaTime);
        }

        float inputZ = canInput ? Input.GetAxis("Vertical") : 0f;
        bool braking = canInput && (Input.GetKey(KeyCode.LeftShift) || inputZ < -0.1f);
        if (braking)
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeRate * Time.deltaTime);
        else
        {
            float targetSpeed = 0f;
            if (inputZ > 0.1f)
                targetSpeed = Mathf.Lerp(0, maxForwardSpeed, inputZ);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);
        }
        currentSpeed = Mathf.Clamp(currentSpeed, 0, maxForwardSpeed);

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
            isDrifting = false;

        if (!isGrounded)
            verticalVel += gravity * Time.deltaTime;

        if (isTurning)
        {
            turnTimer += Time.deltaTime;
            turnProgress = Mathf.Clamp01(turnTimer / turnBlendTime);

            Vector3 pos = Mathf.Pow(1 - turnProgress, 2) * turnStartPos +
                          2 * (1 - turnProgress) * turnProgress * turnControlPoint +
                          Mathf.Pow(turnProgress, 2) * turnEndPos;

            pos.y = startY;

            controller.enabled = false;
            transform.position = pos;
            controller.enabled = true;

            transform.rotation = Quaternion.Slerp(startRot, endRot, turnProgress);

            if (turnProgress >= 1f)
            {
                isTurning = false;
                turnProgress = 0f;
                transform.rotation = endRot;
            }
        }
        else
        {
            Vector3 move = forwardReference.forward * currentSpeed +
                           forwardReference.right * lateralVel +
                           Vector3.up * verticalVel;
            controller.Move(move * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TurnNode") && !isTurning && turnCooldownTimer <= 0f)
        {
            TurnNode node = other.GetComponent<TurnNode>();
            if (node != null && node.targetNode != null)
            {
                Vector3 targetDir = node.GetTargetDirection();
                float angle = Vector3.Angle(transform.forward, targetDir);
                if (angle < 100f)
                {
                    turnStartPos = transform.position;
                    turnEndPos = node.targetNode.position;

                    startY = transform.position.y;
                    turnEndPos.y = startY;

                    float arcRadius = Vector3.Distance(new Vector3(turnStartPos.x, 0, turnStartPos.z),
                                                      new Vector3(turnEndPos.x, 0, turnEndPos.z)) / 2f;
                    float arcAngle = node.curveAngle; // Por exemplo, 90 ou -90
                    Vector3 dir = Quaternion.AngleAxis(arcAngle / 2f, Vector3.up) * transform.forward;
                    turnControlPoint = turnStartPos + dir * arcRadius;
                    turnControlPoint.y = startY;

                    startRot = transform.rotation;
                    // Aqui invertemos o ângulo: se desejado for girar para a direita (arcAngle negativo),
                    // aplicamos -arcAngle para que a rotação final seja correta.
                    endRot = startRot * Quaternion.Euler(0, -arcAngle, 0);

                    turnArcLength = arcRadius * Mathf.Deg2Rad * Mathf.Abs(arcAngle);

                    float desiredTurnTime = (currentSpeed > 0f) ? (turnArcLength / currentSpeed) : turnBlendTime;
                    desiredTurnTime = Mathf.Clamp(desiredTurnTime, minTurnTime, maxTurnTime);
                    turnBlendTime = desiredTurnTime;

                    turnTimer = 0f;
                    turnProgress = 0f;
                    isTurning = true;
                    turnCooldownTimer = node.cooldown;
                }
            }
        }
    }
}
