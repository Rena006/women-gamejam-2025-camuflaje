using UnityEngine;

public class SafeZoneCreator : MonoBehaviour
{
    [Header("Spawn")]
    public Vector3 position = new Vector3(0f, -0.5f, 3f);
    public float radius = 4f;

    private SafeZone created;

    public SafeZone CreateSafeZone()
    {
        if (created != null) return created;

        GameObject go = new GameObject("SafeZone (Runtime)");
        go.transform.position = position;

        var sz = go.AddComponent<SafeZone>();
        sz.radius = radius;
        sz.showVisualIndicator = true;

        created = sz;
        return created;
    }
}
