using Unity.VisualScripting;
using UnityEngine;
using System;

public class PlayerCollisionDetection : MonoBehaviour
{
    [SerializeField] DeliveryController deliveryController;
    public int boxNumber;
    public bool mailboxRange = false;
    private void Start()
    {
        deliveryController = GameObject.Find("Mailboxes").GetComponent<DeliveryController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.transform.CompareTag("Mailbox"))
        {
            boxNumber = deliveryController.mailboxes.FindIndex(m => m.mailbox == other.gameObject);
            mailboxRange = true;

            if (boxNumber >= 0)
            {
                if (!deliveryController.isDelivering)
                {
                    deliveryController.StartDelivery(boxNumber);
                    Debug.Log($"Started Delivery: {boxNumber}");
                }
                else if (!deliveryController.isFailed)
                {
                    if (boxNumber == deliveryController.deliverGoal)
                    {
                        deliveryController.EndDelivery(boxNumber, true);
                        Debug.Log($"Ended Delivery: {boxNumber}");
                    }
                }
            }  
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Mailbox")) mailboxRange = false;
    }
}
