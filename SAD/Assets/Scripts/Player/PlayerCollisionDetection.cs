using Unity.VisualScripting;
using UnityEngine;
using System;

public class PlayerCollisionDetection : MonoBehaviour
{
    [SerializeField] DeliveryController deliveryController;
    public int boxNumber = 0;
    public bool mailboxRange = false;
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
            mailboxRange = true;

            if (!deliveryController.isDelivering)
            {
                deliveryController.StartDelivery(boxNumber);
                Debug.Log($"Started Delivery: {boxNumber}");
            }
            else
            {
                deliveryController.EndDelivery(boxNumber, true); 
                Debug.Log($"Ended Delivery: {boxNumber}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Mailbox")) mailboxRange = false;
    }
}
