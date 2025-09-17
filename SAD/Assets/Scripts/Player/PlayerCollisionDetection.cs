using Unity.VisualScripting;
using UnityEngine;
using System;

public class PlayerCollisionDetection : MonoBehaviour
{
    [SerializeField] DeliveryController deliveryController;
    int boxNumber = 0;
    private void Start()
    {
        GameObject obj = GameObject.Find("Mailboxes");
        deliveryController = obj.GetComponent<DeliveryController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.CompareTag("Mailbox"))
        {
            boxNumber = Array.IndexOf(deliveryController.mailboxes, other.transform);
            if (!deliveryController.isDelivering)
            {
                deliveryController.StartDelivery(boxNumber);
                Debug.Log($"Started Delivery: {boxNumber}");
            }
            else
            {
                deliveryController.EndDelivery(boxNumber);
                Debug.Log($"Ended Delivery: {boxNumber}");
            }
        }
    }
}
