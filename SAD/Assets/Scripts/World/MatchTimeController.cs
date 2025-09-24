using System.Collections;
using TMPro;
using UnityEngine;

public class MatchTimeController : MonoBehaviour
{
    public float maxTime = 120f;
    public float currentTime;
    [SerializeField] TextMeshProUGUI matchTimer;
    [SerializeField] DeliveryController deliveryController;
    [SerializeField] ScoreController scoreController;

    void Start()
    {
        matchTimer = UIManager.instance.gameUI.transform.Find("GameTime").GetComponent<TextMeshProUGUI>();
        deliveryController = GetComponent<DeliveryController>();
        scoreController = GameObject.Find("Mailboxes").GetComponent<ScoreController>();
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
        UIManager.instance.TimesUp(true);
        GameManager.instance.EndGame(scoreController.score);
    }
}
