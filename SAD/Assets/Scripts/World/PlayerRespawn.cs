using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 lastSafePosition;
    PlayerMovementThirdPerson playerMovement;
    CinemachineCamera playerCam;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovementThirdPerson>();
        playerCam = GameObject.Find("PlayerCamera").GetComponent<CinemachineCamera>();
        lastSafePosition = transform.position;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("SafePosition"))
        {
            lastSafePosition = transform.position;
        }
    }

    public void RespawnPlayer()
    {
        StartCoroutine(RespawnPlayerCoroutine());
    }

    IEnumerator RespawnPlayerCoroutine()
    {
        playerMovement.isMoving = false;
        playerCam.Follow = null;

        UIManager.instance.UIAnimator.SetTrigger("Close");
        yield return new WaitForSeconds(0.45f);
        transform.position = lastSafePosition + new Vector3(0, 1, 0);
        playerCam.Follow = this.transform;
        UIManager.instance.UIAnimator.SetTrigger("Open");
        playerMovement.isMoving = true;
    }
}
