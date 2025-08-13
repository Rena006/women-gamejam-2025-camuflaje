using UnityEngine;

[System.Serializable]
public class SafeZoneCreator : MonoBehaviour
{
    [Header("Safe Zone Creation")]
    public bool createOnStart = true;
    public Vector3 safeZonePosition = new Vector3(10, -0.5f, 10); // altura de tu mundo
    public float safeZoneRadius = 4f;
    public Color safeZoneColor = Color.green;
    
    [Header("Auto Positioning")]
    public bool autoPosition = true;
    public float minDistanceFromPlayer = 8f;
    public float minDistanceFromEnemy = 6f;
    
    void Start()
    {
        if (createOnStart) CreateSafeZone();
    }
    
    [ContextMenu("Create Safe Zone")]
    public void CreateSafeZone()
    {
        SafeZone existing = FindFirstObjectByType<SafeZone>();
        if (existing != null)
        {
            Debug.LogWarning("⚠️ Safe Zone already exists!");
            return;
        }
        
        Vector3 finalPos = autoPosition ? FindOptimalPosition() : safeZonePosition;
        finalPos.y = -0.5f; // coincide con la altura del jugador
        
        GameObject go = new GameObject("SafeZone");
        go.transform.position = finalPos;
        
        SafeZone zone = go.AddComponent<SafeZone>();
        zone.radius = safeZoneRadius;
        zone.safeZoneColor = safeZoneColor;
        
        var audio = go.AddComponent<AudioSource>();
        audio.playOnAwake = false; audio.spatialBlend = 1f;
        
        Debug.Log($"✅ Safe Zone created at {finalPos}");
    }
    
    Vector3 FindOptimalPosition()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        EnemyNPC enemy = FindFirstObjectByType<EnemyNPC>();
        
        Vector3 pp = player ? player.transform.position : Vector3.zero;
        Vector3 ep = enemy ? enemy.transform.position : Vector3.zero;
        
        for (int i = 0; i < 20; i++)
        {
            Vector3 c = new Vector3(Random.Range(-15f, 15f), -0.5f, Random.Range(-15f, 15f));
            if (Vector3.Distance(c, pp) >= minDistanceFromPlayer && Vector3.Distance(c, ep) >= minDistanceFromEnemy)
                return c;
        }
        return safeZonePosition;
    }
    
    [ContextMenu("Remove Safe Zone")]
    public void RemoveSafeZone()
    {
        SafeZone z = FindFirstObjectByType<SafeZone>();
        if (z != null) DestroyImmediate(z.gameObject);
        else Debug.LogWarning("⚠️ No Safe Zone found to remove");
    }
}
