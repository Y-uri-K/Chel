using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelDeathController : MonoBehaviour
{
    public static LevelDeathController Instance { get; private set; }

    [SerializeField] GameObject diePanel;
    [SerializeField] InputActionAsset uiInputActions;

    GameObject dieOptionsMenu;
    bool isDead;

    public bool IsDead => isDead;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        AutoFindReferences();
        HideDiePanel();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void AutoFindReferences()
    {
        if (diePanel == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                diePanel = FindChildByName(canvas.transform, "DiePanel");

            if (diePanel == null)
                diePanel = FindInActiveSceneByName("DiePanel");
        }

        if (uiInputActions == null)
        {
            var pauseMenu = GetComponent<PauseMenuController>();
            if (pauseMenu != null)
                uiInputActions = pauseMenu.GetUiInputActions();
        }

        if (diePanel != null)
            dieOptionsMenu = FindChildByName(diePanel.transform, "OptionsMenu");
    }

    static GameObject FindChildByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    static GameObject FindInActiveSceneByName(string objectName)
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindChildByName(root.transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    void BindDiePanelButtons()
    {
        if (diePanel == null)
            return;

        BindNamedButtonIn(diePanel.transform, "RestartButton", GoToHub);
        BindNamedButtonIn(diePanel.transform, "DieButton", GoToHub);
        BindNamedButtonIn(diePanel.transform, "QuitButton", QuitGame);
        BindNamedButtonIn(diePanel.transform, "OptionsButton", OpenDieOptions);

        if (dieOptionsMenu != null)
            BindNamedButtonIn(dieOptionsMenu.transform, "BackButton", CloseDieOptions);

        DisableTextRaycasts(diePanel.transform);
    }

    static void BindNamedButtonIn(Transform root, string objectName, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = FindChildByName(root, objectName);
        if (buttonObject == null)
            return;

        var button = buttonObject.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
        button.interactable = true;
    }

    static void DisableTextRaycasts(Transform root)
    {
        foreach (var text in root.GetComponentsInChildren<Text>(true))
            text.raycastTarget = false;
    }

    void EnsureUiInput()
    {
        var pauseMenu = GetComponent<PauseMenuController>();
        if (pauseMenu != null)
            pauseMenu.EnsureUiReady();

        if (EventSystem.current == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            var inputModule = eventSystemGo.AddComponent<InputSystemUIInputModule>();
            if (uiInputActions != null)
                inputModule.actionsAsset = uiInputActions;
        }
        else
        {
            var inputModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
                inputModule = EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();

            if (uiInputActions != null && inputModule.actionsAsset == null)
                inputModule.actionsAsset = uiInputActions;
        }

        var canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    public void HandlePlayerDeath()
    {
        if (isDead)
            return;

        isDead = true;
        Time.timeScale = 0f;

        ChestController.ResetAllForNewRun();

        var pauseMenu = GetComponent<PauseMenuController>();
        if (pauseMenu != null)
            pauseMenu.ForceClosePauseMenu();

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false;

            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            var stats = player.GetComponent<CharacterStats>();
            if (stats != null && stats.CurrentHealth > 0)
                stats.TakeDamage(stats.MaxHealth);
        }

        ShowDiePanel();
    }

    void ShowDiePanel()
    {
        if (diePanel == null)
        {
            Debug.LogWarning("LevelDeathController: DiePanel не найден на сцене.");
            return;
        }

        EnsureUiInput();
        BindDiePanelButtons();
        CloseDieOptions();

        diePanel.SetActive(true);
        diePanel.transform.SetAsLastSibling();

        var pauseMenu = GetComponent<PauseMenuController>();
        var overlay = pauseMenu != null ? pauseMenu.GetPauseOverlay() : null;
        if (overlay != null)
            overlay.gameObject.SetActive(false);
    }

    void HideDiePanel()
    {
        if (diePanel != null)
            diePanel.SetActive(false);
    }

    void OpenDieOptions()
    {
        if (dieOptionsMenu != null)
            dieOptionsMenu.SetActive(true);
    }

    void CloseDieOptions()
    {
        if (dieOptionsMenu != null)
            dieOptionsMenu.SetActive(false);
    }

    public void GoToHub()
    {
        SceneNav.LoadHub();
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
