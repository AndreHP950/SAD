using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class AreaCollectables
{
    public GameObject areaCollectable;
    public int area;
}

public class PlayerCollisionDetection : MonoBehaviour
{
    [SerializeField] DeliveryController deliveryController;
    [Header("VFX")]
    [Tooltip("VFX que toca quando o jogador encosta na água.")]
    [SerializeField] private GameObject waterSplashVFX;
    private ParticleSystem water;
    [Tooltip("Tempo em segundos antes do jogador reaparecer após tocar na água.")]
    [SerializeField] private float waterRespawnDelay = 1f;

    PlayerRespawn playerRespawn;
    private PlayerAnimationController animationController; // Referência para a animação

    public int boxNumber;
    public bool mailboxRange = false;
    public List<AreaCollectables> areaCollectablesList = new List<AreaCollectables>();
    private int area1Collectables = 0;
    private int area2Collectables = 0;
    private int area3Collectables = 0;
    private int currentAreaCollected = 0;
    private bool isRespawningFromWater = false;

    // Referência para a câmera virtual do jogador
    private CinemachineCamera playerVirtualCamera;

    private void Start()
    {
        deliveryController = GameObject.Find("Mailboxes").GetComponent<DeliveryController>();
        playerRespawn = GetComponent<PlayerRespawn>();
        animationController = GetComponentInChildren<PlayerAnimationController>(); // Pega a referência

        // Encontra a câmera virtual do jogador na cena
        playerVirtualCamera = FindFirstObjectByType<CinemachineCamera>();
        if (playerVirtualCamera == null)
        {
            Debug.LogWarning("Nenhuma CinemachineCamera encontrada na cena.", this);
        }

        GetAvailableAreaCollectables();
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.transform.tag)
        {
            case "Mailbox":
                boxNumber = deliveryController.mailboxes.FindIndex(m => m.mailbox == other.gameObject);
                mailboxRange = true;

                if (boxNumber >= 0)
                {
                    if (!deliveryController.isDelivering)
                    {
                        deliveryController.StartDelivery(boxNumber);
                    }
                    else if (!deliveryController.isFailed)
                    {
                        if (boxNumber == deliveryController.deliverGoal)
                        {
                            deliveryController.EndDelivery(boxNumber, true);
                        }
                    }
                }
                break;
            case "Water":
                if (isRespawningFromWater) return;
                StartCoroutine(WaterRespawnRoutine());
                break;

            case "AreaCollectable":
                // Dispara a animação de coleta (usando o padrão, que é a direita)
                if (animationController != null)
                {
                    animationController.TriggerCollectAnimation();
                }

                AreaCollectables data = areaCollectablesList.Find(item => item.areaCollectable == other.gameObject);

                if (data == null)
                {
                    Debug.Log("Collectable not found in list.");
                    return;
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("Collectable");
                }

                // Notifica o sistema de instruções que uma engrenagem foi coletada
                if (InstructionalTextController.Instance != null)
                {
                    InstructionalTextController.Instance.NotifyGearCollected();
                }

                if (data.area == (int)deliveryController.currentArea)
                {
                    currentAreaCollected++;
                    Destroy(other.gameObject);

                    switch (data.area)
                    {
                        case 1:
                            if (currentAreaCollected >= area1Collectables)
                            {
                                deliveryController.startChangingArea = true;
                                currentAreaCollected = 0;
                            }
                            break;
                        case 2:
                            if (currentAreaCollected >= area2Collectables)
                            {
                                deliveryController.startChangingArea = true;
                                currentAreaCollected = 0;
                            }
                            break;
                        case 3:
                            if (currentAreaCollected >= area3Collectables)
                            {
                                deliveryController.startChangingArea = true;
                                currentAreaCollected = 0;
                            }
                            break;
                    }
                }
                break;
        }
    }

    private IEnumerator WaterRespawnRoutine()
    {
        isRespawningFromWater = true;

        // Desativa a câmera virtual para parar de seguir o jogador
        if (playerVirtualCamera != null)
        {
            playerVirtualCamera.enabled = false;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("WaterSplash");
        }

        if (waterSplashVFX != null)
        {
            // Pega o ParticleSystem do VFX
            water = waterSplashVFX.GetComponentInChildren<ParticleSystem>();
            if (water != null)
            {
                // Move o VFX para a posição do jogador antes de tocar
                water.transform.position = transform.position;
                water.Play();
            }
        }

        yield return new WaitForSeconds(waterRespawnDelay);

        playerRespawn.RespawnPlayer();

        // Reativa a câmera virtual após o respawn
        if (playerVirtualCamera != null)
        {
            playerVirtualCamera.enabled = true;
        }

        isRespawningFromWater = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.CompareTag("Mailbox")) mailboxRange = false;
    }

    private void GetAvailableAreaCollectables()
    {
        GameObject[] areaCollectables = GameObject.FindGameObjectsWithTag("AreaCollectable");

        areaCollectablesList.Clear();

        foreach (GameObject a in areaCollectables)
        {
            string thisArea = "Unknown";

            foreach (Collider area in deliveryController.areaColliders)
            {
                if (area.bounds.Contains(a.transform.position))
                {
                    thisArea = area.name;
                    break;
                }
            }
            switch (thisArea)
            {
                case "Area 1":
                    areaCollectablesList.Add(new AreaCollectables { areaCollectable = a, area = 1 });
                    area1Collectables++;
                    break;
                case "Area 2":
                    areaCollectablesList.Add(new AreaCollectables { areaCollectable = a, area = 2 });
                    area2Collectables++;
                    break;
                case "Area 3":
                    areaCollectablesList.Add(new AreaCollectables { areaCollectable = a, area = 3 });
                    area3Collectables++;
                    break;
            }
        }
    }
}