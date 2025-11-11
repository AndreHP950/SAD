using UnityEngine;

public class MinimapCameraController : MonoBehaviour
{
    GameObject player;
    GameObject playerCamera;
    [SerializeField] float rotationSpeed = 5f;

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        playerCamera = Camera.main.gameObject;
    }
    void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(player.transform.position.x, 200, player.transform.position.z);

        Quaternion targetRotation = Quaternion.Euler(90f, playerCamera.transform.eulerAngles.y, 0f);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed *  Time.deltaTime);
    }
}
