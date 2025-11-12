using UnityEngine;

public class MinimapCameraController : MonoBehaviour
{
    [SerializeField] GameObject player;
    [SerializeField] GameObject playerCamera;
    [SerializeField] MinimapTargetIndicator targetIndicator;
    [SerializeField] float rotationSpeed = 5f;
    

    private void Start()
    {
        player = GameObject.FindWithTag("Player");
        playerCamera = Camera.main.gameObject;

        targetIndicator = GameObject.FindWithTag("PhoneMap").GetComponent<MinimapTargetIndicator>();
        targetIndicator.minimapCamera = this.GetComponent<Camera>();
    }
    void LateUpdate()
    {
        if (player == null) return;

        transform.position = new Vector3(player.transform.position.x, 200, player.transform.position.z);

        Quaternion targetRotation = Quaternion.Euler(90f, playerCamera.transform.eulerAngles.y, 0f);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed *  Time.deltaTime);
    }
}
