using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IntersectionZone : MonoBehaviour
{
    [Tooltip("Camadas consideradas veículos (colisores dos veículos).")]
    public LayerMask vehicleMask = ~0;

    readonly Queue<TrafficVehicleSpline> queue = new Queue<TrafficVehicleSpline>();
    TrafficVehicleSpline current;

    // NOVO: Método público para verificar se um veículo é o primeiro da fila.
    public bool IsFirstInQueue(TrafficVehicleSpline vehicle)
    {
        if (queue.Count == 0 || vehicle == null)
            return false;

        // Retorna true se o veículo fornecido for o mesmo que está no início da fila.
        return queue.Peek() == vehicle;
    }

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsVehicle(other, out var v))
        {
            if (!queue.Contains(v))
            {
                queue.Enqueue(v);
                v.NotifyEnterIntersection(this);
            }
            GrantNext();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsVehicle(other, out var v))
        {
            v.NotifyExitIntersection(this);
            if (current == v)
            {
                current = null;
                GrantNext();
            }
        }
    }

    void GrantNext()
    {
        if (current != null) return;

        TrimQueue();
        if (queue.Count > 0)
        {
            current = queue.Dequeue();
            if (current != null)
            {
                current.GrantIntersection(this);
            }
            else
            {
                // Se o carro na fila foi destruído, tenta o próximo
                GrantNext();
            }
        }
    }

    void TrimQueue()
    {
        while (queue.Count > 0 && (queue.Peek() == null || !queue.Peek().gameObject.activeInHierarchy))
        {
            queue.Dequeue();
        }
    }

    bool IsVehicle(Collider other, out TrafficVehicleSpline v)
    {
        v = null;
        if ((vehicleMask.value & (1 << other.gameObject.layer)) == 0) return false;
        v = other.GetComponentInParent<TrafficVehicleSpline>();
        return v != null;
    }

    public bool HasPriority(TrafficVehicleSpline v)
    {
        return current == v;
    }
}