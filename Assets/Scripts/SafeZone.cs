using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [Header("Safe Zone Settings")]
    public float radius = 4f;
    public Color safeZoneColor = Color.green;
    public Color warningColor = Color.yellow;
    public float winTime = 5f;

    [Header("Visual Effects")]
    public bool showVisualIndicator = true;
    public Material safeZoneMaterial;
    public GameObject visualEffect;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip enterSound;
    public AudioClip winSound;

    // Private variables
    private bool playerInZone = false;
    private float timeInZone = 0f;
    private PlayerController player;
    private EnemyNPC enemy;
    private GameManager gameManager;
    private GameObject visualIndicator;
    private bool gameWon = false;

    void Start()
    {
        InitializeComponents();
        CreateVisualIndicator();
        Debug.Log($"🛡️ Safe Zone created at {transform.position} with radius {radius}");
    }

    void InitializeComponents()
    {
        player = FindFirstObjectByType<PlayerController>();
        enemy = FindFirstObjectByType<EnemyNPC>();
        gameManager = FindFirstObjectByType<GameManager>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
    }

    void CreateVisualIndicator()
    {
        if (!showVisualIndicator) return;

        visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        visualIndicator.name = "SafeZone_Visual";
        visualIndicator.transform.SetParent(transform);

        // pequeño offset en Y para evitar z-fighting con el suelo
        visualIndicator.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        visualIndicator.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);

        var r = visualIndicator.GetComponent<Renderer>();

        if (safeZoneMaterial != null)
        {
            r.material = safeZoneMaterial;
        }
        else
        {
            // Asegurar shader transparente
            Shader sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            r.material = new Material(sh);

            Color c = safeZoneColor;
            c.a = 0.35f;
            r.material.color = c;
        }

        // quitar collider (solo visual)
        var col = visualIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    void Update()
    {
        if (gameWon || player == null) return;

        CheckPlayerInZone();
        UpdateVisualFeedback();

        if (playerInZone) UpdateWinProgress();
    }

    void CheckPlayerInZone()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool wasInZone = playerInZone;
        playerInZone = distanceToPlayer <= radius;

        if (playerInZone && !wasInZone) OnPlayerEnterZone();
        else if (!playerInZone && wasInZone) OnPlayerExitZone();
    }

    void OnPlayerEnterZone()
    {
        timeInZone = 0f;
        Debug.Log("🛡️ Player entered SAFE ZONE! Stay for 5 seconds to win!");
        if (enterSound != null && audioSource != null) audioSource.PlayOneShot(enterSound);
    }

    void OnPlayerExitZone()
    {
        timeInZone = 0f;
        Debug.Log("❌ Player left safe zone - progress reset");
    }

    void UpdateWinProgress()
    {
        timeInZone += Time.deltaTime;

        float timeLeft = winTime - timeInZone;
        int s = Mathf.CeilToInt(timeLeft);
        int sPrev = Mathf.CeilToInt(timeLeft + Time.deltaTime);
        if (s != sPrev && s > 0) Debug.Log($"🛡️ Safe zone countdown: {s} seconds to victory!");

        if (timeInZone >= winTime) TriggerVictory();
    }

    void TriggerVictory()
    {
        if (gameWon) return;
        gameWon = true;

        Debug.Log("🎉 VICTORY! Player reached safe zone!");
        if (winSound != null && audioSource != null) audioSource.PlayOneShot(winSound);
        CreateVictoryEffect();

        if (gameManager != null) gameManager.TriggerVictory();
        if (enemy != null) enemy.enabled = false;
    }

    void UpdateVisualFeedback()
    {
        if (visualIndicator == null) return;

        var r = visualIndicator.GetComponent<Renderer>();
        if (r == null || r.material == null) return;

        if (playerInZone)
        {
            float progress = timeInZone / winTime;
            Color c = Color.Lerp(warningColor, safeZoneColor, progress);
            c.a = 0.5f + (progress * 0.3f);
            r.material.color = c;

            float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.2f;
            visualIndicator.transform.localScale = new Vector3(radius * 2f * pulse, 0.05f, radius * 2f * pulse);
        }
        else
        {
            Color c = safeZoneColor; c.a = 0.35f;
            r.material.color = c;
            visualIndicator.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        }
    }

    void CreateVictoryEffect()
    {
        for (int i = 0; i < 10; i++)
        {
            var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.transform.position = transform.position + Random.insideUnitSphere * radius;
            fx.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);
            var rr = fx.GetComponent<Renderer>();
            if (rr != null) rr.material.color = Color.green;
            var col = fx.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Destroy(fx, 3f);
        }
    }

    // API pública usada por GameManager
    public bool IsPlayerInZone() => playerInZone;
    public float GetTimeInZone() => timeInZone;
    public float GetWinProgress() => Mathf.Clamp01(timeInZone / Mathf.Max(0.01f, winTime));

    // Gizmos
    void OnDrawGizmos()
    {
        Gizmos.color = playerInZone ? safeZoneColor : warningColor;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = new Color(safeZoneColor.r, safeZoneColor.g, safeZoneColor.b, 0.1f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
