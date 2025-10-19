using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

//[System.Serializable]
//public class Character
//{
//    public string name;
//}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool returningFromGame = false;
    int matchScore;
    public int character = 1;

    private bool isLoadingScene = false;
    public bool isExiting = false;

    public bool isMobile = false;

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
        }
    }

    public void Start()
    {
        UIManager.instance.UIAnimator.SetTrigger("Open");
    }

    public void StartGame()
    {
        StartCoroutine(StartGameEvents());
    }

    public void EndGame(int score)
    {
        if (Time.timeScale != 1f) Time.timeScale = 1f;
        if (Time.fixedDeltaTime != 0.02f) Time.fixedDeltaTime = 0.02f;
        returningFromGame = true;
        matchScore = score;
        StartCoroutine(EndGameEvents());
    }

    IEnumerator StartGameEvents()
    {
        if (Time.timeScale != 1f) Time.timeScale = 1f;
        if (Time.fixedDeltaTime != 0.02f) Time.fixedDeltaTime = 0.02f;
        isExiting = false;

        if (isLoadingScene) yield break;
        isLoadingScene = true;

        UIManager.instance.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(1f);

        UIManager.instance.TimesUp(false);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isLoadingScene = false;

        UIManager.instance.gameUI.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);
        UIManager.instance.UIAnimator.SetTrigger("Open");
    }

    public IEnumerator EndGameEvents()
    {
        if (isLoadingScene) yield break;
        isLoadingScene = true;
        if (isExiting) yield return new WaitForSeconds(4f);
        UIManager.instance.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(2f);
        UIManager.instance.gameUI.gameObject.SetActive(false);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isLoadingScene = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        TextMeshProUGUI finalScoreText = GameObject.Find("Menus/Results/Score").GetComponent<TextMeshProUGUI>();
        finalScoreText.text = matchScore.ToString();

        UIManager.instance.UIAnimator.SetTrigger("Open");
    }
}
