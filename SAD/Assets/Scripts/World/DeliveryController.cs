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
    public GameObject spawnedPackage; // Referência para o pacote instanciado
    public Vector3 packageInitialPosition; // Posição inicial para o cálculo da animação
}

public class DeliveryController : MonoBehaviour
{
    public bool isDelivering = false;
    public bool isFailed = false;
    public bool startChangingArea = false;
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
    private float timeDivider;

    [Header("Package Values")]
    [Tooltip("Lista de prefabs de pacotes para instanciar aleatoriamente.")]
    public List<GameObject> packagePrefabs = new List<GameObject>();
    PlayerBackpack playerBackpack;

    [Header("Package Animation")]
    [Tooltip("Velocidade da flutuação vertical.")]
    [SerializeField] private float bobbingSpeed = 1.5f;
    [Tooltip("Altura máxima da flutuação vertical.")]
    [SerializeField] private float bobbingHeight = 0.25f;
    [Tooltip("Velocidade da rotação no eixo Y.")]
    [SerializeField] private float rotationSpeed = 50f;
    public GameObject deliveryVFX;
    [Header("Mailboxes Values")]
    public List<Mailbox> mailboxesArea1 = new List<Mailbox>();
    public List<Mailbox> mailboxesArea2 = new List<Mailbox>();
    public List<Mailbox> mailboxesArea3 = new List<Mailbox>();
    public List<Mailbox> mailboxes;

    [Header("Areas")]
    public PlayableAreas currentArea;
    public Collider[] areaColliders;
    private int areasCompleted = 0;
    public Transform[] areaStartingPoints;
    private int nextAreaValue;
    public GameObject[] areaBridges;
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
        minimapTargetIndicator = UIManager.instance.transform.Find("GameUI/Phone/Screen/Map").GetComponent<MinimapTargetIndicator>();
        matchTimeController = GetComponent<MatchTimeController>();

        currentArea = (PlayableAreas)((int)GameManager.instance.CurrentCharacter.startArea);

        GetMailboxes();
        if (mailboxes.Count > 1) CreateDelivery(-1);

        timeDivider = GameManager.instance.CurrentCharacter.characterPrefab.GetComponent<PlayerMovementThirdPerson>().speed / 10;
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
        else
        {
            // Anima os pacotes que estão esperando para serem coletados
            AnimateCollectablePackages();
        }

        if (Input.GetKeyDown(KeyCode.L))
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
                mailboxes.AddRange(mailboxesArea1);
                break;
            case 2:
                mailboxes.AddRange(mailboxesArea2);
                break;
            case 3:
                mailboxes.AddRange(mailboxesArea3);
                break;
            default:
                mailboxes.AddRange(mailboxesArea1);
                break;
        }
    }


    public void CreateDelivery(int boxNumber)
    {
        // Limpa pacotes antigos antes de criar novos
        foreach (var mb in mailboxes)
        {
            if (mb.spawnedPackage != null)
            {
                Destroy(mb.spawnedPackage);
            }
        }

        for (int i = 0; i < mailboxes.Count; i++)
        {
            if (i != boxNumber)
            {
                mailboxes[i].available = true;

                do
                {
                    mailboxes[i].target = Random.Range(0, mailboxes.Count);
                }
                while (i == mailboxes[i].target);

                // Instancia o pacote para coleta
                SpawnPackageForCollection(mailboxes[i]);

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

            GameObject packageToDeliver = mailboxes[boxNumber].spawnedPackage;
            mailboxes[boxNumber].spawnedPackage = null;

            // Verifica se o jogador tem o componente PlayerBackpack
            if (playerBackpack != null)
            {
                // Se tiver, entrega o pacote para ele gerenciar
                playerBackpack.ReceivePackage(packageToDeliver);
            }
            else
            {
                // Se não tiver, simplesmente destrói o objeto do pacote
                if (packageToDeliver != null)
                {
                    Destroy(packageToDeliver);
                }
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("DeliveryCollect");

            deliverGoal = mailboxes[boxNumber].target;
            mailboxDistance = Vector3.Distance(mailboxes[boxNumber].mailbox.transform.position, mailboxes[mailboxes[boxNumber].target].mailbox.transform.position);
            currentDeliveryTime = (int)mailboxDistance / DistanceDivisionValue;

            if (currentDeliveryTime < 10) currentDeliveryTime = 10;
            else if (currentDeliveryTime > 50) currentDeliveryTime = 50;

            matchTimeController.currentTime += (int)(currentDeliveryTime / timeDivider);

            for (int i = 0; i < mailboxes.Count; i++)
            {
                if (i != boxNumber && mailboxes[i].spawnedPackage != null)
                {
                    Destroy(mailboxes[i].spawnedPackage);
                    mailboxes[i].spawnedPackage = null;
                }

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

            minimapTargetIndicator.target = mailboxes[mailboxes[boxNumber].target].mailbox.transform;
            isDelivering = true;

            // Notifica o sistema de instruções que um pacote foi coletado
            if (InstructionalTextController.Instance != null)
            {
                InstructionalTextController.Instance.NotifyPackagePickedUp();
            }
        }
    }

    public void EndDelivery(int boxNumber, bool scoring)
    {
        if (scoring)
        {
            Debug.Log($"Ended Delivery: {boxNumber}");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX("DeliverySuccess");

            // Entrega visual
            DeliverPackage(mailboxes[boxNumber].mailbox.transform);

            // Balança levemente a caixa de correio no eixo Z
            var mailboxTransform = mailboxes[boxNumber].mailbox.transform;
            PlayMailboxSway(mailboxTransform);
            // 3. Instancia o VFX acima da mailbox, virado para cima
            if (deliveryVFX != null)
            {

                // um pouquinho mais baixo
                const float vfxHeightOffset = 0.75f;

                // usa o eixo Y mundial para evitar saídas laterais
                Vector3 spawnPos = mailboxTransform.position + Vector3.up * vfxHeightOffset;

                GameObject vfx = Instantiate(deliveryVFX, spawnPos, Quaternion.identity);

                // opcional: parentear para acompanhar a caixa
                vfx.transform.SetParent(mailboxTransform, true);

                // garante cone para cima (se seu cone usa Z como eixo, troque por forward)
                 vfx.transform.forward = Vector3.up;
            }
            int deliveryScore = (int)mailboxDistance * scoreDistanceMultiplier + baseDeliveryScoreValue;
            scoreController.ChangeScore(deliveryScore);
            Debug.Log($"Distance: {(int)mailboxDistance} | Score: {deliveryScore}");

            // Notifica o sistema de instruções que uma entrega foi concluída
            if (InstructionalTextController.Instance != null)
            {
                InstructionalTextController.Instance.NotifyDeliveryCompleted();
            }
        }
        else
        {
            ThrowAwayPackage();
        }

        CreateDelivery(boxNumber);
        if (boxNumber >= 0)
        {
            Transform goalMarker = mailboxes[boxNumber].mailbox.transform.Find("Markers/TargetMarker");
            if (goalMarker != null) goalMarker.gameObject.SetActive(false);
        }
        deliveryTime.text = null;
        minimapTargetIndicator.target = null;
        isDelivering = false;
        isFailed = false;
    }

    // Pequena animação de balanço em Z (entre -5 e 5 graus) com amortecimento
    private void PlayMailboxSway(Transform mailboxTransform)
    {
        if (mailboxTransform == null) return;
        // inicia uma corrotina local para a animação
        StartCoroutine(MailboxSwayCoroutine(mailboxTransform));
    }

    private IEnumerator MailboxSwayCoroutine(Transform mailboxTransform)
    {
        // guarda rotação e escala iniciais
        Quaternion startRot = mailboxTransform.localRotation;
        Vector3 startScale = mailboxTransform.localScale;
        Vector3 targetScale = startScale * 1.15f; // Aumenta a escala em 15%

        // escolhe direção inicial (-5 ou 5)
        float maxAngle = 5f;
        float firstAngle = Random.value < 0.5f ? -maxAngle : maxAngle;

        // tempos pequenos para parecer um balanço
        float t1 = 0.12f;  // ida (balanço e escala para cima)
        float t2 = 0.10f;  // retorno para o outro lado
        float t3 = 0.25f;  // volta ao original com amortecimento (escala para baixo)

        // --- Etapa 1: Balanço inicial e aumento de escala ---
        float timer = 0f;
        Quaternion fromRot = startRot;
        Quaternion toRot = startRot * Quaternion.Euler(0f, 0f, firstAngle);
        Vector3 fromScale = startScale;

        while (timer < t1)
        {
            timer += Time.deltaTime;
            float k = Mathf.Clamp01(timer / t1);
            k = k * k * (3f - 2f * k); // Ease in-out

            mailboxTransform.localRotation = Quaternion.Slerp(fromRot, toRot, k);
            mailboxTransform.localScale = Vector3.Lerp(fromScale, targetScale, k);
            yield return null;
        }

        // --- Etapa 2: Balanço para o lado oposto ---
        timer = 0f;
        fromRot = mailboxTransform.localRotation;
        float secondAngle = -firstAngle * 0.6f;
        toRot = startRot * Quaternion.Euler(0f, 0f, secondAngle);

        while (timer < t2)
        {
            timer += Time.deltaTime;
            float k = Mathf.Clamp01(timer / t2);
            k = k * k * (3f - 2f * k);

            mailboxTransform.localRotation = Quaternion.Slerp(fromRot, toRot, k);
            yield return null;
        }

        // --- Etapa 3: Volta à rotação e escala originais ---
        timer = 0f;
        fromRot = mailboxTransform.localRotation;
        fromScale = mailboxTransform.localScale;

        while (timer < t3)
        {
            timer += Time.deltaTime;
            float k = Mathf.Clamp01(timer / t3);
            k = k * k * (3f - 2f * k);

            mailboxTransform.localRotation = Quaternion.Slerp(fromRot, startRot, k);
            mailboxTransform.localScale = Vector3.Lerp(fromScale, startScale, k);
            yield return null;
        }

        // garante retorno exato
        mailboxTransform.localRotation = startRot;
        mailboxTransform.localScale = startScale;
    }


    public void FailedDelivery()
    {
        isFailed = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("CarBreak");
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

            if (mailboxes[i].spawnedPackage != null)
            {
                Destroy(mailboxes[i].spawnedPackage);
            }
        }
    }

    private void SpawnPackageForCollection(Mailbox mailbox)
    {
        if (packagePrefabs == null || packagePrefabs.Count == 0)
        {
            Debug.LogError("A lista 'packagePrefabs' está vazia! Adicione os prefabs de pacote no Inspector.");
            return;
        }

        int randomIndex = Random.Range(0, packagePrefabs.Count);
        GameObject randomPackagePrefab = packagePrefabs[randomIndex];

        Vector3 spawnPosition = mailbox.mailbox.transform.position + new Vector3(0, 1.3f, 0);

        GameObject package = Instantiate(randomPackagePrefab, spawnPosition, mailbox.mailbox.transform.rotation);
        mailbox.spawnedPackage = package;
        mailbox.packageInitialPosition = package.transform.position;
    }

    private void AnimateCollectablePackages()
    {
        foreach (var mailbox in mailboxes)
        {
            if (mailbox.spawnedPackage != null)
            {
                float newY = mailbox.packageInitialPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
                mailbox.spawnedPackage.transform.position = new Vector3(mailbox.spawnedPackage.transform.position.x, newY, mailbox.spawnedPackage.transform.position.z);

                mailbox.spawnedPackage.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void DeliverPackage(Transform mailbox)
    {
        // Só tenta entregar se o jogador tiver uma mochila
        if (playerBackpack != null)
        {
            playerBackpack.DeliverPackage(mailbox);
        }
    }

    private void ThrowAwayPackage()
    {
        // Só tenta jogar fora se o jogador tiver uma mochila
        if (playerBackpack != null)
        {
            playerBackpack.DeliverPackage(null);
        }
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

    private void UnlockNextArea()
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

        startChangingArea = false;

        // Notifica o sistema de instruções que a área foi desbloqueada
        if (InstructionalTextController.Instance != null)
        {
            InstructionalTextController.Instance.NotifyAreaUnlocked();
        }
    }

    private void StartAreaChange(int nextArea)
    {
        nextAreaValue = nextArea - 1;

        matchTimeController.currentTime += (int)((int)Vector3.Distance(player.transform.position, areaStartingPoints[nextAreaValue].transform.position) / DistanceDivisionValue / timeDivider);

        minimapTargetIndicator.target = areaStartingPoints[nextAreaValue];
        areaStartingPoints[nextAreaValue].gameObject.SetActive(true);

        areaBridges[nextAreaValue].gameObject.GetComponent<Animator>().SetTrigger("Activate");

        mailboxes.Clear();

        if (areasCompleted >= 3)
        {
            mailboxes.AddRange(mailboxesArea1);
            mailboxes.AddRange(mailboxesArea2);
            mailboxes.AddRange(mailboxesArea3);

            // Notifica que o mapa inteiro foi liberado!
            if (InstructionalTextController.Instance != null)
            {
                InstructionalTextController.Instance.NotifyFullMapUnlocked();
            }
        }
        else
        {
            switch (nextArea)
            {
                case 1:
                    mailboxes.AddRange(mailboxesArea1);
                    break;
                case 2:
                    mailboxes.AddRange(mailboxesArea2);
                    break;
                case 3:
                    mailboxes.AddRange(mailboxesArea3);
                    break;
            }
        }
    }

    public void EndAreaChange()
    {
        CreateDelivery(-1);

        minimapTargetIndicator.target = null;
        areaStartingPoints[nextAreaValue].gameObject.SetActive(false);

        // Notifica que o jogador chegou na nova área
        if (InstructionalTextController.Instance != null)
        {
            InstructionalTextController.Instance.NotifyArrivedAtNewArea();
        }
    }
}