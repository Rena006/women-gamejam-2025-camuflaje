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
    public Button restartButton;
    public Button mainMenuButton;
    public Button victoryRestartButton;

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
        if (enemy == null) enemy = FindFirstObjectByType<EnemyNPC>();

        if (mainUI == null) CreateUI();
        startTime = Time.time;
    }

    // ---------- UI CREATION ----------
    void CreateUI()
    {
        // Canvas raíz
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

    // Helper para crear un Text
    Text CreateText(string name, Transform parent, string txt, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = color;
        t.text = txt;
        t.alignment = align;
        t.raycastTarget = false; // ¡no bloquear botones!
        return t;
    }

    void CreateHUDPanel()
    {
        // IMPORTANTE: crear con RectTransform y estirarlo a toda la pantalla
        hudPanel = new GameObject("HUD Panel", typeof(RectTransform));
        hudPanel.transform.SetParent(mainUI.transform, false);

        var hudRect = hudPanel.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.offsetMin = Vector2.zero;
        hudRect.offsetMax = Vector2.zero;

        // STATUS (arriba-izquierda)
        statusText = CreateText("Status Text", hudPanel.transform, "Status: READY", 16, Color.white, TextAnchor.UpperLeft);
        var r1 = statusText.rectTransform;
        r1.anchorMin = new Vector2(0f, 1f);
        r1.anchorMax = new Vector2(0f, 1f);
        r1.pivot = new Vector2(0f, 1f);
        r1.anchoredPosition = new Vector2(10f, -10f);
        r1.sizeDelta = new Vector2(520f, 40f);

        // TIMER (arriba‑derecha)
        timerText = CreateText("Timer Text", hudPanel.transform, "Time: 00:00", 22, Color.yellow, TextAnchor.UpperRight);
        var r2 = timerText.rectTransform;
        r2.anchorMin = new Vector2(1f, 1f);
        r2.anchorMax = new Vector2(1f, 1f);
        r2.pivot = new Vector2(1f, 1f);
        r2.anchoredPosition = new Vector2(-10f, -10f);
        r2.sizeDelta = new Vector2(300f, 40f);

        // ESCAPE PROGRESS (abajo‑izquierda)
        escapeProgressText = CreateText("Escape Progress Text", hudPanel.transform, "", 16, Color.green, TextAnchor.LowerLeft);
        var r3 = escapeProgressText.rectTransform;
        r3.anchorMin = new Vector2(0f, 0f);
        r3.anchorMax = new Vector2(0f, 0f);
        r3.pivot = new Vector2(0f, 0f);
        r3.anchoredPosition = new Vector2(10f, 10f);
        r3.sizeDelta = new Vector2(640f, 30f);
    }

    void CreateGameOverPanel()
    {
        var p = new GameObject("Game Over Panel", typeof(RectTransform));
        p.transform.SetParent(mainUI.transform, false);
        gameOverPanel = p;

        var bg = p.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.8f);

        var r = p.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        gameOverText = CreateText("Game Over Text", p.transform, "GAME OVER", 48, Color.red, TextAnchor.MiddleCenter);
        var tr = gameOverText.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.68f); tr.sizeDelta = new Vector2(600, 90);

        var restartObj = new GameObject("Restart Button", typeof(RectTransform));
        restartObj.transform.SetParent(p.transform, false);
        restartButton = restartObj.AddComponent<Button>();
        var ri = restartObj.AddComponent<Image>(); ri.color = Color.gray;
        var rr = restartObj.GetComponent<RectTransform>(); rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.42f); rr.sizeDelta = new Vector2(220, 54);
        var rt = CreateText("Restart Text", restartObj.transform, "RESTART", 20, Color.white, TextAnchor.MiddleCenter);
        var rtr = rt.rectTransform; rtr.anchorMin = Vector2.zero; rtr.anchorMax = Vector2.one; rtr.offsetMin = Vector2.zero; rtr.offsetMax = Vector2.zero;

        var menuObj = new GameObject("Main Menu Button", typeof(RectTransform));
        menuObj.transform.SetParent(p.transform, false);
        mainMenuButton = menuObj.AddComponent<Button>();
        var mi = menuObj.AddComponent<Image>(); mi.color = Color.gray;
        var mr = menuObj.GetComponent<RectTransform>(); mr.anchorMin = mr.anchorMax = new Vector2(0.5f, 0.30f); mr.sizeDelta = new Vector2(220, 50);
        var mt = CreateText("Main Menu Text", menuObj.transform, "MAIN MENU", 18, Color.white, TextAnchor.MiddleCenter);
        var mtr = mt.rectTransform; mtr.anchorMin = Vector2.zero; mtr.anchorMax = Vector2.one; mtr.offsetMin = Vector2.zero; mtr.offsetMax = Vector2.zero;

        gameOverPanel.SetActive(false);
    }

    void CreateVictoryPanel()
    {
        var p = new GameObject("Victory Panel", typeof(RectTransform));
        p.transform.SetParent(mainUI.transform, false);
        victoryPanel = p;

        var bg = p.AddComponent<Image>(); bg.color = new Color(0, 0.5f, 0, 0.8f);
        var r = p.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;

        victoryText = CreateText("Victory Text", p.transform, "VICTORY!", 48, Color.yellow, TextAnchor.MiddleCenter);
        var tr = victoryText.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.70f); tr.sizeDelta = new Vector2(600, 90);

        var againObj = new GameObject("Play Again Button", typeof(RectTransform));
        againObj.transform.SetParent(p.transform, false);
        victoryRestartButton = againObj.AddComponent<Button>();
        var ai = againObj.AddComponent<Image>(); ai.color = Color.green;
        var ar = againObj.GetComponent<RectTransform>(); ar.anchorMin = ar.anchorMax = new Vector2(0.5f, 0.42f); ar.sizeDelta = new Vector2(250, 60);
        var at = CreateText("Play Again Text", againObj.transform, "PLAY AGAIN", 24, Color.white, TextAnchor.MiddleCenter);
        var atr = at.rectTransform; atr.anchorMin = Vector2.zero; atr.anchorMax = Vector2.one; atr.offsetMin = Vector2.zero; atr.offsetMax = Vector2.zero;

        victoryPanel.SetActive(false);
    }

    void SetupUI()
    {
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        if (victoryRestartButton != null) victoryRestartButton.onClick.AddListener(RestartGame);

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

    // ---------- LOOP ----------
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
        UpdateMainTimer();
        UpdateStatusText();
        UpdateEscapeProgress();
    }

    void UpdateMainTimer()
    {
        if (enemy != null && !enemy.IsNPCActive())
        {
            int s = Mathf.CeilToInt(enemy.GetActivationCountdown());
            if (s > 0) { timerText.text = $"🤖 NPC Activating: {s}"; timerText.color = Color.white; timerText.fontSize = 24; return; }
        }

        if (enemy != null && enemy.IsInDetectionDelay() && enemy.GetDetectionSecondsLeft() > 0)
        {
            int s = enemy.GetDetectionSecondsLeft();
            timerText.text = $"⚠️ ESCAPE! {s}";
            timerText.color = Color.red;
            timerText.fontSize = 36;
            return;
        }

        SafeZone sz = FindFirstObjectByType<SafeZone>();
        if (sz != null && sz.IsPlayerInZone())
        {
            float left = Mathf.Max(0f, 5f - sz.GetTimeInZone());
            int s = Mathf.CeilToInt(left);
            if (s > 0) { timerText.text = $"🛡️ SAFE: {s}"; timerText.color = Color.cyan; timerText.fontSize = 30; return; }
        }

        if (showTimer)
        {
            int m = Mathf.FloorToInt(gameTime / 60f);
            int s = Mathf.FloorToInt(gameTime % 60f);
            timerText.text = $"Time: {m:00}:{s:00}";
            timerText.color = Color.yellow;
            timerText.fontSize = 20;
        }
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

        SafeZone sz = FindFirstObjectByType<SafeZone>();
        if (sz != null && sz.IsPlayerInZone())
        {
            float p = sz.GetWinProgress();
            float t = sz.GetTimeInZone();
            escapeProgressText.text = $"🛡️ SAFE ZONE: {(p * 100f):F0}% ({t:F1}/5.0s)";
            escapeProgressText.color = Color.cyan;
            if (p >= 1f && !IsVictoryAchieved()) TriggerVictory();
            return;
        }

        if (enemy == null) return;
        if (enemy.IsInDetectionDelay())
        {
            escapeProgressText.text = "⚠️ DETECTED! Run or hide!";
            escapeProgressText.color = Color.red;
            return;
        }

        if (player != null && player.IsHiddenForNPC())
        {
            escapeProgressText.text = "🫥 Hidden - Enemy can't see you";
            escapeProgressText.color = Color.green;
        }
        else if (enemy.IsPlayerDetected())
        {
            escapeProgressText.text = "👁️ Spotted - Find cover!";
            escapeProgressText.color = new Color(1f, 0.5f, 0f);
        }
        else
        {
            escapeProgressText.text = "🔍 Safe - Enemy patrolling";
            escapeProgressText.color = Color.white;
        }
    }

    void CheckGameConditions()
    {
        if (enemy == null) return;
        if (enemy.GetCurrentState() == EnemyNPC.EnemyState.Attacking && !gameOver)
            TriggerGameOver();
    }

    // ---------- STATES ----------
    public void TriggerGameOver()
    {
        if (gameOver) return;
        gameOver = true;
        if (gameOverSound != null && gameAudioSource != null) gameAudioSource.PlayOneShot(gameOverSound);
        StartCoroutine(ShowGameOverPanel());
    }

    public void TriggerVictory()
    {
        if (victoryAchieved) return;
        victoryAchieved = true;
        if (victorySound != null && gameAudioSource != null) gameAudioSource.PlayOneShot(victorySound);
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

    // ---------- SCENES ----------
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
        else
        {
            Debug.LogWarning($"Main Menu scene '{mainMenuSceneName}' not found. Restarting current scene.");
            RestartGame();
        }
    }

    public bool IsGameOver() => gameOver;
    public bool IsVictoryAchieved() => victoryAchieved;
    public float GetGameTime() => gameTime;
}
