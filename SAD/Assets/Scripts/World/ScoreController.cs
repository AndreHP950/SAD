using TMPro;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    public int score = 0;
    [SerializeField] TextMeshProUGUI scoreText;

    private void Start()
    {
        scoreText = UIManager.instance.gameUI.transform.Find("Score/ScoreText").GetComponent<TextMeshProUGUI>();
        scoreText.text = score.ToString();
    }

    public void ChangeScore(int scoreValue)
    {
        score += scoreValue;
        scoreText.text = score.ToString();
    }
}
