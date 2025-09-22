using UnityEngine;

public class PlayButton : MonoBehaviour
{
    GameManager gameManager;

    void Start()
    {
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    public void StartGame()
    {
        gameManager.StartGame();
    }    
}
