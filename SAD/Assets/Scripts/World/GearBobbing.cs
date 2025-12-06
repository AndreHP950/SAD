using UnityEngine;

/// <summary>
/// Adiciona animação de flutuação (bobbing) e rotação às engrenagens.
/// A flutuação só vai para cima, para não entrar no chão.
/// </summary>
public class GearBobbing : MonoBehaviour
{
    [Header("Flutuação")]
    [Tooltip("Altura máxima da flutuação (só sobe, não desce).")]
    public float bobbingHeight = 0.3f;
    [Tooltip("Velocidade da flutuação.")]
    public float bobbingSpeed = 2f;

    [Header("Rotação")]
    [Tooltip("Velocidade da rotação no eixo Y (graus/segundo).")]
    public float rotationSpeed = 50f;

    // Posição inicial (a mais baixa)
    private Vector3 initialPosition;

    void Start()
    {
        // Guarda a posição inicial como o ponto mais baixo
        initialPosition = transform.position;
    }

    void Update()
    {
        // Flutuação: usa Mathf.Abs(Sin) para só ir para cima (0 a 1, nunca negativo)
        float offset = Mathf.Abs(Mathf.Sin(Time.time * bobbingSpeed)) * bobbingHeight;
        transform.position = initialPosition + Vector3.up * offset;

        // Rotação no eixo Y
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}