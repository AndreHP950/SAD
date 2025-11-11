using Unity.Cinemachine;
using UnityEngine;

public class CameraStartEvents : MonoBehaviour
{
    private void Start()
    {
        GetComponent<CinemachineCamera>().Follow = GameObject.FindWithTag("Player").transform;
    }
}
