using UnityEngine;

public class TurnNode : MonoBehaviour
{
    [Header("Configura��o do TurnNode")]
    public Transform targetNode; // Pr�ximo node
    public float cooldown = 1.0f;
    public float curveAngle = 90f; // �ngulo da curva (em graus, defina no Inspector)

    // Retorna a dire��o para o pr�ximo node
    public Vector3 GetTargetDirection()
    {
        if (targetNode == null) return transform.forward;
        return (targetNode.position - transform.position).normalized;
    }
}
