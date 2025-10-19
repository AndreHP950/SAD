using TMPro;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public int score = 0;
    [SerializeField] TextMeshProUGUI scoreText;
    TextMeshProUGUI pauseScoreText;

    private void Start()
    {
        scoreText = UIManager.instance.gameUI.transform.Find("Score/ScoreText").GetComponent<TextMeshProUGUI>();
        pauseScoreText = UIManager.instance.gameUI.transform.Find("Phone/Screen/PauseMenu/ScoreTime/PauseScoreText").GetComponent<TextMeshProUGUI>();
        pauseScoreText.text = score.ToString();
        scoreText.text = score.ToString();
    }

    public void ChangeScore(int scoreValue)
    {
        score += scoreValue;
        pauseScoreText.text = score.ToString();
        scoreText.text = score.ToString();
    }
}
