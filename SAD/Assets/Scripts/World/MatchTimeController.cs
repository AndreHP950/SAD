using System.Collections;
using TMPro;
using UnityEngine;

public class MatchTimeController : MonoBehaviour
{
    public float maxTime = 120f;
    public float currentTime;
    [SerializeField] TextMeshProUGUI matchTimer;
    TextMeshProUGUI pauseMatchTimer;
    [SerializeField] DeliveryController deliveryController;
    [SerializeField] ScoreController scoreController;

    [Header("Aviso de Tempo")]
    [Tooltip("Tempo restante para mostrar o aviso.")]
    public float warningTime = 20f;
    
    private bool hasShownTimeWarning = false;

    void Start()
    {
        matchTimer = UIManager.instance.gameUI.transform.Find("GameTime").GetComponent<TextMeshProUGUI>();
        pauseMatchTimer = UIManager.instance.gameUI.transform.Find("Phone/Screen/PauseMenu/ScoreTime/PauseTimeText").GetComponent<TextMeshProUGUI>();
        deliveryController = GetComponent<DeliveryController>();
        scoreController = GameObject.Find("Mailboxes").GetComponent<ScoreController>();
        currentTime = maxTime;
        hasShownTimeWarning = false;
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            matchTimer.text = currentTime.ToString("F0");
            pauseMatchTimer.text = currentTime.ToString("F0");

            // Verifica se deve mostrar o aviso de tempo
            if (!hasShownTimeWarning && currentTime <= warningTime)
            {
                hasShownTimeWarning = true;
                ShowTimeWarning();
            }
        }
        else
        {
            currentTime = 0;
            matchTimer.text = currentTime.ToString("F0");
            pauseMatchTimer.text = currentTime.ToString("F0");
            TimeEnd();
        }
    }

    void ShowTimeWarning()
    {
        if (InstructionalTextController.Instance != null)
        {
            InstructionalTextController.Instance.NotifyTimeRunningOut();
        }
    }

    void TimeEnd()
    {
        deliveryController.EndAllDeliveries();
        UIManager.instance.TimesUp(true);
        GameManager.instance.EndGame(scoreController.score);
    }
}