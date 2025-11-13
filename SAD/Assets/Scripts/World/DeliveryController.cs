using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class Mailbox
{
    public GameObject mailbox;
    public int target;
    public bool available;
}

public class DeliveryController : MonoBehaviour
{
    public bool isDelivering = false;
    public bool isFailed = false;
    private bool startChangingArea = false;
    public int deliverGoal = 0;
    GameObject player;
    
    PlayerCollisionDetection playerCollision;
    MatchTimeController matchTimeController;

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

    [Header("Package Values")]
    public GameObject packagePrefab;
    public Transform spawnPoint;
    PlayerBackpack playerBackpack;

    [Header("Mailboxes Values")]
    public List<Mailbox> mailboxesArea1 = new List<Mailbox>();
    public List<Mailbox> mailboxesArea2 = new List<Mailbox>();
    public List<Mailbox> mailboxesArea3 = new List<Mailbox>();
    public List<Mailbox> mailboxes;

    [Header("Areas")]
    public PlayableAreas currentArea;
    public Collider[] areaColliders;
    private int areasCompleted = 0;
    public Transform[] areaBridges;
    public enum PlayableAreas { Area1 = 1, Area2 = 2, Area3 = 3, All = 4 };


    [Header("Minimap")]
    public MinimapTargetIndicator minimapTargetIndicator;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        deliveryTime = GameObject.Find("DeliveryTime").GetComponent<TextMeshProUGUI>();
        playerBackpack = player.GetComponent<PlayerBackpack>();
        playerCollision = player.GetComponent<PlayerCollisionDetection>();
        scoreController = GetComponent<ScoreController>();
        minimapTargetIndicator = GameObject.Find("UIManager/GameUI/Phone/Screen/Map").GetComponent<MinimapTargetIndicator>();
        matchTimeController = GetComponent<MatchTimeController>();

        currentArea = (PlayableAreas)((int)GameManager.instance.CurrentCharacter.startArea);

        GetMailboxes();
        if (mailboxes.Count > 1) CreateDelivery(-1);
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
        else if (startChangingArea)
        {
            UnlockNextArea();
        }
    }

    void GetMailboxes()
    {
        GameObject[] mailbox = GameObject.FindGameObjectsWithTag("Mailbox");

        mailboxesArea1.Clear(); mailboxesArea2.Clear(); mailboxesArea3.Clear();

        foreach (GameObject m in mailbox)
        {
            string thisArea = "Unknown";

            foreach (Collider area in areaColliders)
            {
                if (area.bounds.Contains(m.transform.position))
                {
                    thisArea = area.name;
                    break;
                }
            }
            switch (thisArea)
            {
                case "Area 1":
                    mailboxesArea1.Add(new Mailbox { mailbox = m });
                    break;
                case "Area 2":
                    mailboxesArea2.Add(new Mailbox { mailbox = m });
                    break;
                case "Area 3":
                    mailboxesArea3.Add(new Mailbox { mailbox = m });
                    break;
            }
        }

        switch ((int)currentArea)
        {
            case 1:
                mailboxes = mailboxesArea1;
                break;
            case 2:
                mailboxes = mailboxesArea2;
                break;
            case 3:
                mailboxes = mailboxesArea3;
                break;
            default:
                mailboxes = mailboxesArea1;
                break;
        }
    }


    public void CreateDelivery(int boxNumber)
    {
        for (int i = 0;  i < mailboxes.Count; i++)
        {
            if (i != boxNumber)
            {
                mailboxes[i].available = true;

                do
                {
                    mailboxes[i].target = Random.Range(0, mailboxes.Count);
                }
                while (i == mailboxes[i].target);

                mailboxDistance = Vector3.Distance(mailboxes[i].mailbox.transform.position, mailboxes[mailboxes[i].target].mailbox.transform.position);
                Transform marker = mailboxes[i].mailbox.transform.Find("Markers/TargetMarker");
                if (marker != null) marker.gameObject.SetActive(false);

                if (mailboxDistance <= distanceClose)
                {
                    marker = mailboxes[i].mailbox.transform.Find("Markers/DistanceClose");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else if (mailboxDistance > distanceClose && mailboxDistance < distanceFar)
                {
                    marker = mailboxes[i].mailbox.transform.Find("Markers/DistanceMedium");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
                else
                {
                    marker = mailboxes[i].mailbox.transform.Find("Markers/DistanceFar");
                    if (marker != null) marker.gameObject.SetActive(true);
                }
            }
            else mailboxes[i].available = false;
        }      
    }

    public void StartDelivery(int boxNumber)
    {
        if (mailboxes[boxNumber].available)
        {
            Debug.Log($"Started Delivery: {boxNumber}");

            // SFX: Toca som de coleta de caixa
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCollectBoxSFX();
            deliverGoal = mailboxes[boxNumber].target;
            mailboxDistance = Vector3.Distance(mailboxes[boxNumber].mailbox.transform.position, mailboxes[mailboxes[boxNumber].target].mailbox.transform.position);
            currentDeliveryTime = (int)mailboxDistance / DistanceDivisionValue;
            matchTimeController.currentTime += (int)(currentDeliveryTime / 2);

            for (int i = 0; i < mailboxes.Count; i++)
            {
                Transform markers = mailboxes[i].mailbox.transform.Find("Markers");
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
                if (i == deliverGoal)
                {
                    Transform goalMarker = mailboxes[i].mailbox.transform.Find("Markers/TargetMarker");
                    goalMarker.gameObject.SetActive(true);
                }


            }
            GetPackage(mailboxes[boxNumber].mailbox.transform);
            minimapTargetIndicator.target = mailboxes[mailboxes[boxNumber].target].mailbox.transform;
            isDelivering = true;
        }
    }

    public void EndDelivery(int boxNumber, bool scoring)
    {
        if (scoring)
        {
            Debug.Log($"Ended Delivery: {boxNumber}");
            // SFX: Toca som de entrega bem-sucedida
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayDeliverySuccessSFX();

            DeliverPackage(mailboxes[boxNumber].mailbox.transform);
            int deliveryScore = (int)mailboxDistance * scoreDistanceMultiplier + baseDeliveryScoreValue;
            scoreController.ChangeScore(deliveryScore);
            Debug.Log($"Distance: {(int)mailboxDistance} | Score: {deliveryScore}");
        }
        else ThrowAwayPackage();

        CreateDelivery(boxNumber);
        if(boxNumber >= 0)
        {
            Transform goalMarker = mailboxes[boxNumber].mailbox.transform.Find("Markers/TargetMarker");
            goalMarker.gameObject.SetActive(false);
        }
        deliveryTime.text = null;
        minimapTargetIndicator.target = null;
        isDelivering = false;
        isFailed = false; 
    }

    public void FailedDelivery()
    {
        isFailed = true;

        // SFX: Toca som de entrega falhada
        if (AudioManager.Instance != null) 
        { 
            AudioManager.Instance.PlayDeliveryFailedSFX();
        }

        if (!playerCollision.mailboxRange)
        {
            EndDelivery(-1, false);
        }
        else
        {
            EndDelivery(playerCollision.boxNumber, false);
        }
    }

    void StopDeliverySystem()
    {
        for (int i = 0; i < mailboxes.Count; i++)
        {
            mailboxes[i].available = false;
            mailboxes[i].mailbox.transform.Find("Markers/TargetMarker").gameObject.SetActive(false);
            mailboxes[i].mailbox.transform.Find("Markers/DistanceClose").gameObject.SetActive(false);
            mailboxes[i].mailbox.transform.Find("Markers/DistanceMedium").gameObject.SetActive(false);
            mailboxes[i].mailbox.transform.Find("Markers/DistanceFar").gameObject.SetActive(false);
        }
    }

    private void GetPackage(Transform mailbox)
    {
        GameObject package = Instantiate(packagePrefab, mailbox.position, mailbox.rotation);

        playerBackpack.ReceivePackage(package);
    }

    private void DeliverPackage(Transform mailbox)
    {
        playerBackpack.DeliverPackage(mailbox);
    }

    private void ThrowAwayPackage()
    {
        playerBackpack.DeliverPackage(null);
    }

    public void EndAllDeliveries()
    {
        for (int i = 0; i < mailboxes.Count; ++i)
        {
            mailboxes[i].mailbox.GetComponent<SphereCollider>().enabled = false;
            Transform marker = mailboxes[i].mailbox.transform.Find("Markers");
            marker.gameObject.SetActive(false);

        }
    }

    public void UnlockNextArea()
    {
        StopDeliverySystem();

        areasCompleted++;

        switch (currentArea)
        {
            case PlayableAreas.Area1:
                currentArea = PlayableAreas.Area2;
                StartAreaChange(2);
                break;
            case PlayableAreas.Area2:
                currentArea = PlayableAreas.Area3;
                StartAreaChange(3);
                break;
            case PlayableAreas.Area3:
                currentArea = PlayableAreas.Area1;
                StartAreaChange(1);
                break;
        }
    }

    public void StartAreaChange(int nextArea)
    {
        
    }
}
