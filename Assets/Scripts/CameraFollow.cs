using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 
    
    [Header("Second Person Settings")]
    public Vector3 observerOffset = new Vector3(5, 3, 0); 
    public float smoothSpeed = 0.125f;
    public float rotationSpeed = 2f;
    
    [Header("Second Person Behavior")]
    public bool alwaysLookAtTarget = true;
    public float followDistance = 8f; 
    public float heightOffset = 3f;  
    
    [Header("Dynamic Observer")]
    public bool dynamicObserver = true; 
    public float orbitSpeed = 30f;     
    public bool autoOrbit = false;    
    
    private float currentAngle = 0f;
    
    void LateUpdate()
    {
        if (target == null) return;

        Vector3 observerPosition = CalculateObserverPosition();
        
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, observerPosition, smoothSpeed);
        transform.position = smoothedPosition;

        if (alwaysLookAtTarget)
        {
            Vector3 direction = target.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    Vector3 CalculateObserverPosition()
    {
        if (dynamicObserver)
        {
            if (autoOrbit)
            {
                currentAngle += orbitSpeed * Time.deltaTime;
            }
            else
            {
                if (Input.GetKey(KeyCode.Q))
                    currentAngle -= orbitSpeed * Time.deltaTime;
                if (Input.GetKey(KeyCode.E))
                    currentAngle += orbitSpeed * Time.deltaTime;
            }
            
            float radians = currentAngle * Mathf.Deg2Rad;
            Vector3 orbitPosition = new Vector3(
                Mathf.Sin(radians) * followDistance,
                heightOffset,
                Mathf.Cos(radians) * followDistance
            );
            
            return target.position + orbitPosition;
        }
        else
        {
            return target.position + observerOffset;
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            autoOrbit = !autoOrbit;
            Debug.Log($"Auto-órbita: {(autoOrbit ? "Activada" : "Desactivada")}");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentAngle = 0f;
            Debug.Log("Ángulo de observador reseteado");
        }
    }
}