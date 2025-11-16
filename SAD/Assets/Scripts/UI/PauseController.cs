using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{

    //public GameObject pauseMenu;
    public bool IsPaused => Time.timeScale < 0.1f;
    bool isCooldown = false;
    public float cooldownTime = 0.5f;
    float currentCooldown;
    int pauseLocation = 0;

    Transform gameTime;
    Transform score;
    Transform mobileUI;


    public GameObject[] menusInPause;

    [Header("GameUI Buttons")]
    Button phoneButton;

    private void Start()
    {
        phoneButton = transform.Find("GameUI/Phone").GetComponent<Button>();

        gameTime = transform.Find("GameUI/GameTime");
        score = transform.Find("GameUI/Score");
        mobileUI = transform.Find("GameUI/MobileUI");
    }


    public void Update()
    {
        if (isCooldown)
        {
            Cooldown();
        }
        else
        {
            if (SceneManager.GetActiveScene().name == "Game")
            {
                switch (pauseLocation)
                {
                    case 0: //Minimap - Playing
                        if (Input.GetKey(KeyCode.Escape))
                        {
                            Pause();
                        }
                        break;
                    case 1: //Pause Menu
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            Resume();
                        }
                        break;
                    case 2: //Restart Menu
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            PauseGoTo(1);
                        }
                        break;
                    case 3: //Settings Menu
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            PauseGoTo(1);
                        }
                        break;
                    case 4: //Exit Menu
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            PauseGoTo(1);
                        }
                        break;
                }
            }
        }
        
    }

    public void PauseGoTo(int location)
    {
        if (pauseLocation == 1)
        {
            switch (location)
            {
                case 2:
                    UIManager.instance.UIAnimator.SetTrigger("OpenRestart");
                    break;
                case 3:
                    GetSliderValues();
                    UIManager.instance.UIAnimator.SetTrigger("OpenSettings");
                    break;
                case 4:
                    UIManager.instance.UIAnimator.SetTrigger("OpenExit");
                    break;
            }
        }
        else
        {
            switch (pauseLocation)
            {
                case 2:
                    if (location == 1) UIManager.instance.UIAnimator.SetTrigger("CloseRestart");
                    break;
                case 3:
                    if (location == 1) UIManager.instance.UIAnimator.SetTrigger("CloseSettings");
                    break;
                case 4:
                    if (location == 1) UIManager.instance.UIAnimator.SetTrigger("CloseExit");
                    break;
            }
        }
        DisableButtons();
        pauseLocation = location;
        EnableButtons();
    }

    void DisableButtons()
    {
        if (menusInPause[pauseLocation] != null)
        {
            Button[] buttons = menusInPause[pauseLocation].GetComponentsInChildren<Button>(true);

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
        if (menusInPause[pauseLocation] != null)
        {
            Button[] buttons = menusInPause[pauseLocation].GetComponentsInChildren<Button>(true);

            if (buttons.Length > 0)
            {
                foreach (Button button in buttons)
                {
                    button.enabled = true;
                }
            }
        }
    }

    void GetSliderValues()
    {
        UIManager.instance.masterSlider.value = PlayerPrefs.GetFloat("masterSlider", 1f);
        UIManager.instance.musicSlider.value = PlayerPrefs.GetFloat("musicSlider", 1f);
        UIManager.instance.effectsSlider.value = PlayerPrefs.GetFloat("effectsSlider", 1f);
    }

    public void Pause()
    {
        if (!isCooldown)
        {
            Time.fixedDeltaTime = 0f;
            Time.timeScale = 0f;
            PullPhone();
            StartCooldown();
            if (!GameManager.instance.isMobile) ActivateMouse(true);
            phoneButton.enabled = false;
            pauseLocation = 1;
            EnableButtons();

            gameTime.gameObject.SetActive(false);
            score.gameObject.SetActive(false);
            if (GameManager.instance.isMobile) mobileUI.gameObject.SetActive(false);
        }  
    }

    public void Resume()
    {
        if (!isCooldown)
        {
            Time.fixedDeltaTime = 0.02f;
            Time.timeScale = 1f;
            PushPhone();
            StartCooldown();
            if (!GameManager.instance.isMobile) ActivateMouse(false);
            phoneButton.enabled = true;
            DisableButtons();
            pauseLocation = 0;

            gameTime.gameObject.SetActive(true);
            score.gameObject.SetActive(true);
            if (GameManager.instance.isMobile)mobileUI.gameObject.SetActive(true);
        }
    }

    private void PushPhone()
    {
        UIManager.instance.UIAnimator.SetTrigger("OpenMap");
        UIManager.instance.UIAnimator.SetTrigger("PushPhone");
    }

    private void PullPhone()
    {
        UIManager.instance.UIAnimator.SetTrigger("CloseMap");
        UIManager.instance.UIAnimator.SetTrigger("PullPhone");
    }

    private void StartCooldown()
    {
        currentCooldown = cooldownTime;
        isCooldown = true;
    }

    private void Cooldown()
    {
        currentCooldown -= Time.unscaledDeltaTime;
        if (currentCooldown < 0f) isCooldown = false;
    }

    private void ActivateMouse(bool activate)
    {
        if (activate)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void RestartGame()
    {
        PauseGoTo(1);
        Resume();
        GameManager.instance.StartGame();
    }

    public void EndGame()
    {
        int score = GameObject.Find("Mailboxes").GetComponent<ScoreController>().score;
        GameManager.instance.EndGame(score);
        PauseGoTo(1);
        Resume();
        
    }




}