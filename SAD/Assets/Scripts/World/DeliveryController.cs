using Unity.Jobs;
using UnityEngine;

public class DeliveryController : MonoBehaviour
{
    public bool isDelivering = false;
    float deliverTime = 0f;
    int deliverGoal = 0;

    [Header("Distance Values")]
    [SerializeField] int distanceClose = 25;
    [SerializeField] int distanceFar = 35;
    float mailboxDistance = 0f;

    public Transform[] mailboxes = null;
    public int[] mailboxesTargets = null;
    

    void Start()
    {
        GetMailboxes();
        if (mailboxes.Length > 1) CreateDelivery(-1);
    }

    void Update()
    {
        if (isDelivering)
        {

        }
    }

    void GetMailboxes()
    {
        int count = 0;

        foreach (Transform mailbox in transform)
        {
            if (mailbox.CompareTag("Mailbox"))
            {
                count++;
            }
        }

        mailboxes = new Transform[count];
        mailboxesTargets = new int[count];
        count = 0;

        foreach (Transform mailbox in transform)
        {
            if (mailbox.CompareTag("Mailbox"))
            {
                mailboxes[count] = mailbox;
                count++;
            }
        }
    }

    void CreateDelivery(int boxNumber)
    {
        for (int i = 0;  i < mailboxesTargets.Length; i++)
        {
            if (i != boxNumber)
            {
                mailboxes[i].GetComponent<SphereCollider>().enabled = true;

                do
                {
                    mailboxesTargets[i] = Random.Range(0, mailboxes.Length);
                }
                while (i == mailboxesTargets[i]);

                mailboxDistance = Vector3.Distance(mailboxes[i].position, mailboxes[mailboxesTargets[i]].position);

                if (mailboxDistance <= distanceClose)
                {
                    Transform marker = mailboxes[i].transform.Find("Markers/DistanceClose");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else if (mailboxDistance > distanceClose && mailboxDistance < distanceFar)
                {
                    Transform marker = mailboxes[i].transform.Find("Markers/DistanceMedium");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else
                {
                    Transform marker = mailboxes[i].transform.Find("Markers/DistanceFar");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
            }
        }      
    }

    public void StartDelivery(int boxNumber)
    {
        isDelivering = true;

        deliverGoal = mailboxesTargets[boxNumber];

        for (int i = 0; i < mailboxes.Length; i++)
        {
            Transform markers = mailboxes[i].transform.Find("Markers");
            if (markers != null)
            {
                foreach (Transform marker in markers)
                {
                    if (marker.gameObject.activeSelf)
                    {
                        marker.gameObject.SetActive(false);
                    }
                }
            }
            if (i != deliverGoal)
            {
                mailboxes[i].GetComponent<SphereCollider>().enabled = false;
            }
            else
            {
                Transform goalMarker = mailboxes[i].transform.Find("Markers/TargetMarker");
                goalMarker.gameObject.SetActive(true);
            }
        }
    }

    public void EndDelivery(int boxNumber)
    {
        isDelivering = false;

        Transform goalMarker = mailboxes[boxNumber].transform.Find("Markers/TargetMarker");
        goalMarker.gameObject.SetActive(false);
        mailboxes[boxNumber].GetComponent<SphereCollider>().enabled = false;

        CreateDelivery(boxNumber);
    }
}
