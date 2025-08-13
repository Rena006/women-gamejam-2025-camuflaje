using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Game Objects")]
    public PlayerController player;
    public EnemyNPC enemy;
    
    [Header("UI Panels")]
    public Canvas mainUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject hudPanel;
    
    [Header("UI Text Elements")]
    public Text statusText;
    public Text timerText;
    public Text escapeProgressText;
    public Text gameOverText;
    public Text victoryText;
    
    [Header("UI Buttons")]
    public Button restartButton;                 // Game Over: solo este
    public Button victoryRestartButton;
    public Button victoryMainMenuButton;         // Victory sí mantiene Main Menu
    
    [Header("Game Settings")]
    public bool showTimer = true;
    public bool showEscapeProgress = true;
    public float gameTime = 0f;
    
    [Header("Audio")]
    public AudioSource gameAudioSource;
    public AudioClip gameOverSound;
    public AudioClip victorySound;
    public AudioClip backgroundMusic;
    
    [Header("Scene Names")]
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";
    
    private bool gameStarted = false;
    private bool gameOver = false;
    private bool victoryAchieved = false;
    private float startTime;
    
    void Start()
    {
        InitializeGame();
        SetupUI();
        SetupAudio();
        StartGame();
    }
    
    void InitializeGame()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (enemy == null)  enemy  = FindFirstObjectByType<EnemyNPC>();
        if (mainUI == null) CreateUI();
        startTime = Time.time;
    }
    
    void CreateUI()
    {
        GameObject canvasObj = new GameObject("GameCanvas");
        mainUI = canvasObj.AddComponent<Canvas>();
        mainUI.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        CreateHUDPanel();
        CreateGameOverPanel();
        CreateVictoryPanel();
    }
    
    void CreateHUDPanel()
    {
        // Panel HUD que ocupa toda la pantalla
        GameObject hudObj = new GameObject("HUD Panel");
        hudObj.transform.SetParent(mainUI.transform, false);
        var hudRect = hudObj.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;
        hudPanel = hudObj;

        // ===== STATUS (arriba-izquierda) =====
        GameObject statusObj = new GameObject("Status Text");
        statusObj.transform.SetParent(hudPanel.transform, false);
        statusText = statusObj.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 16;
        statusText.color = Color.white;
        statusText.text = "Status: Normal";
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.raycastTarget = false;

        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot    = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(10f, -10f);
        statusRect.sizeDelta = new Vector2(520f, 60f);

        // ===== TIMER (arriba-derecha) =====
        GameObject timerObj = new GameObject("Timer Text");
        timerObj.transform.SetParent(hudPanel.transform, false);
        timerText = timerObj.AddComponent<Text>();
        timerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timerText.fontSize = 18;
        timerText.color = Color.yellow;
        timerText.text = "Time: 00:00";
        timerText.alignment = TextAnchor.UpperRight;
        timerText.raycastTarget = false;

        RectTransform timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(1f, 1f);
        timerRect.anchorMax = new Vector2(1f, 1f);
        timerRect.pivot    = new Vector2(1f, 1f);
        timerRect.anchoredPosition = new Vector2(-10f, -10f);
        timerRect.sizeDelta = new Vector2(260f, 40f);

        // ===== ESCAPE PROGRESS (abajo-izquierda) =====
        GameObject escapeObj = new GameObject("Escape Progress Text");
        escapeObj.transform.SetParent(hudPanel.transform, false);
        escapeProgressText = escapeObj.AddComponent<Text>();
        escapeProgressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        escapeProgressText.fontSize = 16;
        escapeProgressText.color = Color.green;
        escapeProgressText.text = "";
        escapeProgressText.alignment = TextAnchor.LowerLeft;
        escapeProgressText.raycastTarget = false;

        RectTransform escapeRect = escapeProgressText.GetComponent<RectTransform>();
        escapeRect.anchorMin = new Vector2(0f, 0f);
        escapeRect.anchorMax = new Vector2(0f, 0f);
        escapeRect.pivot    = new Vector2(0f, 0f);
        escapeRect.anchoredPosition = new Vector2(10f, 10f);
        escapeRect.sizeDelta = new Vector2(620f, 40f);
    }
    
    void CreateGameOverPanel()
    {
        GameObject p = new GameObject("Game Over Panel");
        p.transform.SetParent(mainUI.transform, false);
        gameOverPanel = p;

        Image bg = p.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        var r = p.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        
        // Título
        GameObject tObj = new GameObject("Game Over Text");
        tObj.transform.SetParent(p.transform, false);
        gameOverText = tObj.AddComponent<Text>();
        gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gameOverText.fontSize = 48;
        gameOverText.color = Color.red;
        gameOverText.text = "GAME OVER";
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.raycastTarget = false;

        var tr = gameOverText.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.65f);
        tr.sizeDelta = new Vector2(600, 100);

        // ÚNICO BOTÓN: RESTART
        GameObject b1 = new GameObject("Restart Button");
        b1.transform.SetParent(p.transform, false);
        restartButton = b1.AddComponent<Button>();
        Image bi1 = b1.AddComponent<Image>();
        bi1.color = Color.gray;

        var br1 = restartButton.GetComponent<RectTransform>();
        br1.anchorMin = br1.anchorMax = new Vector2(0.5f, 0.42f);
        br1.sizeDelta = new Vector2(240, 56);

        var bt1Obj = new GameObject("Restart Text");
        bt1Obj.transform.SetParent(b1.transform, false);
        var bt1 = bt1Obj.AddComponent<Text>();
        bt1.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bt1.fontSize = 22;
        bt1.color = Color.white;
        bt1.text = "RESTART";
        bt1.alignment = TextAnchor.MiddleCenter;
        bt1.raycastTarget = false;

        var btr1 = bt1.GetComponent<RectTransform>();
        btr1.anchorMin = Vector2.zero;
        btr1.anchorMax = Vector2.one;
        btr1.offsetMin = Vector2.zero;
        btr1.offsetMax = Vector2.zero;

        gameOverPanel.SetActive(false);
    }
    
    void CreateVictoryPanel()
    {
        GameObject p = new GameObject("Victory Panel");
        p.transform.SetParent(mainUI.transform, false);
        victoryPanel = p;

        Image bg = p.AddComponent<Image>();
        bg.color = new Color(0, 0.5f, 0, 0.8f);

        var r = p.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        
        GameObject tObj = new GameObject("Victory Text");
        tObj.transform.SetParent(p.transform, false);
        victoryText = tObj.AddComponent<Text>();
        victoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        victoryText.fontSize = 48;
        victoryText.color = Color.yellow;
        victoryText.text = "VICTORY!";
        victoryText.alignment = TextAnchor.MiddleCenter;
        victoryText.raycastTarget = false;

        var tr = victoryText.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.7f);
        tr.sizeDelta = new Vector2(600, 100);
        
        // Play Again
        GameObject b1 = new GameObject("Victory Restart Button");
        b1.transform.SetParent(p.transform, false);
        victoryRestartButton = b1.AddComponent<Button>();
        Image bi1 = b1.AddComponent<Image>(); 
        bi1.color = Color.green;

        var br1 = victoryRestartButton.GetComponent<RectTransform>();
        br1.anchorMin = br1.anchorMax = new Vector2(0.5f, 0.42f);
        br1.sizeDelta = new Vector2(220, 54);

        var bt1 = new GameObject("Victory Restart Text").AddComponent<Text>();
        bt1.transform.SetParent(b1.transform, false);
        bt1.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); 
        bt1.fontSize = 20; 
        bt1.color = Color.white; 
        bt1.text = "PLAY AGAIN"; 
        bt1.alignment = TextAnchor.MiddleCenter;
        bt1.raycastTarget = false;

        var btr1 = bt1.GetComponent<RectTransform>();
        btr1.anchorMin = Vector2.zero; 
        btr1.anchorMax = Vector2.one; 
        btr1.offsetMin = Vector2.zero; 
        btr1.offsetMax = Vector2.zero;
        
        // Main Menu (solo en Victory)
        GameObject b2 = new GameObject("Victory Main Menu Button");
        b2.transform.SetParent(p.transform, false);
        victoryMainMenuButton = b2.AddComponent<Button>();
        Image bi2 = b2.AddComponent<Image>(); 
        bi2.color = Color.green;

        var br2 = victoryMainMenuButton.GetComponent<RectTransform>();
        br2.anchorMin = br2.anchorMax = new Vector2(0.5f, 0.3f);
        br2.sizeDelta = new Vector2(220, 54);

        var bt2 = new GameObject("Victory Main Menu Text").AddComponent<Text>();
        bt2.transform.SetParent(b2.transform, false);
        bt2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); 
        bt2.fontSize = 18; 
        bt2.color = Color.white; 
        bt2.text = "MAIN MENU"; 
        bt2.alignment = TextAnchor.MiddleCenter;
        bt2.raycastTarget = false;

        var btr2 = bt2.GetComponent<RectTransform>(); 
        btr2.anchorMin = Vector2.zero; 
        btr2.anchorMax = Vector2.one; 
        btr2.offsetMin = Vector2.zero; 
        btr2.offsetMax = Vector2.zero;

        victoryPanel.SetActive(false);
    }
    
    void SetupUI()
    {
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (victoryRestartButton != null) victoryRestartButton.onClick.AddListener(RestartGame);
        if (victoryMainMenuButton != null) victoryMainMenuButton.onClick.AddListener(GoToMainMenu);
        
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
    }
    
    void SetupAudio()
    {
        if (gameAudioSource == null) gameAudioSource = gameObject.AddComponent<AudioSource>();
        if (backgroundMusic != null)
        {
            gameAudioSource.clip = backgroundMusic;
            gameAudioSource.loop = true;
            gameAudioSource.volume = 0.3f;
            gameAudioSource.Play();
        }
    }
    
    void StartGame()
    {
        gameStarted = true; gameOver = false; victoryAchieved = false; startTime = Time.time;
    }
    
    void Update()
    {
        if (!gameStarted || gameOver || victoryAchieved) return;
        gameTime = Time.time - startTime;
        UpdateUITexts();
        CheckGameConditions();
    }

    void UpdateUITexts()
    {
        if (timerText != null && showTimer)
        {
            int m = Mathf.FloorToInt(gameTime / 60f);
            int s = Mathf.FloorToInt(gameTime % 60f);
            timerText.text = $"Time: {m:00}:{s:00}";
        }
        UpdateStatusText();
        UpdateEscapeProgress();
    }
    
    void UpdateStatusText()
    {
        if (statusText == null) return;
        string status = "READY";
        if (player != null && enemy != null)
        {
            if (player.IsHiddenForNPC()) status = "HIDDEN";
            else if (enemy.IsPlayerDetected()) status = "DETECTED!";
            else status = "NORMAL";
            status += player.IsPlayerTransformed() ? " | CUBE" : " | SPHERE";
            status += $" | Dist: {enemy.GetDistanceToPlayer():F1}m";
        }
        statusText.text = status;
    }
    
    void UpdateEscapeProgress()
    {
        if (escapeProgressText == null || !showEscapeProgress) return;

        // SAFE ZONE primero
        SafeZone safeZone = FindFirstObjectByType<SafeZone>();
        if (safeZone != null && safeZone.IsPlayerInZone())
        {
            float p = safeZone.GetWinProgress();   // 0..1
            float t = safeZone.GetTimeInZone();
            escapeProgressText.text = $"🛡️ SAFE ZONE: {(p*100f):F0}% ({t:F1}/5.0s)";
            escapeProgressText.color = Color.cyan;

            if (p >= 1f && !IsVictoryAchieved()) TriggerVictory();
            return;
        }

        // Escape por distancia
        if (enemy == null) return;
        float dist = enemy.GetDistanceToPlayer();
        float escapeDist = 12f;

        if (dist >= escapeDist)
        {
            float escapeTime = 10f;
            float cur = enemy.GetEscapeTimer();
            float pct = (cur / escapeTime) * 100f;
            escapeProgressText.text = pct >= 100f
                ? "🎉 ESCAPING! Victory incoming!"
                : $"🏃 ESCAPING: {pct:F0}% ({cur:F1}/{escapeTime:F0}s)";
            escapeProgressText.color = pct >= 100f ? Color.yellow : Color.green;
        }
        else
        {
            float pct = (dist / escapeDist) * 100f;
            escapeProgressText.text = $"Distance to escape: {pct:F0}% ({dist:F1}/{escapeDist:F0}m)";
            escapeProgressText.color = Color.white;
        }
    }
    
    void CheckGameConditions()
    {
        if (enemy == null) return;
        if (enemy.GetCurrentState() == EnemyNPC.EnemyState.Attacking && !gameOver)
            TriggerGameOver();
    }
    
    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        if (gameOverSound != null && gameAudioSource != null)
            gameAudioSource.PlayOneShot(gameOverSound);
        StartCoroutine(ShowGameOverPanel());
    }
    
    public void TriggerVictory()
    {
        if (victoryAchieved) return;
        victoryAchieved = true;
        if (victorySound != null && gameAudioSource != null)
            gameAudioSource.PlayOneShot(victorySound);
        StartCoroutine(ShowVictoryPanel());
    }
    
    IEnumerator ShowGameOverPanel()
    {
        yield return new WaitForSeconds(1f);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    IEnumerator ShowVictoryPanel()
    {
        yield return new WaitForSeconds(1f);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void GoToMainMenu()
    {
        Debug.Log("[GM] Main Menu button pressed");
        Time.timeScale = 1f;

#if UNITY_EDITOR
        try
        {
            SceneManager.LoadScene(mainMenuSceneName);
            Debug.Log($"[GM] Loading scene '{mainMenuSceneName}'");
        }
        catch
        {
            Debug.LogWarning($"[GM] Scene '{mainMenuSceneName}' no encontrada. ¿Está en Build Settings?");
            RestartGame();
        }
#else
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            Debug.Log($"[GM] Loading scene '{mainMenuSceneName}'");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"[GM] Scene '{mainMenuSceneName}' no encontrada en build. Reiniciando escena actual.");
            RestartGame();
        }
#endif
    }
    
    public bool IsGameOver() => gameOver;
    public bool IsVictoryAchieved() => victoryAchieved;
    public float GetGameTime() => gameTime;
}
