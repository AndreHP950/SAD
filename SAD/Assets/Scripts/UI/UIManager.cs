using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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

        if (!transition.gameObject.activeInHierarchy) transition.gameObject.SetActive(true);
        if (SceneManager.GetActiveScene().name == "Game" && gameUI.gameObject.activeInHierarchy == false) gameUI.gameObject.SetActive(true);
        else if (SceneManager.GetActiveScene().name == "MainMenu") gameUI.gameObject.SetActive(false);
    }

    private void Start()
    {
        masterSlider.onValueChanged.RemoveAllListeners();
        masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMaster);

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMusic);

        effectsSlider.onValueChanged.RemoveAllListeners();
        effectsSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeEffects);
    }

    public void TimesUp(bool activate)
    {
        Transform timesUp = gameUI.transform.Find("TimesUp");
        if (activate) timesUp.gameObject.SetActive(true);
        else timesUp.gameObject.SetActive(false);
    }
}
