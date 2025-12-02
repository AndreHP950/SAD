using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class MatchTimeController : MonoBehaviour
{
    public float maxTime = 120f;
    public float currentTime;
    [SerializeField] TextMeshProUGUI matchTimer;
    TextMeshProUGUI pauseMatchTimer;
    [SerializeField] DeliveryController deliveryController;
    [SerializeField] ScoreController scoreController;

    void Start()
    {
        matchTimer = UIManager.instance.gameUI.transform.Find("GameTime").GetComponent<TextMeshProUGUI>();
        pauseMatchTimer = UIManager.instance.gameUI.transform.Find("Phone/Screen/PauseMenu/ScoreTime/PauseTimeText").GetComponent<TextMeshProUGUI>();
        deliveryController = GetComponent<DeliveryController>();
        scoreController = GameObject.Find("Mailboxes").GetComponent<ScoreController>();
        currentTime = maxTime;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            matchTimer.text = currentTime.ToString("F0");
            pauseMatchTimer.text = currentTime.ToString("F0");
        }
        else
        {
            currentTime = 0;
            matchTimer.text = currentTime.ToString("F0");
            pauseMatchTimer.text = currentTime.ToString("F0");
            TimeEnd();
        }
    }

    void TimeEnd()
    {
        deliveryController.EndAllDeliveries();
        UIManager.instance.TimesUp(true);
        GameManager.instance.EndGame(scoreController.score);
    }
}
