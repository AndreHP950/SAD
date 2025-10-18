using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{

    //public GameObject pauseMenu;
    public bool IsPaused => Time.timeScale < 0.1f;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }


    public void Pause()
    {
        Time.fixedDeltaTime = 0f;
        Time.timeScale = 0f;
        //pauseMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PullPhone();
    }

    public void Resume()
    {
        Time.fixedDeltaTime = 0.02f;
        Time.timeScale = 1f;
        //pauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PushPhone();
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
}