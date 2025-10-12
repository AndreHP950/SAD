using UnityEngine;

public class MinimapCameraController : MonoBehaviour
{
    GameObject player;
    private void Start()
    {
        player = GameObject.FindWithTag("Player");
    }
    void Update()
    {
        transform.position = new Vector3(player.transform.position.x, 200, player.transform.position.z);
    }
}
