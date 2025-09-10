using Unity.VisualScripting;
using UnityEngine;
using System;

public class PlayerCollisionDetection : MonoBehaviour
{
    [SerializeField] DeliveryController deliveryController;
    int boxNumber = 0;
    private void Start()
    {
        GameObject obj = GameObject.Find("MailBoxes");
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
            }
            else
            {
                deliveryController.EndDelivery(boxNumber);
            }
        }
    }
}
