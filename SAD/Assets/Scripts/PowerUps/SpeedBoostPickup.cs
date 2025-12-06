using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Config")]
    public float duration = 5f;
    public float multiplier = 1.3f; // +30%
    public bool destroyOnPickup = true;
    public GameObject pickupVFX; // opcional

    [Header("Animation")]
    [Tooltip("Velocidade da flutuação vertical.")]
    public float bobbingSpeed = 1.5f;
    [Tooltip("Altura máxima da flutuação vertical.")]
    public float bobbingHeight = 0.25f;
    [Tooltip("Velocidade da rotação no eixo Y.")]
    public float rotationSpeed = 50f;

    // Estado interno para a animação
    private Vector3 initialPosition;

    void Start()
    {
        // Guarda a posição inicial para centralizar a animação de flutuação
        initialPosition = transform.position;
    }

    void Update()
    {
        // Aplica as animações de flutuação e rotação a cada frame
        AnimatePickup();
    }

    private void AnimatePickup()
    {
        // Animação de Flutuação (sobe e desce)
        float newY = initialPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Animação de Rotação
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"SpeedBoostPickup triggered by {other.name}");
        if (!other.CompareTag("Player")) return;

        // PlayerMovement geralmente está no mesmo GO do CharacterController
        var pm = other.GetComponent<PlayerMovementThirdPerson>() ?? other.GetComponentInParent<PlayerMovementThirdPerson>();
        if (pm == null) return;

        pm.ActivateSpeedBoost(duration, multiplier);

        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, transform.rotation);

        // SFX: Toca o som de power-up
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("PowerUP");
        }

        // Notifica o sistema de instruções
        if (InstructionalTextController.Instance != null)
        {
            InstructionalTextController.Instance.NotifySpeedBoostCollected();
        }

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}