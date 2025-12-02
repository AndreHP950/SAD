using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    [Header("Configurações do Gol")]
    [Tooltip("Pontos a serem adicionados quando a bola entra.")]
    public int scoreValue = 100;

    [Tooltip("Nome do objeto que deve ser detectado como a bola.")]
    public string ballObjectName = "Ball";

    [Header("Efeitos")]
    [Tooltip("Prefab do VFX a ser instanciado no local do gol.")]
    public GameObject goalVFX;

    [Tooltip("Nome do SFX a ser tocado.")]
    public string goalSFX = "Kazoo";

    private ScoreController scoreController;

    void Start()
    {
        // Garante que o collider é um trigger
        GetComponent<Collider>().isTrigger = true;

        // Encontra o ScoreController na cena
        scoreController = FindFirstObjectByType<ScoreController>();
        if (scoreController == null)
        {
            Debug.LogError("GoalTrigger: ScoreController não encontrado na cena!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verifica se o objeto que entrou no trigger tem o nome correto ("Ball")
        if (other.gameObject.name == ballObjectName)
        {
            

            // 1. Adiciona pontos ao placar
            if (scoreController != null)
            {
                scoreController.ChangeScore(scoreValue);
            }

            // 2. Toca o SFX
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(goalSFX);
            }

            // 3. Instancia o VFX
            if (goalVFX != null)
            {
                Instantiate(goalVFX, transform.position, Quaternion.identity);
            }

            
            
        }
    }
}