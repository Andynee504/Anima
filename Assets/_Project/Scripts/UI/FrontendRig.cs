using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform pivot;           // arraste FrontEndPivot
    public Camera cam;                // arraste FrontEndCamera
    public Vector3 offset = new Vector3(0, 2.5f, 10f);
    public float orbitDegPerSec = 8f; // velocidade da “rotação”
    public float bobAmp = 0.2f;       // opcional: sobe/desce leve
    public float bobFreq = 0.3f;

    float angle;

    void LateUpdate()
    {
        if (!pivot || !cam) return;
        angle += orbitDegPerSec * Time.deltaTime;

        var rot = Quaternion.Euler(0f, angle, 0f);
        var pos = pivot.position + rot * offset;
        pos.y += Mathf.Sin(Time.time * Mathf.PI * 2f * bobFreq) * bobAmp;

        cam.transform.position = pos;
        cam.transform.LookAt(pivot.position, Vector3.up);
    }
}
