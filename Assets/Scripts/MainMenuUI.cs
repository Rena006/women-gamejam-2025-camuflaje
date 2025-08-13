using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI roots")]
    [Tooltip("Canvas/raíz del menú. Si lo dejas vacío, usa este mismo objeto.")]
    public GameObject menuRoot;
    [Tooltip("HUD del juego (opcional). Lo activamos al darle Play.")]
    public GameObject hudRoot;

    [Header("Safe Zone")]
    [Tooltip("Opción A: crear zona en runtime.")]
    public SafeZoneCreator safeZoneCreator;
    [Tooltip("Opción B: usar un objeto SafeZone ya existente en la escena.")]
    public GameObject existingSafeZone;

    void Awake()
    {
        if (menuRoot == null) menuRoot = gameObject;

        // Menú visible al iniciar y juego pausado
        menuRoot.SetActive(true);
        if (hudRoot != null) hudRoot.SetActive(false);
        Time.timeScale = 0f;
    }

    public void PlayGame()
    {
        Debug.Log("[MENU] PlayGame pressed");

        // Crear o activar la zona segura
        if (existingSafeZone != null)
        {
            existingSafeZone.SetActive(true);
        }
        else if (safeZoneCreator != null)
        {
            safeZoneCreator.CreateSafeZone(); // solo crea si no existe
        }

        // Ocultar menú y reanudar
        if (hudRoot != null) hudRoot.SetActive(true);
        menuRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Debug.Log("[MENU] Quit pressed");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Útil si desde Game Over quieres “volver al menú” sin cambiar de escena
    public void ShowMenu()
    {
        if (hudRoot != null) hudRoot.SetActive(false);
        menuRoot.SetActive(true);
        Time.timeScale = 0f;
    }
}
