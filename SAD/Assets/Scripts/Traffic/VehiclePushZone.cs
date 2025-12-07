using UnityEngine;

/// <summary>
/// Zona de empurrão ao redor do veículo. Empurra o player para longe quando ele entra na área.
/// Deve ser colocado em um GameObject filho do veículo com um Collider marcado como Trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VehiclePushZone : MonoBehaviour
{
    [Header("Configuração do Empurrão")]
    [Tooltip("Força horizontal do empurrão.")]
    public float pushForce = 10f;
    [Tooltip("Força vertical do empurrão (para cima).")]
    public float pushUpForce = 5f;
    [Tooltip("Cooldown entre empurrões (segundos).")]
    public float pushCooldown = 0.3f;
    [Tooltip("Multiplicador baseado na velocidade do veículo (0 = ignora velocidade).")]
    public float speedMultiplier = 0.5f;

    [Header("Referências")]
    [Tooltip("Referência ao veículo pai. Se vazio, procura no parent.")]
    public TrafficVehicleSpline vehicle;

    private float lastPushTime = -10f;
    private Collider triggerCollider;

    void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (vehicle == null)
        {
            vehicle = GetComponentInParent<TrafficVehicleSpline>();
        }

        if (vehicle == null)
        {
            Debug.LogWarning($"[VehiclePushZone] {name}: Nenhum TrafficVehicleSpline encontrado no parent!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        TryPushPlayer(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryPushPlayer(other);
    }

    private void TryPushPlayer(Collider other)
    {
        // Verifica se é o player
        if (!other.CompareTag("Player")) return;

        // Verifica cooldown
        if (Time.time - lastPushTime < pushCooldown) return;

        // Pega o PlayerMovementThirdPerson
        PlayerMovementThirdPerson playerMovement = other.GetComponent<PlayerMovementThirdPerson>();
        if (playerMovement == null)
            playerMovement = other.GetComponentInParent<PlayerMovementThirdPerson>();

        if (playerMovement == null) return;

        // Calcula a direção do empurrão (do centro do veículo para o player)
        Vector3 vehicleCenter = vehicle != null ? vehicle.transform.position : transform.position;
        Vector3 playerPos = other.transform.position;
        
        Vector3 pushDirection = (playerPos - vehicleCenter);
        pushDirection.y = 0;
        
        // Se a direção for muito pequena, usa a direção oposta ao forward do veículo
        if (pushDirection.sqrMagnitude < 0.1f)
        {
            pushDirection = vehicle != null ? -vehicle.transform.forward : -transform.forward;
            pushDirection.y = 0;
        }
        pushDirection.Normalize();

        // Calcula multiplicador de velocidade
        float speedFactor = 1f;
        if (vehicle != null && speedMultiplier > 0)
        {
            float vehicleSpeed = vehicle.GetCurrentSpeed();
            float maxSpeed = vehicle.maxSpeed;
            speedFactor = 1f + (vehicleSpeed / Mathf.Max(maxSpeed, 1f)) * speedMultiplier;
        }

        // Monta o vetor de empurrão
        Vector3 pushVector = pushDirection * pushForce * speedFactor;
        pushVector.y = pushUpForce;

        // Aplica o empurrão
        playerMovement.ApplyExternalForce(pushVector);
        lastPushTime = Time.time;

        // Debug visual (opcional)
        Debug.DrawRay(playerPos, pushVector, Color.red, 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        // Desenha a área do trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Laranja transparente
            
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(capsule.center + Vector3.up * (capsule.height / 2 - capsule.radius), capsule.radius);
                Gizmos.DrawWireSphere(capsule.center - Vector3.up * (capsule.height / 2 - capsule.radius), capsule.radius);
            }
        }
    }
}