using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InstructionalTextController : MonoBehaviour
{
    public static InstructionalTextController Instance { get; private set; }

    [Header("Configurações")]
    [Tooltip("O objeto de texto para exibir as instruções.")]
    public TextMeshProUGUI instructionalText;

    [Header("Timing")]
    [Tooltip("Delay inicial antes da primeira instrução.")]
    public float initialDelay = 2f;
    [Tooltip("Delay entre mensagens.")]
    public float delayBetweenMessages = 2f;
    [Tooltip("Duração da animação de entrada (escala).")]
    public float scaleInDuration = 0.3f;
    [Tooltip("Duração da animação de saída (escala).")]
    public float scaleOutDuration = 0.2f;
    [Tooltip("Tempo mínimo que uma instrução fica na tela após a ação ser concluída.")]
    public float minDisplayTimeAfterAction = 1f;
    [Tooltip("Tempo máximo que uma instrução fica na tela se não houver ação.")]
    public float defaultDisplayTime = 5f;
    [Tooltip("Tempo que mensagens de elogio ficam na tela.")]
    public float praiseDisplayTime = 2.5f;

    [Header("Cores para Destaque")]
    public Color keywordColorMovement = Color.yellow;
    public Color keywordColorMailbox = new Color(0.3f, 0.6f, 1f);
    public Color keywordColorGear = Color.gray;
    public Color keywordColorMap = Color.green;
    public Color keywordColorPraise = new Color(1f, 0.8f, 0f);
    public Color keywordColorSpeed = new Color(0f, 1f, 1f);

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Estado das instruções (só mostra uma vez)
    private bool hasShownGears = false;
    private bool hasShownNextArea = false;
    private bool hasShownAllIslands = false;

    // Fase atual da sequência principal
    private int currentPhase = -1; // -1 = não iniciado, 0=movimento, 1=coleta, 2=entrega, 3=fim

    // Flags de ação do jogador (resetadas a cada fase)
    private bool actionCompleted = false;
    private bool arrivedAtNewArea = false;

    // Contadores
    private int deliveriesCompleted = 0;

    // Mensagens de elogio para TrickScore
    private readonly string[] trickScoreMessages = new string[]
    {
        "Nice move!",
        "Awesome trick!",
        "Great skills!",
        "Impressive!",
        "You're on fire!",
        "Fantastic!",
        "Nailed it!",
        "Perfect!"
    };

    // Mensagens de elogio para Gol
    private readonly string[] goalMessages = new string[]
    {
        "GOAL!",
        "What a shot!",
        "Amazing goal!",
        "Incredible!",
        "Golazo!",
        "Top corner!",
        "Unstoppable!"
    };

    // Mensagens para Speed Boost (snacks)
    private readonly string[] speedBoostMessages = new string[]
    {
        "Tasty snack! Speed up!",
        "Yummy! Faster now!",
        "Delicious boost!",
        "Snack attack! Zoom!",
        "Sugar rush!",
        "Nom nom! Speedy!",
        "Treat time! Go fast!",
        "Energy boost!"
    };

    // Controle de exibição
    private bool isBusy = false;
    private bool isMainSequenceRunning = false;
    private Coroutine mainSequenceCoroutine = null;

    // Fila de mensagens pendentes
    private Queue<System.Action> pendingMessages = new Queue<System.Action>();

    void Awake()
    {
        // Sempre substitui a instância anterior para garantir que temos a correta
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        if (instructionalText == null)
        {
            instructionalText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (instructionalText != null)
        {
            instructionalText.transform.localScale = Vector3.zero;
            instructionalText.gameObject.SetActive(true);
        }

        LogDebug("InstructionalTextController Awake - Instance set");
    }

    void Start()
    {
        ResetAllState();
        mainSequenceCoroutine = StartCoroutine(MainInstructionSequence());
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        // Processa fila de mensagens quando não está ocupado e a sequência principal terminou
        if (!isBusy && !isMainSequenceRunning && pendingMessages.Count > 0)
        {
            var action = pendingMessages.Dequeue();
            action?.Invoke();
        }
    }

    // ========== RESET ==========

    /// <summary>
    /// Reseta todo o estado do controlador para começar do zero.
    /// </summary>
    public void ResetAllState()
    {
        LogDebug("Resetting all state");

        // Para qualquer coroutine em andamento
        if (mainSequenceCoroutine != null)
        {
            StopCoroutine(mainSequenceCoroutine);
            mainSequenceCoroutine = null;
        }
        StopAllCoroutines();

        // Reseta flags
        hasShownGears = false;
        hasShownNextArea = false;
        hasShownAllIslands = false;
        currentPhase = -1;
        actionCompleted = false;
        arrivedAtNewArea = false;
        deliveriesCompleted = 0;
        isBusy = false;
        isMainSequenceRunning = false;

        // Limpa fila
        pendingMessages.Clear();

        // Esconde texto imediatamente
        if (instructionalText != null)
        {
            instructionalText.transform.localScale = Vector3.zero;
        }
    }

    // ========== SEQUÊNCIA PRINCIPAL ==========

    public IEnumerator MainInstructionSequence()
    {
        isMainSequenceRunning = true;
        LogDebug("Main sequence starting");

        yield return new WaitForSeconds(initialDelay);

        // ===== FASE 0: MOVIMENTO =====
        currentPhase = 0;
        actionCompleted = false;
        LogDebug($"Phase 0: Showing movement instruction (actionCompleted={actionCompleted})");

        string moveText = GetMovementInstructionText();
        yield return ShowAndWaitForAction(moveText, 60f);

        LogDebug("Phase 0: Complete, waiting before next");
        yield return new WaitForSeconds(delayBetweenMessages);

        // ===== FASE 1: COLETA =====
        currentPhase = 1;
        actionCompleted = false;
        LogDebug($"Phase 1: Showing pickup instruction (actionCompleted={actionCompleted})");

        string pickupText = FormatText("Approach a {Mailbox} to pick up a package", "Mailbox", keywordColorMailbox);
        yield return ShowAndWaitForAction(pickupText, 60f);

        LogDebug("Phase 1: Complete, waiting before next");
        yield return new WaitForSeconds(delayBetweenMessages);

        // ===== FASE 2: ENTREGA =====
        currentPhase = 2;
        actionCompleted = false;
        LogDebug($"Phase 2: Showing delivery instruction (actionCompleted={actionCompleted})");

        string deliveryText = FormatText("Check the {iPaw} to see the delivery location on the minimap", "iPaw", keywordColorMap);
        yield return ShowAndWaitForAction(deliveryText, 120f);

        LogDebug("Phase 2: Complete");

        // ===== FIM =====
        currentPhase = 3;
        isMainSequenceRunning = false;
        mainSequenceCoroutine = null;
        LogDebug("Main sequence finished");
    }

    private IEnumerator ShowAndWaitForAction(string text, float timeout)
    {
        isBusy = true;

        // Mostra o texto
        yield return AnimateIn(text);

        LogDebug($"Waiting for action (phase={currentPhase}, timeout={timeout}s)");

        // Espera a ação ser completada ou timeout
        float elapsed = 0f;
        while (!actionCompleted && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        LogDebug($"Wait ended: actionCompleted={actionCompleted}, elapsed={elapsed:F1}s");

        // Delay após a ação antes de esconder
        yield return new WaitForSeconds(minDisplayTimeAfterAction);

        // Esconde o texto
        yield return AnimateOut();

        isBusy = false;
    }

    // ========== ANIMAÇÕES ==========

    private IEnumerator AnimateIn(string text)
    {
        instructionalText.text = text;
        instructionalText.transform.localScale = Vector3.zero;

        float timer = 0f;
        while (timer < scaleInDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / scaleInDuration);
            float eased = EaseOutCubic(progress);
            instructionalText.transform.localScale = Vector3.one * eased;
            yield return null;
        }
        instructionalText.transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateOut()
    {
        float timer = 0f;
        while (timer < scaleOutDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / scaleOutDuration);
            float eased = EaseOutCubic(progress);
            instructionalText.transform.localScale = Vector3.one * (1f - eased);
            yield return null;
        }
        instructionalText.transform.localScale = Vector3.zero;
    }

    private IEnumerator ShowMessageForDuration(string text, float duration)
    {
        isBusy = true;

        yield return AnimateIn(text);
        yield return new WaitForSeconds(duration);
        yield return AnimateOut();

        isBusy = false;
    }

    private IEnumerator ShowMessageUntilCondition(string text, System.Func<bool> condition, float timeout)
    {
        isBusy = true;

        yield return AnimateIn(text);

        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(minDisplayTimeAfterAction);
        yield return AnimateOut();

        isBusy = false;
    }

    private float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    // ========== FORMATAÇÃO ==========

    private string GetMovementInstructionText()
    {
        bool isMobile = GameManager.instance != null && GameManager.instance.isMobile;
        if (isMobile)
        {
            return FormatText("Use the {Joystick} to move", "Joystick", keywordColorMovement);
        }
        return FormatText("Use {WASD} to move", "WASD", keywordColorMovement);
    }

    private string FormatText(string text, string keyword, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        return text.Replace("{" + keyword + "}", $"<color=#{colorHex}>{keyword}</color>");
    }

    private string GetRandomMessage(string[] messages)
    {
        return messages[Random.Range(0, messages.Length)];
    }

    // ========== MÉTODOS PÚBLICOS - NOTIFICAÇÕES ==========

    public void NotifyPlayerMoved()
    {
        LogDebug($"NotifyPlayerMoved called - currentPhase={currentPhase}, actionCompleted={actionCompleted}");

        if (currentPhase == 0 && !actionCompleted)
        {
            actionCompleted = true;
            LogDebug("Phase 0 action COMPLETED!");
        }
    }

    public void NotifyPackagePickedUp()
    {
        LogDebug($"NotifyPackagePickedUp called - currentPhase={currentPhase}, actionCompleted={actionCompleted}");

        if (currentPhase == 1 && !actionCompleted)
        {
            actionCompleted = true;
            LogDebug("Phase 1 action COMPLETED!");
        }
    }

    public void NotifyDeliveryCompleted()
    {
        LogDebug($"NotifyDeliveryCompleted called - currentPhase={currentPhase}, actionCompleted={actionCompleted}");
        deliveriesCompleted++;

        if (currentPhase == 2 && !actionCompleted)
        {
            actionCompleted = true;
            LogDebug("Phase 2 action COMPLETED!");
        }

        // Após 3 entregas, mostra instrução das engrenagens
        if (deliveriesCompleted >= 3 && !hasShownGears && !isMainSequenceRunning)
        {
            hasShownGears = true;
            QueueMessage(() =>
            {
                string text = FormatText("Collect all {Gears} in this area to unlock the next one", "Gears", keywordColorGear);
                StartCoroutine(ShowMessageForDuration(text, defaultDisplayTime));
            });
        }
    }

    public void NotifyGearCollected()
    {
        LogDebug("Gear collected!");

        if (!hasShownGears && !isMainSequenceRunning)
        {
            hasShownGears = true;
            QueueMessage(() =>
            {
                string text = FormatText("Collect all {Gears} in this area to unlock the next one", "Gears", keywordColorGear);
                StartCoroutine(ShowMessageForDuration(text, defaultDisplayTime));
            });
        }
    }

    public void NotifyAreaUnlocked()
    {
        LogDebug("Area unlocked!");
        if (!hasShownNextArea)
        {
            hasShownNextArea = true;
            arrivedAtNewArea = false;

            QueueMessage(() =>
            {
                string text = FormatText("Follow the {Minimap} to reach the next area", "Minimap", keywordColorMap);
                StartCoroutine(ShowMessageUntilCondition(text, () => arrivedAtNewArea, 300f));
            });
        }
    }

    public void NotifyArrivedAtNewArea()
    {
        LogDebug("Arrived at new area!");
        arrivedAtNewArea = true;
        hasShownNextArea = false;

        if (!hasShownAllIslands)
        {
            hasShownAllIslands = true;
            QueueMessage(() =>
            {
                string text = FormatText("Collect all {Gears} from the 3 islands to unlock the entire map!", "Gears", keywordColorGear);
                StartCoroutine(ShowMessageForDuration(text, defaultDisplayTime));
            });
        }
    }

    public void NotifyTrickScoreCompleted()
    {
        LogDebug("Trick score!");

        if (!isMainSequenceRunning)
        {
            string message = GetRandomMessage(trickScoreMessages);
            QueuePraiseMessage(message);
        }
    }

    public void NotifyGoalScored()
    {
        LogDebug("Goal scored!");

        if (!isMainSequenceRunning)
        {
            string message = GetRandomMessage(goalMessages);
            QueuePraiseMessage(message);
        }
    }

    public void NotifySpeedBoostCollected()
    {
        LogDebug("Speed boost collected!");

        if (!isMainSequenceRunning)
        {
            string message = GetRandomMessage(speedBoostMessages);
            QueuePraiseMessage(message, keywordColorSpeed);
        }
    }

    // ========== SISTEMA DE FILA ==========

    private void QueueMessage(System.Action showAction)
    {
        if (isBusy || isMainSequenceRunning)
        {
            pendingMessages.Enqueue(showAction);
        }
        else
        {
            showAction();
        }
    }

    private void QueuePraiseMessage(string message)
    {
        QueuePraiseMessage(message, keywordColorPraise);
    }

    private void QueuePraiseMessage(string message, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string formattedMessage = $"<color=#{colorHex}>{message}</color>";

        if (!isBusy && !isMainSequenceRunning)
        {
            StartCoroutine(ShowMessageForDuration(formattedMessage, praiseDisplayTime));
        }
        else
        {
            pendingMessages.Enqueue(() =>
            {
                StartCoroutine(ShowMessageForDuration(formattedMessage, praiseDisplayTime));
            });
        }
    }

    public bool IsTextVisible()
    {
        return instructionalText != null && instructionalText.transform.localScale.x > 0.01f;
    }

    // ========== DEBUG ==========

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[Instructions] {message}");
        }
    }

    /// <summary>
    /// Retorna informações de debug sobre o estado atual.
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Phase: {currentPhase}, ActionCompleted: {actionCompleted}, IsBusy: {isBusy}, MainRunning: {isMainSequenceRunning}, Queue: {pendingMessages.Count}";
    }
}