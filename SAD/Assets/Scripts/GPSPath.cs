using UnityEngine;
using UnityEngine.AI;

public class GPSPath : MonoBehaviour
{
    public Transform objective;
    public LineRenderer lineRenderer;
    public DeliveryController deliveryController;

    private void Start()
    {
        deliveryController = GameObject.Find("Mailboxes").GetComponent<DeliveryController>();
    }

    void Update()
    {
        if (deliveryController.isDelivering)
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(transform.position, objective.position, NavMesh.AllAreas, path))
            {
                DrawPath(path);
            }
        }
    }

    void DrawPath (NavMeshPath path)
    {
        lineRenderer.positionCount = path.corners.Length;
        lineRenderer.SetPositions(path.corners);
    }
}
