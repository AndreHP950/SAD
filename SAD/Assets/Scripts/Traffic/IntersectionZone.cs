using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IntersectionZone : MonoBehaviour
{
    [Tooltip("Camadas consideradas veículos (colisores dos veículos).")]
    public LayerMask vehicleMask = ~0;

    readonly Queue<TrafficVehicleSpline> queue = new Queue<TrafficVehicleSpline>();
    TrafficVehicleSpline current;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsVehicle(other, out var v)) return;

        v.NotifyEnterIntersection(this);
        if (current == null && queue.Count == 0)
        {
            current = v;
            v.GrantIntersection(this);
        }
        else
        {
            if (!queue.Contains(v))
                queue.Enqueue(v);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsVehicle(other, out var v)) return;

        v.NotifyExitIntersection(this);
        if (v == current)
        {
            current = null;
            GrantNext();
        }
        else
        {
            // limpa entradas obsoletas
            TrimQueue();
        }
    }

    void GrantNext()
    {
        while (queue.Count > 0 && current == null)
        {
            var next = queue.Dequeue();
            if (next != null && next.isActiveAndEnabled)
            {
                current = next;
                next.GrantIntersection(this);
            }
        }
    }

    void TrimQueue()
    {
        if (queue.Count == 0) return;
        var tmp = new Queue<TrafficVehicleSpline>();
        while (queue.Count > 0)
        {
            var v = queue.Dequeue();
            if (v != null) tmp.Enqueue(v);
        }
        while (tmp.Count > 0) queue.Enqueue(tmp.Dequeue());
    }

    bool IsVehicle(Collider other, out TrafficVehicleSpline v)
    {
        v = other.GetComponentInParent<TrafficVehicleSpline>();
        return v != null;
    }

    public bool HasPriority(TrafficVehicleSpline v) => v == current;
}