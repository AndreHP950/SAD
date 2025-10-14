using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{

    public GameObject pauseMenu;
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
        pauseMenu.SetActive(true);
    }

    public void Resume()
    {
        Time.fixedDeltaTime = 0.02f;
        Time.timeScale = 1f;
        pauseMenu.SetActive(false);
    }
}