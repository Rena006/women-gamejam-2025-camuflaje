using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0, 8, -10);
    public float smoothSpeed = 3f;

    [Header("Camera Rotation")]
    public bool lookAtTarget = true;
    public float rotationSpeed = 2f;

    [Header("Optional: Dynamic Camera")]
    public bool enableDynamicCamera = false;
    public float followDistance = 8f;
    public float heightOffset = 3f;
    public float orbitSpeed = 30f;

    private float currentAngle = 0f;

    void Start()
    {
        // Auto-find Player
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");

            if (player == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) player = pc.gameObject;
            }

            if (player == null)
            {
                var all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in all)
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
                Debug.LogError("❌ CÁMARA: No se encontró el Player.");
            }
        }

        if (target != null)
        {
            transform.position = target.position + offset;
            if (lookAtTarget) transform.LookAt(target);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = enableDynamicCamera ? CalculateDynamicPosition() : target.position + offset;
        transform.position = Vector3.Slerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (lookAtTarget)
        {
            Vector3 dir = target.position - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                var q = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, q, rotationSpeed * Time.deltaTime);
            }
        }
    }

    Vector3 CalculateDynamicPosition()
    {
        if (Input.GetKey(KeyCode.Q)) currentAngle -= orbitSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) currentAngle += orbitSpeed * Time.deltaTime;

        float rad = currentAngle * Mathf.Deg2Rad;
        Vector3 orbit = new Vector3(Mathf.Sin(rad) * followDistance, heightOffset, Mathf.Cos(rad) * followDistance);
        return target.position + orbit;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            enableDynamicCamera = !enableDynamicCamera;
            if (!enableDynamicCamera) currentAngle = 0f;
        }
        if (Input.GetKeyDown(KeyCode.V)) currentAngle = 0f;

        // Ajustes rápidos del offset
        if (Input.GetKey(KeyCode.O)) offset.y += 2f * Time.deltaTime;
        if (Input.GetKey(KeyCode.L)) offset.y -= 2f * Time.deltaTime;
        if (Input.GetKey(KeyCode.K)) offset.z += 2f * Time.deltaTime;
        if (Input.GetKey(KeyCode.M)) offset.z -= 2f * Time.deltaTime;
    }

    public void SetOffset(Vector3 newOffset) => offset = newOffset;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        Debug.Log($"Nuevo target asignado: {newTarget.name}");
    }
}
