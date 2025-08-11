using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; 
    
    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0, 8, -10); // Mejor ángulo por defecto
    public float smoothSpeed = 3f; // Menos agresivo para evitar tambaleo
    public float damping = 0.1f; // Amortiguación adicional
    
    [Header("Camera Rotation")]
    public bool lookAtTarget = true; 
    public float rotationSpeed = 2f; // Rotación más lenta
    
    [Header("Optional: Dynamic Camera")]
    public bool enableDynamicCamera = false;
    public float followDistance = 8f; 
    public float heightOffset = 3f;  
    public float orbitSpeed = 30f;
    
    private float currentAngle = 0f;
    private Vector3 velocity = Vector3.zero; // Para SmoothDamp
    
    void Start()
    {
        // Auto-encontrar el jugador AGRESIVAMENTE
        if (target == null)
        {
            // Método 1: Por tag
            GameObject player = GameObject.FindWithTag("Player");
            
            // Método 2: Por nombre
            if (player == null)
            {
                player = GameObject.Find("Player");
            }
            
            // Método 3: Buscar cualquier objeto con PlayerController
            if (player == null)
            {
                PlayerController playerController = FindObjectOfType<PlayerController>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                }
            }
            
            // Método 4: Buscar el primer objeto con esfera y renderer
            if (player == null)
            {
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.ToLower().Contains("player") || 
                        (obj.GetComponent<Renderer>() != null && 
                         obj.GetComponent<Rigidbody>() != null &&
                         obj.name.Contains("Sphere")))
                    {
                        player = obj;
                        break;
                    }
                }
            }
            
            if (player != null)
            {
                target = player.transform;
                Debug.Log($"✅ CÁMARA: Player encontrado automáticamente: {player.name}");
            }
            else
            {
                Debug.LogError("❌ CÁMARA: No se encontró el Player. Búsqueda exhaustiva falló.");
            }
        }
        else
        {
            Debug.Log($"✅ CÁMARA: Target ya asignado: {target.name}");
        }
        
        // Posición inicial INMEDIATA de la cámara
        if (target != null)
        {
            Vector3 initialPos = target.position + offset;
            transform.position = initialPos;
            Debug.Log($"✅ CÁMARA: Posición inicial establecida en {initialPos}");
            
            if (lookAtTarget)
            {
                transform.LookAt(target);
            }
        }
    }
    
    void LateUpdate()
    {
        if (target == null) 
        {
            return;
        }

        Vector3 desiredPosition;
        
        if (enableDynamicCamera)
        {
            desiredPosition = CalculateDynamicPosition();
        }
        else
        {
            desiredPosition = target.position + offset;
        }
        
        // MOVIMIENTO MUY SUAVE sin tambaleo
        float actualSmoothSpeed = smoothSpeed * Time.deltaTime;
        transform.position = Vector3.Slerp(transform.position, desiredPosition, actualSmoothSpeed);

        // Rotación muy suave hacia el target
        if (lookAtTarget)
        {
            Vector3 direction = target.position - transform.position;
            if (direction.sqrMagnitude > 0.001f) // Evitar divisiones por cero
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
    Vector3 CalculateDynamicPosition()
    {
        // Controles para orbitar la cámara
        if (Input.GetKey(KeyCode.Q))
        {
            currentAngle -= orbitSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
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
    
    void Update()
    {
        // Controles de cámara
        if (Input.GetKeyDown(KeyCode.C))
        {
            enableDynamicCamera = !enableDynamicCamera;
            Debug.Log($"Cámara dinámica: {(enableDynamicCamera ? "Activada" : "Desactivada")}");
            
            if (!enableDynamicCamera)
            {
                // Resetear a posición fija
                currentAngle = 0f;
            }
        }
        
        // Reset del ángulo de cámara
        if (Input.GetKeyDown(KeyCode.V))
        {
            currentAngle = 0f;
            Debug.Log("Ángulo de cámara reseteado");
        }
        
        // Ajustar offset dinámicamente con las teclas
        if (Input.GetKey(KeyCode.O))
        {
            offset.y += 2f * Time.deltaTime; // Subir cámara
        }
        if (Input.GetKey(KeyCode.L))
        {
            offset.y -= 2f * Time.deltaTime; // Bajar cámara
        }
        if (Input.GetKey(KeyCode.K))
        {
            offset.z += 2f * Time.deltaTime; // Acercar cámara
        }
        if (Input.GetKey(KeyCode.M))
        {
            offset.z -= 2f * Time.deltaTime; // Alejar cámara
        }
    }
    
    // Método para cambiar el offset dinámicamente
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    // Método para cambiar el objetivo
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        velocity = Vector3.zero; // Reset velocity for smooth transition
        Debug.Log($"Nuevo target asignado: {newTarget.name}");
    }
}