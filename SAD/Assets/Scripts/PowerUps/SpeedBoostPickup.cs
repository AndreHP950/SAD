using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Config")]
    public float duration = 5f;
    public float multiplier = 1.3f; // +30%
    public bool destroyOnPickup = true;
    public GameObject pickupVFX; // opcional

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

        if (destroyOnPickup) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}