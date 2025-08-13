using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SafeZone : MonoBehaviour
{
    [Header("Safe Zone")]
    public float radius = 4f;
    public Color safeZoneColor = new Color(0f, 1f, 0f, 0.5f);
    public float timeToWin = 5f;

    [Header("Visual Options")]
    public float lift = 0.05f;            // eleva el visual para evitar z-fighting
    public float ringWidth = 0.06f;       // grosor del LineRenderer
    public int ringSegments = 64;         // calidad del círculo

    private SphereCollider triggerCol;
    private PlayerController player;
    private float timeInZone = 0f;
    private bool playerInside = false;

    private GameObject visualDisk;        // disco visible
    private LineRenderer ring;            // aro visible

    void Awake()
    {
        // Collider trigger del área
        triggerCol = GetComponent<SphereCollider>();
        triggerCol.isTrigger = true;
        triggerCol.radius = radius;

        // Busca player
        var pObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (pObj) player = pObj.GetComponent<PlayerController>();
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        CreateVisuals();
    }

    void CreateVisuals()
    {
        // ----- DISCO (cilindro muy bajo) -----
        visualDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualDisk.name = "SafeZone_Disk";
        visualDisk.transform.SetParent(transform, false);
        visualDisk.transform.localPosition = new Vector3(0f, lift, 0f);
        visualDisk.transform.localRotation = Quaternion.identity;
        visualDisk.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f); // diámetro = 2*radio
        Destroy(visualDisk.GetComponent<Collider>());

        var mr = visualDisk.GetComponent<Renderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.material = BuildTransparentUnlitMaterial(safeZoneColor);

        // ----- ARO (LineRenderer) -----
        var ringObj = new GameObject("SafeZone_Ring");
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = new Vector3(0f, lift * 1.5f, 0f);

        ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.widthMultiplier = ringWidth;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.material = BuildTransparentUnlitMaterial(new Color(safeZoneColor.r, safeZoneColor.g, safeZoneColor.b, 0.9f));
        ring.positionCount = ringSegments;

        Vector3[] pts = new Vector3[ringSegments];
        for (int i = 0; i < ringSegments; i++)
        {
            float t = (i / (float)ringSegments) * Mathf.PI * 2f;
            pts[i] = new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
        }
        ring.SetPositions(pts);
    }

    Material BuildTransparentUnlitMaterial(Color c)
    {
        // Intenta URP/Unlit -> Unlit/Color -> Standard (fallback)
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Standard");

        var mat = new Material(sh);

        // Color + transparencia (funciona con URP/Unlit y Unlit/Color; en Standard se verá opaco si no se cambia el modo)
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c); // URP
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", c);     // Unlit/Color o Standard

        // Asegura que se renderice después del suelo para evitar parpadeo
        mat.renderQueue = 3000; // Transparent
        return mat;
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(
            new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z),
            transform.position
        );

        bool inRange = dist <= radius + 0.05f;
        if (inRange) { playerInside = true; timeInZone += Time.deltaTime; }
        else { playerInside = false; timeInZone = 0f; }
    }

    void OnValidate()
    {
        if (triggerCol == null) triggerCol = GetComponent<SphereCollider>();
        if (triggerCol != null) triggerCol.radius = radius;

        if (visualDisk != null)
            visualDisk.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);

        if (ring != null)
        {
            ring.positionCount = ringSegments;
            Vector3[] pts = new Vector3[ringSegments];
            for (int i = 0; i < ringSegments; i++)
            {
                float t = (i / (float)ringSegments) * Mathf.PI * 2f;
                pts[i] = new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius);
            }
            ring.SetPositions(pts);
            ring.widthMultiplier = ringWidth;
        }
    }

    // API para GameManager
    public bool IsPlayerInZone() => playerInside;
    public float GetWinProgress() => Mathf.Clamp01(timeInZone / Mathf.Max(0.01f, timeToWin));
    public float GetTimeInZone() => timeInZone;

    void OnDrawGizmos()
    {
        Gizmos.color = safeZoneColor;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, transform.position.y, transform.position.z), radius);
    }
}
