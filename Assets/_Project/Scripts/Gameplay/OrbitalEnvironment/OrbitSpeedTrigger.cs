using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OrbitSpeedTrigger : MonoBehaviour
{
    public OrbitManager manager;
    public float enterMultiplier = 1.8f;
    public float exitMultiplier = 1.0f;
    public float blendSeconds = 1.0f;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (manager) manager.LerpSpeedMultiplierTo(enterMultiplier, blendSeconds);
    }

    void OnTriggerExit(Collider other)
    {
        if (manager) manager.LerpSpeedMultiplierTo(exitMultiplier, blendSeconds);
    }
}
