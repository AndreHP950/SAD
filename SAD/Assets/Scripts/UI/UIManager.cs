using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Base UI")]
    public Transform gameUI;
    public Transform transition;
    public Transform mobileHUD;
    public Joystick joystick;
    public Button jumpButton;

    [Header("Animators")]
    public Animator UIAnimator;

    [Header("Settings Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider effectsSlider;

    [Header("Instructional Text")]
    [Tooltip("Referência ao controlador de instruções dentro do GameUI.")]
    public InstructionalTextController instructionalTextController;

    [Header("HUD Toggle")]
    [Tooltip("Tecla para esconder/mostrar o HUD.")]
    public KeyCode hudToggleKey = KeyCode.F1;
    private bool isHudVisible = true;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UIAnimator = GetComponent<Animator>();
        gameUI = transform.Find("GameUI");
        transition = transform.Find("Transition");
        mobileHUD = transform.Find("GameUI/MobileUI");
        joystick = GameObject.Find("GameUI/MobileUI/Joystick").GetComponent<Joystick>();
        jumpButton = GameObject.Find("GameUI/MobileUI/Jump").GetComponent<Button>();

        // Encontra o InstructionalTextController
        if (gameUI != null)
        {
            instructionalTextController = gameUI.GetComponentInChildren<InstructionalTextController>(true);
        }

        if (!transition.gameObject.activeInHierarchy) transition.gameObject.SetActive(true);

        // NÃO desativa o GameUI aqui - deixa o OnSceneLoaded cuidar disso
        // Isso evita conflitos de ativação/desativação

        if (!GameManager.instance.isMobile)
        {
            mobileHUD.gameObject.SetActive(false);
        }

        // Registra callback para mudança de cena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMaster);

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMusic);

        effectsSlider.onValueChanged.RemoveAllListeners();
        effectsSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeEffects);

        // Configura estado inicial baseado na cena atual
        ConfigureUIForScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        // Toggle HUD com F1
        if (Input.GetKeyDown(hudToggleKey))
        {
            ToggleHUD();
        }
    }

    private void ToggleHUD()
    {
        if (gameUI == null) return;

        isHudVisible = !isHudVisible;
        gameUI.gameObject.SetActive(isHudVisible);
    }

    /// <summary>
    /// Define a visibilidade do HUD manualmente.
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        if (gameUI == null) return;

        isHudVisible = visible;
        gameUI.gameObject.SetActive(isHudVisible);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[UIManager] Scene loaded: {scene.name}");
        ConfigureUIForScene(scene.name);
    }

    private void ConfigureUIForScene(string sceneName)
    {
        if (sceneName == "Game")
        {
            // Ativa o GameUI (respeita o estado do toggle)
            if (gameUI != null && isHudVisible && !gameUI.gameObject.activeSelf)
            {
                gameUI.gameObject.SetActive(true);
            }

            // Inicia o sistema de instruções com delay
            StartCoroutine(StartInstructionalSystemDelayed());
        }
        else if (sceneName == "MainMenu")
        {
            // Desativa o GameUI no menu
            if (gameUI != null && gameUI.gameObject.activeSelf)
            {
                gameUI.gameObject.SetActive(false);
            }
            // Reseta o estado do HUD para quando voltar ao jogo
            isHudVisible = true;
        }
    }

    private IEnumerator StartInstructionalSystemDelayed()
    {
        // Espera um tempo fixo para garantir que tudo está inicializado
        yield return new WaitForSecondsRealtime(0.1f);

        StartInstructionalSystem();
    }

    private void StartInstructionalSystem()
    {
        // Tenta encontrar o controller se não tiver referência
        if (instructionalTextController == null && gameUI != null)
        {
            instructionalTextController = gameUI.GetComponentInChildren<InstructionalTextController>(true);
        }

        if (instructionalTextController == null)
        {
            Debug.LogWarning("[UIManager] InstructionalTextController not found!");
            return;
        }

        Debug.Log("[UIManager] Starting instructional system");

        // Garante que o objeto está ativo
        if (!instructionalTextController.gameObject.activeSelf)
        {
            instructionalTextController.gameObject.SetActive(true);
        }

        // Reseta e inicia a sequência
        instructionalTextController.ResetAllState();
        instructionalTextController.StartCoroutine(instructionalTextController.MainInstructionSequence());
    }

    public void TimesUp(bool activate)
    {
        Transform timesUp = gameUI.transform.Find("TimesUp");
        if (activate) timesUp.gameObject.SetActive(true);
        else timesUp.gameObject.SetActive(false);
    }
}