using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public Animator UIAnimator;
    public Transform gameUI;
    public Transform transition;

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
    }

    void Start()
    {
        if (!transition.gameObject.activeInHierarchy) transition.gameObject.SetActive(true);


    }

    public void TimesUp(bool activate)
    {
        Transform timesUp = gameUI.transform.Find("TimesUp");
        if (activate) timesUp.gameObject.SetActive(true);
        else timesUp.gameObject.SetActive(false);
    }
}
