using TMPro;
using UnityEngine;

public class MatchTimeController : MonoBehaviour
{
    public float maxTime = 120f;
    public float currentTime;

    [SerializeField] TextMeshProUGUI matchTimer;

    void Start()
    {
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

    }
}
