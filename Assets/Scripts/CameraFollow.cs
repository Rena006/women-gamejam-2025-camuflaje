using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Jugador
    public Vector3 offset;   // Distancia desde el jugador
    public float smoothSpeed = 0.125f; // Velocidad de seguimiento

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        transform.LookAt(target); // opcional: que mire siempre al jugador
    }
}
