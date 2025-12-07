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
    public float initialDelay = 2f;
    public float delayBetweenMessages = 2f;
    public float scaleInDuration = 0.3f;
    public float scaleOutDuration = 0.2f;
    public float minDisplayTimeAfterAction = 1f;
    public float defaultDisplayTime = 5f;
    public float praiseDisplayTime = 2.5f;

    [Header("Aviso de Tempo")]
    [Tooltip("Duração do aviso de tempo acabando.")]
    public float timeWarningDuration = 3f;
    [Tooltip("Velocidade do pulso do aviso.")]
    public float timeWarningPulseSpeed = 4f;
    [Tooltip("Escala mínima do pulso.")]
    public float timeWarningMinScale = 0.9f;
    [Tooltip("Escala máxima do pulso.")]
    public float timeWarningMaxScale = 1.1f;

    [Header("Mapa Completo")]
    [Tooltip("Duração da mensagem de mapa completo.")]
    public float fullMapDuration = 4f;
    [Tooltip("Velocidade do pulso da mensagem de mapa completo.")]
    public float fullMapPulseSpeed = 3f;

    [Header("Cores para Destaque")]
    public Color keywordColorMovement = Color.yellow;
    public Color keywordColorMailbox = new Color(0.3f, 0.6f, 1f);
    public Color keywordColorGear = Color.gray;
    public Color keywordColorMap = Color.green;
    public Color keywordColorPraise = new Color(1f, 0.8f, 0f);
    public Color keywordColorSpeed = new Color(0f, 1f, 1f);
    public Color keywordColorWarning = new Color(1f, 0.3f, 0.3f);
    public Color keywordColorSuccess = new Color(0.3f, 1f, 0.3f);
    public Color keywordColorFail = new Color(1f, 0.5f, 0.2f); // Laranja para falha

    [Header("Debug")]
    public bool enableDebugLogs = true;

    private bool hasShownGears = false;
    private bool hasShownNextArea = false;
    private bool hasShownAllIslands = false;
    private bool hasShownFullMap = false;
    private int currentPhase = -1;
    private bool playerHasMoved = false;
    private bool packagePickedUp = false;
    private bool deliveryCompleted = false;
    private bool arrivedAtNewArea = false;
    private int deliveriesCompleted = 0;

    private readonly string[] trickScoreMessages = new string[]
    {
        "Nice move!", "Awesome trick!", "Great skills!", "Impressive!",
        "You're on fire!", "Fantastic!", "Nailed it!", "Perfect!"
    };

    private readonly string[] goalMessages = new string[]
    {
        "GOAL!", "What a shot!", "Amazing goal!", "Incredible!",
        "Golazo!", "Top corner!", "Unstoppable!"
    };

    private readonly string[] speedBoostMessages = new string[]
    {
        "Tasty snack! Speed up!", "Yummy! Faster now!", "Delicious boost!",
        "Snack attack! Zoom!", "Sugar rush!", "Nom nom! Speedy!",
        "Treat time! Go fast!", "Energy boost!"
    };

    private readonly string[] timeWarningMessages = new string[]
    {
        "Hurry up! Time is running out!",
        "Only 20 seconds left!",
        "Quick! The clock is ticking!",
        "Rush! Almost out of time!",
        "Faster! Time's almost up!",
        "Hurry! Final countdown!",
        "Speed up! 20 seconds remaining!"
    };

    private readonly string[] fullMapMessages = new string[]
    {
        "AMAZING! Full map unlocked!",
        "INCREDIBLE! All islands open!",
        "FANTASTIC! The whole city is yours!",
        "AWESOME! Complete map unlocked!",
        "LEGENDARY! All areas available!",
        "PERFECT! You unlocked everything!",
        "SUPERB! Full access granted!"
    };

    // Mensagens de sucesso no minigame - GATO pegou o RATO
    private readonly string[] catCatchSuccessMessages = new string[]
    {
        "Gotcha! That mouse is yours!",
        "Purrfect catch! Speedy kitty!",
        "Meow-velous! The mouse didn't stand a chance!"
    };

    // Mensagens de falha no minigame - RATO escapou do GATO
    private readonly string[] catCatchFailMessages = new string[]
    {
        "The mouse got away! Maybe next time...",
        "Too slow! That mouse was quick!",
        "Escaped! That sneaky little mouse!"
    };

    // Mensagens de sucesso no minigame - CACHORRO pegou a GALINHA
    private readonly string[] dogCatchSuccessMessages = new string[]
    {
        "Got it! Good boy catches the chicken!",
        "Woof! That chicken is yours!",
        "Pawsome catch! Fast doggy!"
    };

    // Mensagens de falha no minigame - GALINHA escapou do CACHORRO
    private readonly string[] dogCatchFailMessages = new string[]
    {
        "The chicken flew away! Better luck next time!",
        "Too slow! That chicken was fast!",
        "Escaped! Cluck cluck, bye bye!"
    };

    private bool isBusy = false;
    private bool isMainSequenceRunning = false;
    private Coroutine mainSequenceCoroutine = null;
    private Queue<System.Action> pendingMessages = new Queue<System.Action>();

    void Awake()
    {
        if (instructionalText == null)
        {
            instructionalText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (instructionalText != null)
        {
            instructionalText.transform.localScale = Vector3.zero;
            instructionalText.gameObject.SetActive(true);
        }

        LogDebug("Awake");
    }

    void OnEnable()
    {
        Instance = this;
        LogDebug("OnEnable - Instance set to this");
    }

    void OnDisable()
    {
        LogDebug("OnDisable called");
        StopAllCoroutines();
        isMainSequenceRunning = false;
        isBusy = false;
        mainSequenceCoroutine = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            LogDebug("OnDestroy - Instance cleared");
        }
    }

    void Update()
    {
        if (!isBusy && !isMainSequenceRunning && pendingMessages.Count > 0)
        {
            var action = pendingMessages.Dequeue();
            action?.Invoke();
        }
    }

    public void ResetAllState()
    {
        LogDebug("Resetting all state");

        StopAllCoroutines();
        mainSequenceCoroutine = null;

        hasShownGears = false;
        hasShownNextArea = false;
        hasShownAllIslands = false;
        hasShownFullMap = false;
        currentPhase = -1;
        playerHasMoved = false;
        packagePickedUp = false;
        deliveryCompleted = false;
        arrivedAtNewArea = false;
        deliveriesCompleted = 0;
        isBusy = false;
        isMainSequenceRunning = false;

        pendingMessages.Clear();

        if (instructionalText != null)
        {
            instructionalText.transform.localScale = Vector3.zero;
        }
    }

    public IEnumerator MainInstructionSequence()
    {
        if (isMainSequenceRunning)
        {
            LogDebug("Main sequence already running, aborting");
            yield break;
        }

        isMainSequenceRunning = true;
        LogDebug("Main sequence STARTED");

        yield return new WaitForSeconds(initialDelay);

        // FASE 0: MOVIMENTO
        currentPhase = 0;
        LogDebug($"Phase 0: Movement (playerHasMoved={playerHasMoved})");
        yield return ShowAndWaitForCondition(GetMovementInstructionText(), () => playerHasMoved, 60f);

        LogDebug("Phase 0 COMPLETE");
        yield return new WaitForSeconds(delayBetweenMessages);

        // FASE 1: COLETA
        currentPhase = 1;
        LogDebug($"Phase 1: Pickup (packagePickedUp={packagePickedUp})");
        yield return ShowAndWaitForCondition(
            FormatText("Approach a {Mailbox} to pick up a package", "Mailbox", keywordColorMailbox),
            () => packagePickedUp, 60f);

        LogDebug("Phase 1 COMPLETE");
        yield return new WaitForSeconds(delayBetweenMessages);

        // FASE 2: ENTREGA
        currentPhase = 2;
        LogDebug($"Phase 2: Delivery (deliveryCompleted={deliveryCompleted})");
        yield return ShowAndWaitForCondition(
            FormatText("Check the {iPaw} to see the delivery location on the minimap", "iPaw", keywordColorMap),
            () => deliveryCompleted, 120f);

        LogDebug("Phase 2 COMPLETE");

        currentPhase = 3;
        isMainSequenceRunning = false;
        mainSequenceCoroutine = null;
        LogDebug("Main sequence FINISHED");
    }

    private IEnumerator ShowAndWaitForCondition(string text, System.Func<bool> condition, float timeout)
    {
        isBusy = true;
        yield return AnimateIn(text);

        float elapsed = 0f;
        while (!condition() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        LogDebug($"Condition result: {condition()}, elapsed: {elapsed:F1}s");

        yield return new WaitForSeconds(minDisplayTimeAfterAction);
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
            instructionalText.transform.localScale = Vector3.one * EaseOutCubic(Mathf.Clamp01(timer / scaleInDuration));
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
            instructionalText.transform.localScale = Vector3.one * (1f - EaseOutCubic(Mathf.Clamp01(timer / scaleOutDuration)));
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

    private IEnumerator ShowPulsingMessage(string text, float duration, float pulseSpeed, float minScale, float maxScale)
    {
        isBusy = true;

        yield return AnimateIn(text);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            instructionalText.transform.localScale = Vector3.one * pulse;
            yield return null;
        }

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

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

    // ========== FORMATAÇÃO ==========

    private string GetMovementInstructionText()
    {
        bool isMobile = GameManager.instance != null && GameManager.instance.isMobile;
        return isMobile
            ? FormatText("Use the {Joystick} to move", "Joystick", keywordColorMovement)
            : FormatText("Use {WASD} to move", "WASD", keywordColorMovement);
    }

    private string FormatText(string text, string keyword, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        return text.Replace("{" + keyword + "}", $"<color=#{colorHex}>{keyword}</color>");
    }

    private string GetRandomMessage(string[] messages) => messages[Random.Range(0, messages.Length)];

    // ========== NOTIFICAÇÕES ==========

    public void NotifyPlayerMoved()
    {
        if (!playerHasMoved)
        {
            playerHasMoved = true;
            LogDebug("Player movement REGISTERED!");
        }
    }

    public void NotifyPackagePickedUp()
    {
        if (!packagePickedUp)
        {
            packagePickedUp = true;
            deliveryCompleted = false;
            LogDebug("Package pickup REGISTERED!");
        }
    }

    public void NotifyDeliveryCompleted()
    {
        deliveriesCompleted++;
        if (!deliveryCompleted)
        {
            deliveryCompleted = true;
            packagePickedUp = false;
            LogDebug("Delivery REGISTERED!");
        }

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
                string text = FormatText("Follow the {iPaw} to reach the next area", "iPaw", keywordColorMap);
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

    public void NotifyFullMapUnlocked()
    {
        LogDebug("FULL MAP UNLOCKED!");

        if (hasShownFullMap) return;
        hasShownFullMap = true;

        if (isBusy)
        {
            StopAllCoroutines();
            isBusy = false;
            isMainSequenceRunning = false;
        }

        string message = GetRandomMessage(fullMapMessages);
        string colorHex = ColorUtility.ToHtmlStringRGB(keywordColorSuccess);
        string formattedMessage = $"<color=#{colorHex}>{message}</color>";

        StartCoroutine(ShowPulsingMessage(
            formattedMessage,
            fullMapDuration,
            fullMapPulseSpeed,
            timeWarningMinScale,
            timeWarningMaxScale
        ));
    }

    /// <summary>
    /// Notifica que o jogador pegou o alvo no minigame com sucesso.
    /// </summary>
    /// <param name="isCat">True se é gato (pegou rato), False se é cachorro (pegou galinha)</param>
    public void NotifyMinigameCatchSuccess(bool isCat)
    {
        LogDebug($"Minigame SUCCESS! isCat={isCat}");

        string message = isCat
            ? GetRandomMessage(catCatchSuccessMessages)
            : GetRandomMessage(dogCatchSuccessMessages);

        string colorHex = ColorUtility.ToHtmlStringRGB(keywordColorSuccess);
        string formattedMessage = $"<color=#{colorHex}>{message}</color>";

        QueueMessage(() => StartCoroutine(ShowMessageForDuration(formattedMessage, praiseDisplayTime)));
    }

    /// <summary>
    /// Notifica que o alvo escapou no minigame (tempo acabou ou alvo fugiu).
    /// </summary>
    /// <param name="isCat">True se é gato (rato escapou), False se é cachorro (galinha escapou)</param>
    public void NotifyMinigameTargetEscaped(bool isCat)
    {
        LogDebug($"Minigame FAILED! isCat={isCat}");

        string message = isCat
            ? GetRandomMessage(catCatchFailMessages)
            : GetRandomMessage(dogCatchFailMessages);

        string colorHex = ColorUtility.ToHtmlStringRGB(keywordColorFail);
        string formattedMessage = $"<color=#{colorHex}>{message}</color>";

        QueueMessage(() => StartCoroutine(ShowMessageForDuration(formattedMessage, praiseDisplayTime)));
    }

    public void NotifyTrickScoreCompleted()
    {
        if (!isMainSequenceRunning)
            QueuePraiseMessage(GetRandomMessage(trickScoreMessages));
    }

    public void NotifyGoalScored()
    {
        if (!isMainSequenceRunning)
            QueuePraiseMessage(GetRandomMessage(goalMessages));
    }

    public void NotifySpeedBoostCollected()
    {
        if (!isMainSequenceRunning)
            QueuePraiseMessage(GetRandomMessage(speedBoostMessages), keywordColorSpeed);
    }

    public void NotifyTimeRunningOut()
    {
        LogDebug("Time running out!");

        if (isBusy)
        {
            StopAllCoroutines();
            isBusy = false;
            isMainSequenceRunning = false;
        }

        string message = GetRandomMessage(timeWarningMessages);
        string colorHex = ColorUtility.ToHtmlStringRGB(keywordColorWarning);
        string formattedMessage = $"<color=#{colorHex}>{message}</color>";

        StartCoroutine(ShowPulsingMessage(
            formattedMessage,
            timeWarningDuration,
            timeWarningPulseSpeed,
            timeWarningMinScale,
            timeWarningMaxScale
        ));
    }

    // ========== FILA ==========

    private void QueueMessage(System.Action showAction)
    {
        if (isBusy || isMainSequenceRunning)
            pendingMessages.Enqueue(showAction);
        else
            showAction();
    }

    private void QueuePraiseMessage(string message) => QueuePraiseMessage(message, keywordColorPraise);

    private void QueuePraiseMessage(string message, Color color)
    {
        string formatted = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>";

        if (!isBusy && !isMainSequenceRunning)
            StartCoroutine(ShowMessageForDuration(formatted, praiseDisplayTime));
        else
            pendingMessages.Enqueue(() => StartCoroutine(ShowMessageForDuration(formatted, praiseDisplayTime)));
    }

    public bool IsTextVisible() => instructionalText != null && instructionalText.transform.localScale.x > 0.01f;

    private void LogDebug(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[Instructions] {message}");
    }
}