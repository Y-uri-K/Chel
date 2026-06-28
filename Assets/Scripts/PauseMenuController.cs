using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject navPanel;
    [SerializeField] GameObject optionsMenu;
    [SerializeField] Image pauseOverlay;
    [SerializeField] Button burgerMenuButton;
    [SerializeField] Button returnButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button optionsButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button optionsBackButton;
    [SerializeField] InputActionAsset uiInputActions;

    [Header("Restart")]
    [SerializeField] Vector2 playerSpawnPosition;
    [SerializeField] bool restartLoadsHubScene = false;

    bool isPaused;
    bool spawnCaptured;

    public bool IsPaused => isPaused;

    void Awake()
    {
        AutoFindReferences();
        EnsureEventSystem();
        EnsureCanvasCamera();
        EnsureOverlay();
        HidePauseMenu();
        BindButtons();
        CaptureSpawnPosition();
    }

    void Update()
    {
        if (IsDeathBlockingInput())
            return;

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        HandleEscapePressed();
    }

    bool IsDeathBlockingInput()
    {
        if (LevelDeathController.Instance != null && LevelDeathController.Instance.IsDead)
            return true;

        return LevelWinController.Instance != null && LevelWinController.Instance.HasWon;
    }

    bool IsCharacteristicsBlockingInput()
    {
        return CharacteristicsPanelController.Instance != null && CharacteristicsPanelController.Instance.IsOpen;
    }

    bool IsShopBlockingInput()
    {
        var trader = FindFirstObjectByType<TraderController>();
        return trader != null && trader.IsShopOpen;
    }

    void HandleEscapePressed()
    {
        var trader = FindFirstObjectByType<TraderController>();
        if (trader != null && trader.IsShopOpen)
        {
            trader.CloseShop();
            return;
        }

        if (IsCharacteristicsBlockingInput())
        {
            CharacteristicsPanelController.Instance.ForceClosePanel();
            return;
        }

        if (optionsMenu != null && optionsMenu.activeSelf)
        {
            CloseOptionsImmediate();
            return;
        }

        TogglePauseMenu();
    }

    void CaptureSpawnPosition()
    {
        if (spawnCaptured)
            return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        var rb = player.GetComponent<Rigidbody2D>();
        playerSpawnPosition = rb != null ? rb.position : player.transform.position;
        spawnCaptured = true;
    }

    void AutoFindReferences()
    {
        if (navPanel == null)
        {
            var nav = transform.Find("NavPanel");
            if (nav != null)
                navPanel = nav.gameObject;
        }

        if (optionsMenu == null && navPanel != null)
        {
            var optionsTransform = navPanel.transform.Find("OptionsMenu");
            if (optionsTransform != null)
                optionsMenu = optionsTransform.gameObject;
        }

        if (burgerMenuButton == null)
            burgerMenuButton = FindButton("BurgerMenuButton");

        if (returnButton == null)
            returnButton = FindButton("ReturnButton");

        if (restartButton == null)
            restartButton = FindButton("RestartButton");

        if (optionsButton == null)
            optionsButton = FindButton("OptionsButton");

        if (quitButton == null)
            quitButton = FindButton("QuitButton");

        if (optionsBackButton == null && optionsMenu != null)
            optionsBackButton = optionsMenu.transform.Find("BackButton")?.GetComponent<Button>();
    }

    static Button FindButton(string objectName)
    {
        var target = GameObject.Find(objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    public void EnsureUiReady()
    {
        EnsureEventSystem();
        EnsureCanvasCamera();
    }

    public InputActionAsset GetUiInputActions() => uiInputActions;

    public Image GetPauseOverlay() => pauseOverlay;

    void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();

        var inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
        if (uiInputActions != null)
            inputModule.actionsAsset = uiInputActions;
    }

    void EnsureCanvasCamera()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    void EnsureOverlay()
    {
        if (pauseOverlay != null)
            return;

        var overlayObject = new GameObject("PauseOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(transform, false);
        overlayObject.transform.SetAsFirstSibling();

        var rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        pauseOverlay = overlayObject.GetComponent<Image>();
        pauseOverlay.color = new Color(0f, 0f, 0f, 0.65f);
        pauseOverlay.raycastTarget = true;
    }

    void BindButtons()
    {
        BindButton(burgerMenuButton, TogglePauseMenu);
        BindButton(returnButton, ResumeGame);
        BindButton(restartButton, RestartHub);
        BindButton(optionsButton, OpenOptions);
        BindButton(quitButton, QuitGame);
        BindButton(optionsBackButton, CloseOptions);
    }

    static void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    void TogglePauseMenu()
    {
        if (IsDeathBlockingInput() || IsCharacteristicsBlockingInput() || IsShopBlockingInput())
            return;

        if (isPaused)
            ResumeGame();
        else
            OpenPauseMenu();
    }

    public void ForceClosePauseMenu()
    {
        isPaused = false;
        HidePauseMenu();
    }

    void OpenPauseMenu()
    {
        if (isPaused)
            return;

        if (CharacteristicsPanelController.Instance != null)
            CharacteristicsPanelController.Instance.ForceClosePanel();

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(true);

        if (navPanel != null)
            navPanel.SetActive(true);

        CloseOptionsImmediate();
        KeepBurgerAboveOverlay();
    }

    void KeepBurgerAboveOverlay()
    {
        if (burgerMenuButton != null)
            burgerMenuButton.transform.SetAsLastSibling();
    }

    void ResumeGame()
    {
        if (!isPaused)
            return;

        isPaused = false;
        Time.timeScale = 1f;
        HidePauseMenu();
    }

    void HidePauseMenu()
    {
        CloseOptionsImmediate();

        if (navPanel != null)
            navPanel.SetActive(false);

        if (pauseOverlay != null)
            pauseOverlay.gameObject.SetActive(false);
    }

    void OpenOptions()
    {
        if (!isPaused)
            return;

        if (optionsMenu != null)
            optionsMenu.SetActive(true);
    }

    void CloseOptions()
    {
        if (!isPaused)
            return;

        CloseOptionsImmediate();
    }

    void CloseOptionsImmediate()
    {
        if (optionsMenu != null)
            optionsMenu.SetActive(false);
    }

    void RestartHub()
    {
        if (restartLoadsHubScene)
        {
            SceneNav.LoadHub();
            return;
        }

        Time.timeScale = 1f;
        isPaused = false;

        ChestController.ResetAllForNewRun();
        MinotaurBossEncounter.ResetForNewRun();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (rb != null)
                rb.position = playerSpawnPosition;
            else
                player.transform.position = playerSpawnPosition;

            var stats = player.GetComponent<CharacterStats>();
            if (stats != null)
                stats.ResetToFullHealth();
        }

        HidePauseMenu();
    }

    void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
