using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    public GameObject player;

    private void Awake()
    {
        if(GameManager.instance == null)
        {
            Debug.Log("GameManager not found");
            return;
        }

        if (GameObject.FindWithTag("Player") == null)
        {
            var character = GameManager.instance.CurrentCharacter;
            Instantiate(character.characterPrefab, character.spawnPosition,character.spawnRotation);
        }
    }
}
