using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int matchScore;
    public int completedDeliveries;

    [Header("Characters")]
    public AvailableCharacters selectedCharacter;
    public CharacterData[] characterList;
    public enum AvailableCharacters { Felicia = 0, Doug = 1, Akita = 2 };
    public CharacterData CurrentCharacter => characterList[(int)selectedCharacter];

    [Header("Bools")]
    public bool returningFromGame = false;
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
            return;
        }
    }

    public void Start()
    {
        UIManager.instance.UIAnimator.SetTrigger("Open");

        if (isMobile) Application.targetFrameRate = 60;
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

        yield return null;

        isLoadingScene = false;

        // Configura mobile HUD
        UIManager.instance.mobileHUD.gameObject.SetActive(isMobile);

        // Configura o AudioSource do player
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var audioSource = player.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                AudioManager.Instance.sfxSource = audioSource;
            }

            // Reseta a notificação de movimento do player
            var movement = player.GetComponent<PlayerMovementThirdPerson>();
            if (movement != null)
            {
                movement.ResetMovementNotification();
            }
        }

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

        TextMeshProUGUI finalDeliveryCount = GameObject.Find("Menus/Results/DeliveriesScore").GetComponent<TextMeshProUGUI>();
        finalDeliveryCount.text = completedDeliveries.ToString();

        ResultsRankManager resultRank = GameObject.Find("Menus/Results/Rank").GetComponent<ResultsRankManager>();
        if (matchScore < 30000) resultRank.NewRanking(0);
        else if (matchScore >= 30000 && matchScore < 60000) resultRank.NewRanking(1);
        else if (matchScore >= 60000 && matchScore < 100000) resultRank.NewRanking(2);
        else resultRank.NewRanking(3);

            MenuRecordsManager menuRecords = GameObject.Find("SAD Central Block/RecordsPlates").GetComponent<MenuRecordsManager>();
        menuRecords.UpdateRanking(selectedCharacter, matchScore);

        UIManager.instance.UIAnimator.SetTrigger("Open");
    }

    public void ChooseCharacter(int character)
    {
        selectedCharacter = (AvailableCharacters)character;
    }
}