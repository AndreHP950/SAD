using TMPro;
using Unity.Jobs;
using UnityEngine;

public class DeliveryController : MonoBehaviour
{
    public bool isDelivering = false;
    public bool isFailed = false;
    int deliverGoal = 0;
    PlayerCollisionDetection playerCollision;

    [Header("Distance Values")]
    [SerializeField] int distanceClose = 25;
    [SerializeField] int distanceFar = 35;
    float mailboxDistance = 0f;

    [Header("Delivery Time Values")]
    public int DistanceDivisionValue = 5;
    public TextMeshProUGUI deliveryTime;
    private float currentDeliveryTime;

    [Header("Score Values")]
    [SerializeField] int baseDeliveryScoreValue = 200;
    [SerializeField] int scoreDistanceMultiplier = 10;
    ScoreController scoreController;

    [Header("Mailboxes Values")]
    public Transform[] mailboxes = null;
    public int[] mailboxesTargets = null;

    

    void Start()
    {
        playerCollision = GameObject.FindWithTag("Player").GetComponent<PlayerCollisionDetection>();
        scoreController = GetComponent<ScoreController>();
        GetMailboxes();
        if (mailboxes.Length > 1) CreateDelivery(-1);
    }

    void Update()
    {
        if (isDelivering)
        {
            if (currentDeliveryTime > 0)
            {
                currentDeliveryTime -= Time.deltaTime;
                deliveryTime.text = currentDeliveryTime.ToString("F0");
            }
            else
            {
                FailedDelivery();
                deliveryTime.text = null;
            }
        }
        else
        {
            deliveryTime.text = null;
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
                Transform marker = mailboxes[i].transform.Find("Markers/TargetMarker");
                if (marker != null) marker.gameObject.SetActive(false);

                if (mailboxDistance <= distanceClose)
                {
                    marker = mailboxes[i].transform.Find("Markers/DistanceClose");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else if (mailboxDistance > distanceClose && mailboxDistance < distanceFar)
                {
                    marker = mailboxes[i].transform.Find("Markers/DistanceMedium");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else
                {
                    marker = mailboxes[i].transform.Find("Markers/DistanceFar");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
            }
        }      
    }

    public void StartDelivery(int boxNumber)
    {
        deliverGoal = mailboxesTargets[boxNumber];
        mailboxDistance = Vector3.Distance(mailboxes[boxNumber].position, mailboxes[mailboxesTargets[boxNumber]].position);
        currentDeliveryTime = (int)mailboxDistance / DistanceDivisionValue;

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
                mailboxes[i].GetComponent<SphereCollider>().enabled = true;
                goalMarker.gameObject.SetActive(true);
            }
            isDelivering = true;
        }
    }

    public void EndDelivery(int boxNumber, bool scoring)
    {
        if (scoring)
        {
            int deliveryScore = (int)mailboxDistance * scoreDistanceMultiplier + baseDeliveryScoreValue;
            scoreController.ChangeScore(deliveryScore);
            Debug.Log($"Distance: {(int)mailboxDistance} | Score: {deliveryScore}");
        }

        CreateDelivery(boxNumber);
        Transform goalMarker = mailboxes[boxNumber].transform.Find("Markers/TargetMarker");
        goalMarker.gameObject.SetActive(false);
        mailboxes[boxNumber].GetComponent<SphereCollider>().enabled = false;
        isDelivering = false;
        isFailed = false; 
    }

    public void FailedDelivery()
    {
        isFailed = true;
        RestoreMailboxesTrigger();
        if (playerCollision.mailboxRange)
        {
            EndDelivery(playerCollision.boxNumber, false);
        }
        else EndDelivery(-1, false);
        
    }

    void RestoreMailboxesTrigger() //Used only on failed deliveries
    {
        for(int i = 0; i < mailboxes.Length; i++)
        {
            mailboxes[i].GetComponent<SphereCollider>().enabled = true;
        }
    }



}
