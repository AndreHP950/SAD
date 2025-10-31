using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;

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

    [Header("First-Person (FPV)")]
    [Tooltip("Se quiser controlar a vcam em vez de parentar a Main Camera, arraste-a aqui.")]
    public CinemachineCamera virtualCamera;
    public float fpvFOV = 90f;
    public Vector3 fpvLocalPositionOffset = Vector3.zero;
    public Vector3 fpvLocalEulerOffset = Vector3.zero;

    [Header("Catch / Reward")]
    public float catchDistance = 1.2f;
    public float speedBoostMultiplier = 1.3f;
    public float speedBoostDuration = 5f;

    enum MinigameState { Idle, Running, Ending }
    MinigameState state = MinigameState.Idle;

    Camera mainCam;
    Transform previousMainCamParent;
    Vector3 previousMainCamLocalPos;
    Quaternion previousMainCamLocalRot;
    float previousMainCamFOV;

    PlayerMovement playerMovement;
    Transform playerTransform;
    Transform playerCameraPivot;

    FPVCameraBobbing cameraBobbing;
    MonoBehaviour thirdPersonCamComp;

    RatAI activeRat;
    float timer;

    // player renderers storage
    Renderer[] playerRenderers;
    bool[] playerRenderersPrevState;

    void Start()
    {
        mainCam = Camera.main;
        if (vignetteImage != null) vignetteImage.gameObject.SetActive(false);
    }

    // Iniciado pelo RatTrigger: apenas recebe o RatAI (que já tem suas splines)
    public void StartRatChase(RatAI rat)
    {
        if (state != MinigameState.Idle) return;
    if (rat == null) return;

    activeRat = rat;
    activeRat.StartRunning(this);

    // pega player
    playerMovement = FindFirstObjectByType<PlayerMovement>();
    if (playerMovement == null) { Debug.LogWarning("PlayerMovement not found."); return; }

    playerTransform = playerMovement.transform;
    playerCameraPivot = playerMovement.fpvCameraPivot != null ? playerMovement.fpvCameraPivot : playerMovement.cameraPivot;
    if (playerCameraPivot == null) { Debug.LogWarning("Player camera pivot not set."); return; }

    // Força player a olhar para o rato
    Vector3 directionToRat = (rat.transform.position - playerTransform.position);
    directionToRat.y = 0; // mantém rotação apenas no plano horizontal
    if (directionToRat.sqrMagnitude > 0.001f)
    {
        playerTransform.rotation = Quaternion.LookRotation(directionToRat.normalized, Vector3.up);
    }
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
            // desative para impedir override da main camera (vamos parentar)
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

        // start timer
        timer = chaseDuration;
        state = MinigameState.Running;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (state != MinigameState.Running) return;

        if (vignetteImage != null)
        {
            float a = 0.5f + 0.5f * Mathf.Sin(Time.time * vignettePulseSpeed);
            Color c = vignetteImage.color;
            c.a = vignetteColor.a * a;
            vignetteImage.color = c;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            EndChase(false);
            return;
        }

        if (activeRat != null && playerMovement != null)
        {
            float d = activeRat.DistanceToPoint(playerTransform.position);
            if (d <= catchDistance)
            {
                // captura: destrói rato e finaliza com sucesso
                activeRat.OnCaught();
                EndChase(true);
            }
        }
    }

    // chamado pelo RatAI quando o rato termina a spline
    public void NotifyRatEscaped(RatAI rat)
    {
        if (rat != activeRat) return;
        EndChase(false);
    }

    void EndChase(bool success)
    {
        if (state != MinigameState.Running) return;
        state = MinigameState.Ending;

        if (activeRat != null) activeRat.StopRunning();

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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (success && playerMovement != null)
            playerMovement.ActivateSpeedBoost(speedBoostDuration, speedBoostMultiplier);

        activeRat = null;
        state = MinigameState.Idle;
    }
}