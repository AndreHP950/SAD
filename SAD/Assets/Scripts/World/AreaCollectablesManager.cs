using UnityEngine;

public class AreaCollectablesManager : MonoBehaviour
{
    [SerializeField] private int collectableArea;

    private void Start()
    {
        switch (transform.parent.name)
        {
            case "Area1":
                collectableArea = 1;
                break;
            case "Area2":
                collectableArea = 2;
                break;
            case "Area3":
                collectableArea = 3;
                break;
        }
    }

    public int GetArea()
    {
        return collectableArea;
    }
}
