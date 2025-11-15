using UnityEngine;

public class AreaStartingPointScript : MonoBehaviour
{
    private DeliveryController deliveryController;

    private void Start()
    {
        deliveryController = GameObject.Find("Mailboxes").GetComponent<DeliveryController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player entrou na nova area");
            deliveryController.EndAreaChange();
        }
    }
}
