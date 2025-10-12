using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrafficStopZone : MonoBehaviour
{
    public enum StopFilter { Any, BusOnly, CarOnly }

    public StopFilter filter = StopFilter.BusOnly;
    public float stopDuration = 2f;
    public float extraBusTime = 2f;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        var v = other.GetComponentInParent<TrafficVehicleSpline>();
        if (v == null) return;

        if (filter == StopFilter.BusOnly && v.type != TrafficVehicleSpline.VehicleType.Bus) return;
        if (filter == StopFilter.CarOnly && v.type != TrafficVehicleSpline.VehicleType.Car) return;

        float t = stopDuration + (v.type == TrafficVehicleSpline.VehicleType.Bus ? extraBusTime : 0f);
        v.RequestStop(t);
    }
}