using UnityEngine;

public class AreaCollectablesManager : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Area Collectable"))
        {

        }
    }
}
