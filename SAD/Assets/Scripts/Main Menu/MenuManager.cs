using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] CinemachineCamera[] cameraList;
    [SerializeField] Canvas[] canvas;
    public int menuLocation = 0;
    GameManager gameManager;

    public Animator mainMenuMaskAnimator;

    [Header("Settings Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider effectsSlider;

    [Header("Cutscenes")]
    public PlayableDirector logoToMenuCutscene;

    private void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();

        if (gameManager.returningFromGame) MenuGoTo(2);

        masterSlider.onValueChanged.RemoveAllListeners();
        masterSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMaster);

        musicSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeMusic);

        effectsSlider.onValueChanged.RemoveAllListeners();
        effectsSlider.onValueChanged.AddListener(AudioManager.Instance.SetVolumeEffects);
    }

    void Update()
    {
        switch (menuLocation)
        {
            case 0:
                LogoMenu();
                break;
            case 1:
                MainMenu();
                break;
            case 2:
                Results();
                break;
            case 3:
                Records();
                break;
            case 4:
                Configurations();
                break;
            case 5:
                Credits();
                break;
            case 6:
                ExitGame();
                break;
            default:
                break;
        }

        //if (Input.GetKeyDown(KeyCode.O)) mainMenuMaskAnimator.SetTrigger("TurnOn");
        //if (Input.GetKeyDown(KeyCode.P)) mainMenuMaskAnimator.SetTrigger("TurnOff");
    }

    #region Menu

    void LogoMenu() //menuLocation 0
    {
        if (Input.anyKeyDown)
        {
            MenuGoTo(1);
        }
    }

    void MainMenu() //menuLocation 1
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(6);
        }
    }

    void Results() //menuLocation 2
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(1);
        }
    }

    void Records() //menuLocation 3
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(1);
        }
    }

    void Configurations() //menuLocation 4
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(1);
        }
    }

    void Credits() //menuLocation 5
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(1);
        }
    }

    void ExitGame() //menuLocation 6
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            MenuGoTo(1);
        }
    }

    #endregion

    //IEnumerator LogoToMenuCutscene()
    //{
    //    cameraList[menuLocation].enabled = false;
    //    logoToMenuCutscene.Play();
    //    yield return new WaitForSeconds(4.0f);
    //    MenuGoTo(1);
    //}

    public void MenuGoTo(int location)
    {
        DisableButtons();
        cameraList[location].enabled = true;
        cameraList[menuLocation].enabled = false;
        //cameraList[location].Prioritize();
        menuLocation = location;
        EnableButtons();

        if (location == 1) mainMenuMaskAnimator.SetTrigger("TurnOn");
        if (location == 4) GetSliderValues();
    }

    private void GetSliderValues()
    {
        masterSlider.value = PlayerPrefs.GetFloat("masterSlider", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("musicSlider", 1f);
        effectsSlider.value = PlayerPrefs.GetFloat("effectsSlider", 1f);
    }

    void DisableButtons()
    {
        if (canvas[menuLocation] != null)
        {
            Button[] buttons = canvas[menuLocation].GetComponentsInChildren<Button>(true);

            if (buttons.Length > 0)
            {
                foreach (Button button in buttons)
                {
                    button.enabled = false;
                }
            }
        }        
    }

    void EnableButtons()
    {
        if (canvas[menuLocation] != null)
        {
            Button[] buttons = canvas[menuLocation].GetComponentsInChildren<Button>(true);

            if (buttons.Length > 0)
            {
                foreach (Button button in buttons)
                {
                    button.enabled = true;
                }
            }
        }        
    }
}
