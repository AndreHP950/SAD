using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class GameManager : MonoBehaviour
{
    [SerializeField]UIManager uiManager;
    [SerializeField]Transform gameUI;
    [SerializeField]Transform timesUp;
    public bool returningFromGame = false;
    int matchScore;

    private bool isLoadingScene = false;

    private void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("GameManager");
        if (objs.Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    public void Start()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        gameUI = uiManager.transform.Find("GameUI");
        timesUp = uiManager.transform.Find("GameUI/TimesUp");
        uiManager.UIAnimator.SetTrigger("Open");
    }

    public void StartGame()
    {
        StartCoroutine(StartGameEvents());
    }

    public void EndGame(int score)
    {
        returningFromGame = true;
        matchScore = score;
        StartCoroutine(EndGameEvents());
    }

    IEnumerator StartGameEvents()
    {
        if (isLoadingScene) yield break;
        isLoadingScene = true;

        timesUp.gameObject.SetActive(false);
        uiManager.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(1f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        isLoadingScene = false;

        gameUI.gameObject.SetActive(true);
        uiManager.UIAnimator.SetTrigger("Open");
    }

    public IEnumerator EndGameEvents()
    {
        if (isLoadingScene) yield break;
        isLoadingScene = true;
        yield return new WaitForSeconds(4f);
        uiManager.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(2f);
        gameUI.gameObject.SetActive(false);

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

        uiManager.UIAnimator.SetTrigger("Open");
    }
}
