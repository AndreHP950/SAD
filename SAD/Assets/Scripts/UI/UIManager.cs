using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Base UI")]
    public Transform gameUI;
    public Transform transition;

    [Header("Animators")]
    public Animator UIAnimator;

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
        if (!transition.gameObject.activeInHierarchy) transition.gameObject.SetActive(true);
    }

    public void TimesUp(bool activate)
    {
        Transform timesUp = gameUI.transform.Find("TimesUp");
        if (activate) timesUp.gameObject.SetActive(true);
        else timesUp.gameObject.SetActive(false);
    }
}
