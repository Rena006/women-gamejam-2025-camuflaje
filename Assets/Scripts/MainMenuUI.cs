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
    public SafeZoneCreator safeZoneCreator;   // si lo usas
    [Tooltip("Opción B: usar un objeto SafeZone ya existente en la escena.")]
    public GameObject existingSafeZone;

    void Awake()
    {
        if (menuRoot == null) menuRoot = gameObject;
        menuRoot.SetActive(true);
        if (hudRoot) hudRoot.SetActive(false);
        Time.timeScale = 0f;
    }

    public void PlayGame()
    {
        if (existingSafeZone) existingSafeZone.SetActive(true);
        else if (safeZoneCreator) safeZoneCreator.CreateSafeZone();

        if (hudRoot) hudRoot.SetActive(true);
        menuRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowMenu()
    {
        if (hudRoot) hudRoot.SetActive(false);
        menuRoot.SetActive(true);
        Time.timeScale = 0f;
    }
}
