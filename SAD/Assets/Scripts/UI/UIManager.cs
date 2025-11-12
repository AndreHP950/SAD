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

    [Header("Animators")]
    public Animator UIAnimator;

    [Header("Settings Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider effectsSlider;

    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("UIManager");
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        UIAnimator = GetComponent<Animator>();
        gameUI = transform.Find("GameUI");
        transition = transform.Find("Transition");
        mobileHUD = transform.Find("GameUI/MobileUI");

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

        if (GameManager.instance.isMobile) mobileHUD.gameObject.SetActive(true);
        else mobileHUD.gameObject.SetActive(false);
    }

    public void TimesUp(bool activate)
    {
        Transform timesUp = gameUI.transform.Find("TimesUp");
        if (activate) timesUp.gameObject.SetActive(true);
        else timesUp.gameObject.SetActive(false);
    }
}
