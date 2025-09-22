using TMPro;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public int score = 0;
    [SerializeField] TextMeshProUGUI scoreText;

    private void Start()
    {
        scoreText = GameObject.FindWithTag("UIManager").transform.Find("GameUI/Score/ScoreText").GetComponent<TextMeshProUGUI>();
        scoreText.text = score.ToString();
    }

    public void ChangeScore(int scoreValue)
    {
        score += scoreValue;
        scoreText.text = score.ToString();
    }
}
