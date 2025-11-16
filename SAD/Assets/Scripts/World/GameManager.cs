using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    int matchScore;
    
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
    }
    public void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    character = (int)Character.Cat;
        //    Debug.Log($"[CHEAT] Trocou para Gato");
        //}
        //else if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    character = (int)Character.Dog;
        //    Debug.Log($"[CHEAT] Trocou para Cachorro");
        //}
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

        UIManager.instance.mobileHUD.gameObject.SetActive(GameManager.instance.isMobile);
        UIManager.instance.gameUI.gameObject.SetActive(true);

        AudioManager.Instance.sfxSource = GameObject.FindWithTag("Player").GetComponent<AudioSource>();

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

    public void ChooseCharacter(int character)
    {
        selectedCharacter = (AvailableCharacters)character;
    }
}
