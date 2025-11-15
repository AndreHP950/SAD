using UnityEngine;


// Bobbing simples da câmera em FPV quando o player corre.
// Deve ser adicionado ao GameObject da Main Camera.
public class FPVCameraBobbing : MonoBehaviour
{
    public float amplitude = 0.06f;
    public float frequency = 10f;
    public float speedThreshold = 2f;

    PlayerMovementThirdPerson pm;
    float baseY;

    void Start()
    {
        baseY = transform.localPosition.y;
    }

    // Chamado pelo MinigameController: anexa/desanexa o player
    public void SetPlayerMovement(PlayerMovementThirdPerson p)
    {
        pm = p;
    }

    void LateUpdate()
    {
        if (pm == null) return;

        // API pública em PlayerMovement
        float speed = pm.GetHorizontalSpeed();

        if (speed > speedThreshold)
        {
            float y = baseY + Mathf.Sin(Time.time * frequency) * amplitude;
            var lp = transform.localPosition;
            lp.y = y;
            transform.localPosition = lp;
        }
        else
        {
            var lp = transform.localPosition;
            lp.y = Mathf.Lerp(lp.y, baseY, Time.deltaTime * 8f);
            transform.localPosition = lp;
        }
    }
}