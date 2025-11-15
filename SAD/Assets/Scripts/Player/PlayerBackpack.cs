using UnityEngine;

public class PlayerBackpack : MonoBehaviour
{
    [Header("Configuração dos Slots")]
    [Tooltip("O slot na mão esquerda do personagem. Deixe vazio se este personagem não segura pacotes.")]
    public Transform packageSlotLeft;
    [Tooltip("O slot na mão direita do personagem. Deixe vazio se este personagem não segura pacotes.")]
    public Transform packageSlotRight;

    [Header("Animação do Pacote")]
    public float packageSpeed = 30f;
    public float closeDistance = 0.1f;

    // Estado interno
    private GameObject currentPackage;
    private Transform activePackageSlot;
    private Transform deliveryTarget;
    private bool isMovingToSlot = false;
    private bool isMovingToMailbox = false;

    void Update()
    {
        if (isMovingToSlot && currentPackage != null && activePackageSlot != null)
        {
            MovePackage(activePackageSlot.position, () => {
                currentPackage.transform.SetParent(activePackageSlot, true);
                isMovingToSlot = false;
            });
        }
        else if (isMovingToMailbox && currentPackage != null && deliveryTarget != null)
        {
            MovePackage(deliveryTarget.position, () => {
                Destroy(currentPackage);
                currentPackage = null;
                isMovingToMailbox = false;
            });
        }
    }

    private void MovePackage(Vector3 targetPosition, System.Action onArrived)
    {
        if (currentPackage == null) return;

        currentPackage.transform.position = Vector3.Lerp(currentPackage.transform.position, targetPosition, packageSpeed * Time.deltaTime);

        if (Vector3.Distance(currentPackage.transform.position, targetPosition) < closeDistance)
        {
            currentPackage.transform.position = targetPosition;
            onArrived?.Invoke();
        }
    }

    /// <summary>
    /// Chamado pelo DeliveryController quando um pacote é coletado.
    /// </summary>
    public void ReceivePackage(GameObject package)
    {
        if (package == null) return;

        // Determina qual slot usar (ou se algum existe)
        activePackageSlot = packageSlotLeft != null ? packageSlotLeft : packageSlotRight;

        // --- LÓGICA PRINCIPAL DA CORREÇÃO ---
        // Se nenhum slot de pacote foi configurado para este personagem...
        if (activePackageSlot == null)
        {
            // ...simplesmente destrói o pacote visual imediatamente.
            Destroy(package);
            return;
        }

        // Se um slot existe, inicia a animação para mover o pacote até ele.
        currentPackage = package;
        currentPackage.transform.SetParent(null); // Garante que o pacote não é filho de nada
        isMovingToSlot = true;
        isMovingToMailbox = false;
    }

    /// <summary>
    /// Chamado pelo DeliveryController para iniciar a entrega ou descarte do pacote.
    /// </summary>
    public void DeliverPackage(Transform targetObject)
    {
        // Se targetObject for null, significa que o tempo acabou e o pacote deve ser descartado.
        if (targetObject == null)
        {
            if (currentPackage != null)
            {
                Destroy(currentPackage);
                currentPackage = null;
            }
            return;
        }

        // Se há um pacote e um alvo, inicia a animação de entrega.
        if (currentPackage != null)
        {
            deliveryTarget = targetObject;
            currentPackage.transform.SetParent(null);
            isMovingToMailbox = true;
            isMovingToSlot = false;
        }
    }
}