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

    [Header("Comportamento do Meu Personagem")]
    [Tooltip("Se verdadeiro, o pacote fica na mão apenas durante a animação de coleta (ou pelo tempo definido) e é destruído visualmente após.")]
    public bool destroyVisualAfterCollect = false;
    [Tooltip("Escala aplicada ao pacote quando chega na mão durante a coleta.")]
    public float collectPickupScale = 0.3f;
    [Tooltip("Tempo (s) para o pacote sumir após chegar na mão quando destroyVisualAfterCollect estiver ativo.")]
    public float collectVisualLifetime = 0.6f;

    // Estado interno
    private GameObject currentPackage;
    private Transform activePackageSlot;
    private Transform deliveryTarget;
    private bool isMovingToSlot = false;
    private bool isMovingToMailbox = false;

    // Referência ao controlador de animação
    private PlayerAnimationController animationController;

    void Awake()
    {
        animationController = GetComponentInChildren<PlayerAnimationController>();
    }

    void Update()
    {
        if (isMovingToSlot && currentPackage != null && activePackageSlot != null)
        {
            MovePackage(activePackageSlot.position, () =>
            {
                // Parent ao slot e ajusta escala visual
                currentPackage.transform.SetParent(activePackageSlot, true);
                currentPackage.transform.localScale = Vector3.one * collectPickupScale;

                isMovingToSlot = false;

                // Se for para sumir após coleta com base em tempo, agenda
                if (destroyVisualAfterCollect && animationController != null)
                {
                    StartCoroutine(WaitCollectLifetimeThenClear());
                }
            });
        }
        else if (isMovingToMailbox && currentPackage != null && deliveryTarget != null)
        {
            MovePackage(deliveryTarget.position, () =>
            {
                Destroy(currentPackage);
                currentPackage = null;
                isMovingToMailbox = false;
            });
        }
    }

    private void MovePackage(Vector3 targetPosition, System.Action onArrived)
    {
        if (currentPackage == null) return;

        currentPackage.transform.position = Vector3.Lerp(
            currentPackage.transform.position,
            targetPosition,
            packageSpeed * Time.deltaTime
        );

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

        // Decide o lado e slot
        activePackageSlot = null;
        bool isRightSide = true;

        Vector3 dirToPackage = package.transform.position - transform.position;
        dirToPackage.y = 0f;
        if (dirToPackage.sqrMagnitude < 0.0001f) dirToPackage = transform.forward;
        float sideDot = Vector3.Dot(transform.right, dirToPackage.normalized);

        if (packageSlotLeft != null && packageSlotRight != null)
        {
            if (sideDot >= 0f)
            {
                activePackageSlot = packageSlotRight;
                isRightSide = true;
            }
            else
            {
                activePackageSlot = packageSlotLeft;
                isRightSide = false;
            }
        }
        else if (packageSlotRight != null)
        {
            activePackageSlot = packageSlotRight;
            isRightSide = true;
        }
        else if (packageSlotLeft != null)
        {
            activePackageSlot = packageSlotLeft;
            isRightSide = false;
        }

        // Dispara animação direcional
        if (animationController != null)
        {
            animationController.TriggerCollectAnimation(isRightSide);
        }

        // Se nenhum slot foi configurado, destrói imediatamente
        if (activePackageSlot == null)
        {
            Destroy(package);
            return;
        }

        // Move pacote até o slot
        currentPackage = package;
        currentPackage.transform.SetParent(null);
        isMovingToSlot = true;
        isMovingToMailbox = false;
    }

    /// <summary>
    /// Chamado pelo DeliveryController para iniciar a entrega ou descarte do pacote.
    /// </summary>
    public void DeliverPackage(Transform targetObject)
    {
        // descarte
        if (targetObject == null)
        {
            if (currentPackage != null)
            {
                Destroy(currentPackage);
                currentPackage = null;
            }
            return;
        }

        // entrega
        if (currentPackage != null)
        {
            deliveryTarget = targetObject;
            currentPackage.transform.SetParent(null);
            isMovingToMailbox = true;
            isMovingToSlot = false;
        }
    }

    // Aguarda apenas um tempo fixo após coleta e limpa o visual (sem depender do fim da animação)
    private System.Collections.IEnumerator WaitCollectLifetimeThenClear()
    {
        // garante que o pacote está parentado/visível um frame
        yield return null;

        float t = collectVisualLifetime;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        if (currentPackage != null)
        {
            Destroy(currentPackage);
            currentPackage = null;
        }
    }
}