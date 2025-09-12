using UnityEngine;

public class TurnNode : MonoBehaviour
{
    [Header("Configuração do TurnNode")]
    public Transform targetNode; // Próximo node
    public float cooldown = 1.0f;
    public float curveAngle = 90f; // Ângulo da curva (em graus, defina no Inspector)

    // Retorna a direção para o próximo node
    public Vector3 GetTargetDirection()
    {
        if (targetNode == null) return transform.forward;
        return (targetNode.position - transform.position).normalized;
    }
}
