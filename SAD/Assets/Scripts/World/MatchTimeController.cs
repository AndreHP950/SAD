using System.Collections;
using TMPro;
using UnityEngine;

public class MatchTimeController : MonoBehaviour
{
    public float maxTime = 120f;
    public float currentTime;
    [SerializeField] TextMeshProUGUI matchTimer;
    [SerializeField] GameManager gameManager;
    [SerializeField] DeliveryController deliveryController;
    [SerializeField] ScoreController scoreController;
    [SerializeField] Transform timesUp;

    void Start()
    {
        matchTimer = GameObject.FindWithTag("UIManager").transform.Find("GameUI/GameTime").GetComponent<TextMeshProUGUI>();
        deliveryController = GetComponent<DeliveryController>();
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
        scoreController = GameObject.Find("Mailboxes").GetComponent<ScoreController>();
        timesUp = GameObject.FindWithTag("UIManager").transform.Find("GameUI/TimesUp");
        currentTime = maxTime;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            matchTimer.text = currentTime.ToString("F2");
        }
        else
        {
            currentTime = 0;
            matchTimer.text = currentTime.ToString("F2");
            TimeEnd();
        }
    }

    void TimeEnd()
    {
        deliveryController.EndAllDeliveries();
        timesUp.gameObject.SetActive(true);
        gameManager.EndGame(scoreController.score);
    }
}
