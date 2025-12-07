using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using TMPro;
using UnityEngine.EventSystems;

public class MinigameController : MonoBehaviour
{
    [Header("Minigame")]
    [Tooltip("Tempo máximo para pegar o rato (s)")]
    public float chaseDuration = 10f;
    [Tooltip("Velocidade do rato (m/s) - transferido para RatAI, mas mantido como referência")]
    public float ratSpeed = 6f;

    [Header("Camera / UI")]
    public Image vignetteImage;
    public float vignettePulseSpeed = 4f;
    public Color vignetteColor = new Color(0.7f, 0f, 0f, 0.5f);
    [Tooltip("Velocidade do pulso de escala do texto de alerta.")]
    [SerializeField] private float alertPulseSpeed = 2f;
    [Tooltip("Escala mínima do pulso.")]
    [SerializeField] private float alertMinScale = 0.95f;
    [Tooltip("Escala máxima do pulso.")]
    [SerializeField] private float alertMaxScale = 1.05f;

    [Header("First-Person (FPV)")]
    [Tooltip("Se quiser controlar a vcam em vez de parentar a Main Camera, arraste-a aqui.")]
    public CinemachineCamera virtualCamera;
    public float fpvFOV = 90f;
    public Vector3 fpvLocalPositionOffset = Vector3.zero;
    public Vector3 fpvLocalEulerOffset = Vector3.zero;

    [Header("FPV Camera Control")]
    [Tooltip("Sensibilidade do mouse para rotação da câmera.")]
    public float mouseSensitivity = 2f;
    [Tooltip("Sensibilidade do toque para rotação da câmera.")]
    public float touchSensitivity = 0.15f;
    [Tooltip("Limite de rotação vertical (pitch) para cima.")]
    public float maxPitch = 60f;
    [Tooltip("Limite de rotação vertical (pitch) para baixo.")]
    public float minPitch = -60f;

    [Header("Catch / Reward")]
    public float catchDistance = 1.2f;
    [Tooltip("Cooldown em segundos no início do minigame antes de poder capturar.")]
    public float initialCatchCooldown = 1.0f;
    public float speedBoostMultiplier = 1.3f;
    public float speedBoostDuration = 5f;

    enum MinigameState { Idle, Running, Ending }
    MinigameState state = MinigameState.Idle;

    Camera mainCam;
    Transform previousMainCamParent;
    Vector3 previousMainCamLocalPos;
    Quaternion previousMainCamLocalRot;
    float previousMainCamFOV;

    PlayerMovementThirdPerson playerMovement;
    Transform playerTransform;
    Transform playerCameraPivot;

    FPVCameraBobbing cameraBobbing;
    MonoBehaviour thirdPersonCamComp;

    ChasableAI activeTarget;
    float timer;
    float catchCooldown;

    // Referências para o texto de alerta
    private TextMeshProUGUI minigameAlertText;
    private RectTransform alertTextRect;

    // player renderers storage
    Renderer[] playerRenderers;
    bool[] playerRenderersPrevState;

    // Controle de rotação FPV
    private float currentPitch = 0f;
    private float currentYaw = 0f;

    // Tipo de alvo atual (0 = rato/gato, 1 = galinha/cachorro)
    private int currentTargetType = 0;

    void Start()
    {
        mainCam = Camera.main;
        if (vignetteImage != null) vignetteImage.gameObject.SetActive(false);

        // Encontra o texto de alerta automaticamente para não perder a referência
        if (UIManager.instance != null)
        {
            Transform textTransform = UIManager.instance.transform.Find("GameUI/Minigame Alert");
            if (textTransform != null)
            {
                minigameAlertText = textTransform.GetComponent<TextMeshProUGUI>();
                alertTextRect = textTransform.GetComponent<RectTransform>();
                if (minigameAlertText != null)
                {
                    minigameAlertText.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning("O objeto 'Minigame Alert' não foi encontrado dentro de 'GameUI' no UIManager.");
            }
        }
    }

    // Iniciado pelo RatTrigger: apenas recebe o RatAI (que já tem suas splines)
    public void StartChaseMinigame(ChasableAI target, int targetType = 0)
    {
        if (state != MinigameState.Idle) return;
        if (target == null) return;

        activeTarget = target;
        currentTargetType = targetType;
        activeTarget.StartRunning(this);

        // pega player
        playerMovement = FindFirstObjectByType<PlayerMovementThirdPerson>();
        if (playerMovement == null) { Debug.LogWarning("PlayerMovement not found."); return; }

        // --- NOVAS FUNCIONALIDADES ---
        // 1. Para completamente o movimento do jogador
        playerMovement.StopMomentum();
        // 2. Ativa o modo de movimento preciso do minigame
        playerMovement.SetMovementMode(PlayerMovementThirdPerson.MovementMode.Minigame);

        playerTransform = playerMovement.transform;
        playerCameraPivot = playerMovement.fpvCameraPivot != null ? playerMovement.fpvCameraPivot : playerMovement.transform;
        if (playerCameraPivot == null) { Debug.LogWarning("Player camera pivot not set."); return; }

        // Força player a olhar para o rato
        Vector3 directionToRat = (activeTarget.transform.position - playerTransform.position);
        directionToRat.y = 0;
        if (directionToRat.sqrMagnitude > 0.001f)
        {
            playerTransform.rotation = Quaternion.LookRotation(directionToRat.normalized, Vector3.up);
        }

        // Inicializa os ângulos de rotação baseado na rotação atual do player
        currentYaw = playerTransform.eulerAngles.y;
        currentPitch = 0f;

        // salva estado da Main Camera
        if (mainCam != null)
        {
            previousMainCamParent = mainCam.transform.parent;
            previousMainCamLocalPos = mainCam.transform.localPosition;
            previousMainCamLocalRot = mainCam.transform.localRotation;
            previousMainCamFOV = mainCam.fieldOfView;
        }

        // desativa vcam (se quiser parentar a MainCamera)
        if (virtualCamera == null)
            virtualCamera = Object.FindFirstObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
        {
            virtualCamera.enabled = false;
        }

        // parenta Main Camera ao pivot FPV e ajusta FOV
        if (mainCam != null)
        {
            mainCam.transform.SetParent(playerCameraPivot, false);
            mainCam.transform.localPosition = fpvLocalPositionOffset;
            mainCam.transform.localRotation = Quaternion.Euler(fpvLocalEulerOffset);
            mainCam.fieldOfView = fpvFOV;
        }

        // desativa renderers do player para evitar clipping
        playerRenderers = playerMovement.GetComponentsInChildren<Renderer>(true);
        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            playerRenderersPrevState = new bool[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; ++i)
            {
                var r = playerRenderers[i];
                if (r != null)
                {
                    playerRenderersPrevState[i] = r.enabled;
                    r.enabled = false;
                }
            }
        }

        // tenta desativar script third-person na Main Camera (se existir)
        if (mainCam != null)
        {
            var tp = mainCam.GetComponent("ThirdPersonCameraCollision") as MonoBehaviour;
            if (tp != null)
            {
                thirdPersonCamComp = tp;
                thirdPersonCamComp.enabled = false;
            }

            // adiciona/ativa bobbing
            cameraBobbing = mainCam.gameObject.GetComponent<FPVCameraBobbing>();
            if (cameraBobbing == null) cameraBobbing = mainCam.gameObject.AddComponent<FPVCameraBobbing>();
            cameraBobbing.SetPlayerMovement(playerMovement);
        }

        // vignette
        if (vignetteImage != null)
        {
            vignetteImage.color = vignetteColor;
            vignetteImage.gameObject.SetActive(true);
        }

        // Ativa o texto de alerta
        if (minigameAlertText != null)
        {
            minigameAlertText.gameObject.SetActive(true);
        }

        // start timer e cooldown
        timer = chaseDuration;
        catchCooldown = initialCatchCooldown;
        state = MinigameState.Running;

        // Esconde e trava o cursor (tanto mobile quanto desktop)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (state != MinigameState.Running) return;

        // Atualiza o cooldown de captura
        if (catchCooldown > 0)
        {
            catchCooldown -= Time.deltaTime;
        }

        // Controle de câmera FPV
        HandleFPVCameraRotation();

        // Animação de pulso da vinheta
        if (vignetteImage != null)
        {
            float a = 0.5f + 0.5f * Mathf.Sin(Time.time * vignettePulseSpeed);
            Color c = vignetteImage.color;
            c.a = vignetteColor.a * a;
            vignetteImage.color = c;
        }

        // Animação de pulso do texto de alerta
        if (alertTextRect != null)
        {
            float scale = Mathf.Lerp(alertMinScale, alertMaxScale, (Mathf.Sin(Time.time * alertPulseSpeed) + 1f) / 2f);
            alertTextRect.localScale = new Vector3(scale, scale, 1f);
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            EndChase(false);
            return;
        }

        // Verifica a captura SÓ SE o cooldown tiver acabado
        if (catchCooldown <= 0 && activeTarget != null && playerMovement != null)
        {
            float d = activeTarget.DistanceToPoint(playerTransform.position);
            if (d <= catchDistance)
            {
                activeTarget.OnCaught();
                EndChase(true);
            }
        }
    }

    private void HandleFPVCameraRotation()
    {
        if (playerTransform == null || mainCam == null) return;

        float mouseX = 0f;
        float mouseY = 0f;

        bool isMobile = GameManager.instance != null && GameManager.instance.isMobile;

        if (isMobile)
        {
            // Controle por toque
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    // Ignora toques em elementos de UI
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                        continue;

                    if (touch.phase == TouchPhase.Moved)
                    {
                        mouseX = touch.deltaPosition.x * touchSensitivity;
                        mouseY = touch.deltaPosition.y * touchSensitivity;
                        break;
                    }
                }
            }
        }
        else
        {
            // Controle por mouse
            mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        // Atualiza os ângulos
        currentYaw += mouseX;
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Aplica rotação horizontal ao player (yaw)
        playerTransform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        // Aplica rotação vertical à câmera (pitch)
        mainCam.transform.localRotation = Quaternion.Euler(currentPitch + fpvLocalEulerOffset.x, fpvLocalEulerOffset.y, fpvLocalEulerOffset.z);
    }

    // chamado pelo RatAI quando o rato termina a spline
    public void NotifyTargetEscaped(ChasableAI target)
    {
        if (target != activeTarget) return;
        EndChase(false);
    }

    void EndChase(bool success)
    {
        if (state != MinigameState.Running) return;
        state = MinigameState.Ending;

        // Volta ao modo de movimento normal
        if (playerMovement != null)
        {
            playerMovement.SetMovementMode(PlayerMovementThirdPerson.MovementMode.Normal);
        }

        if (activeTarget != null) activeTarget.StopRunning();

        // restaura main camera
        if (mainCam != null)
        {
            mainCam.transform.SetParent(previousMainCamParent, true);
            mainCam.transform.localPosition = previousMainCamLocalPos;
            mainCam.transform.localRotation = previousMainCamLocalRot;
            mainCam.fieldOfView = previousMainCamFOV;
        }

        // reativa vcam (se houver)
        if (virtualCamera != null)
            virtualCamera.enabled = true;

        if (thirdPersonCamComp != null) thirdPersonCamComp.enabled = true;

        // restaura renderers do player
        if (playerRenderers != null && playerRenderersPrevState != null)
        {
            for (int i = 0; i < playerRenderers.Length; ++i)
            {
                var r = playerRenderers[i];
                if (r != null) r.enabled = playerRenderersPrevState[i];
            }
        }
        playerRenderers = null;
        playerRenderersPrevState = null;

        // stop bobbing
        if (cameraBobbing != null) cameraBobbing.SetPlayerMovement(null);

        if (vignetteImage != null) vignetteImage.gameObject.SetActive(false);

        // Desativa o texto de alerta
        if (minigameAlertText != null)
        {
            minigameAlertText.gameObject.SetActive(false);
        }

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Notifica o sistema de instruções sobre o resultado do minigame
        if (InstructionalTextController.Instance != null)
        {
            bool isCat = currentTargetType == 0; // 0 = rato (gato), 1 = galinha (cachorro)

            if (success)
            {
                InstructionalTextController.Instance.NotifyMinigameCatchSuccess(isCat);
            }
            else
            {
                InstructionalTextController.Instance.NotifyMinigameTargetEscaped(isCat);
            }
        }

        if (success && playerMovement != null)
            playerMovement.ActivateSpeedBoost(speedBoostDuration, speedBoostMultiplier);

        activeTarget = null;
        state = MinigameState.Idle;
    }
}