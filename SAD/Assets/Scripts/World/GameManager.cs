using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager instance;
    [SerializeField]UIManager uiManager;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Start()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        uiManager.UIAnimator.SetTrigger("Open");
    }

    public void StartGame()
    {
        StartCoroutine(StartGameEvents());
    }

    IEnumerator StartGameEvents()
    {
        uiManager.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Game");
        uiManager.UIAnimator.SetTrigger("Open");
    }
}
