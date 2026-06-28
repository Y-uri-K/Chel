using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelWinController : MonoBehaviour
{
    public static LevelWinController Instance { get; private set; }

    [SerializeField] GameObject winPanel;
    [SerializeField] InputActionAsset uiInputActions;
    [SerializeField] string bossName = "DemonSlime";
    [SerializeField] string mainMenuSceneName = "menu";

    MonsterStats bossStats;
    MonsterAnimation bossAnim;
    Coroutine winRoutine;
    GameObject winOptionsMenu;
    bool hasWon;

    public bool HasWon => hasWon;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        AutoFindReferences();
        HideWinPanel();
    }

    void Start()
    {
        BindBoss();
    }

    void OnDestroy()
    {
        UnbindBoss();
        CancelWinRoutine();

        if (Instance == this)
            Instance = null;
    }

    void AutoFindReferences()
    {
        if (winPanel == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                winPanel = FindChildByName(canvas.transform, "WinPanel");

            if (winPanel == null)
                winPanel = FindInActiveSceneByName("WinPanel");
        }

        if (uiInputActions == null)
        {
            var pauseMenu = GetComponent<PauseMenuController>();
            if (pauseMenu != null)
                uiInputActions = pauseMenu.GetUiInputActions();
        }

        if (winPanel != null && winOptionsMenu == null)
            winOptionsMenu = FindChildByName(winPanel.transform, "OptionsMenu");
    }

    void BindBoss()
    {
        UnbindBoss();

        var boss = GameObject.Find(bossName);
        if (boss == null)
        {
            Debug.LogWarning($"[LevelWinController] Босс '{bossName}' не найден на сцене.");
            return;
        }

        bossStats = boss.GetComponent<MonsterStats>();
        if (bossStats == null)
        {
            Debug.LogWarning($"[LevelWinController] У '{bossName}' нет MonsterStats.");
            return;
        }

        if (bossStats.IsDead)
            HandleBossDeath(bossStats);
        else
            bossStats.OnDeath += HandleBossDeath;
    }

    void UnbindBoss()
    {
        if (bossStats != null)
            bossStats.OnDeath -= HandleBossDeath;

        if (bossAnim != null)
            bossAnim.OnAnimationFinished -= HandleBossDeathAnimationFinished;

        bossStats = null;
        bossAnim = null;
    }

    void HandleBossDeath(MonsterStats stats)
    {
        if (hasWon || winRoutine != null)
            return;

        bossAnim = stats.GetComponent<MonsterAnimation>();
        if (bossAnim != null)
        {
            bossAnim.OnAnimationFinished += HandleBossDeathAnimationFinished;

            if (bossAnim.CurrentState == MonsterState.Death && bossAnim.AnimationFinished)
                HandleBossDeathAnimationFinished(MonsterState.Death);
            else
                winRoutine = StartCoroutine(WaitForDeathAnimationFallback());

            return;
        }

        float duration = 1f;
        winRoutine = StartCoroutine(WaitAndCompleteWin(duration));
    }

    void HandleBossDeathAnimationFinished(MonsterState state)
    {
        if (state != MonsterState.Death || hasWon)
            return;

        CancelWinRoutine();
        CompleteWin();
    }

    IEnumerator WaitForDeathAnimationFallback()
    {
        float duration = bossAnim != null ? bossAnim.GetClipDuration(MonsterState.Death) : 0f;
        if (duration <= 0f)
            duration = 1f;

        yield return new WaitForSeconds(duration);

        if (!hasWon)
            CompleteWin();
    }

    IEnumerator WaitAndCompleteWin(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (!hasWon)
            CompleteWin();
    }

    void CancelWinRoutine()
    {
        if (winRoutine == null)
            return;

        StopCoroutine(winRoutine);
        winRoutine = null;
    }

    void CompleteWin()
    {
        if (hasWon)
            return;

        if (bossAnim != null)
        {
            bossAnim.OnAnimationFinished -= HandleBossDeathAnimationFinished;
            bossAnim = null;
        }

        winRoutine = null;
        hasWon = true;
        Time.timeScale = 0f;

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
        }

        ShowWinPanel();
    }

    void ShowWinPanel()
    {
        if (winPanel == null)
        {
            Debug.LogWarning("[LevelWinController] WinPanel не найден на сцене.");
            return;
        }

        EnsureUiInput();
        BindWinPanelButtons();
        CloseWinOptions();

        winPanel.SetActive(true);
        winPanel.transform.SetAsLastSibling();

        var pauseMenu = GetComponent<PauseMenuController>();
        var overlay = pauseMenu != null ? pauseMenu.GetPauseOverlay() : null;
        if (overlay != null)
            overlay.gameObject.SetActive(false);
    }

    void HideWinPanel()
    {
        CloseWinOptions();

        if (winPanel != null)
            winPanel.SetActive(false);
    }

    void BindWinPanelButtons()
    {
        if (winPanel == null)
            return;

        BindNamedButtonIn(winPanel.transform, "MainMenuButton", GoToMainMenu);
        BindNamedButtonIn(winPanel.transform, "OptionsButton", OpenWinOptions);
        BindNamedButtonIn(winPanel.transform, "QuitButton", QuitGame);

        if (winOptionsMenu != null)
            BindNamedButtonIn(winOptionsMenu.transform, "BackButton", CloseWinOptions);

        DisableTextRaycasts(winPanel.transform);
    }

    void OpenWinOptions()
    {
        if (winOptionsMenu != null)
            winOptionsMenu.SetActive(true);
    }

    void CloseWinOptions()
    {
        if (winOptionsMenu != null)
            winOptionsMenu.SetActive(false);
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

    public void GoToMainMenu()
    {
        SceneNav.LoadMenu();
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
}
