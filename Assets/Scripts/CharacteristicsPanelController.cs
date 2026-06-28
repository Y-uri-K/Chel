using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class CharacteristicsPanelController : MonoBehaviour
{
    public static CharacteristicsPanelController Instance { get; private set; }

    [SerializeField] GameObject characteristicsPanel;
    [SerializeField] Button itemsButton;
    [SerializeField] Button backButton;
    [SerializeField] InputActionAsset uiInputActions;

    bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        AutoFindReferences();
        HidePanel();
        BindButtons();
    }

    void Update()
    {
        if (IsBlocked())
            return;

        if (Keyboard.current == null || !Keyboard.current.iKey.wasPressedThisFrame)
            return;

        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ForceClosePanel()
    {
        if (!isOpen)
            return;

        ClosePanel();
    }

    void AutoFindReferences()
    {
        if (characteristicsPanel == null)
        {
            var panelTransform = transform.Find("ItemsButton/CharacteristicsPanel");
            if (panelTransform != null)
                characteristicsPanel = panelTransform.gameObject;
        }

        if (characteristicsPanel == null)
            characteristicsPanel = FindInactiveByName("CharacteristicsPanel");

        if (itemsButton == null)
        {
            var items = transform.Find("ItemsButton");
            if (items != null)
                itemsButton = items.GetComponent<Button>();
        }

        if (itemsButton == null)
        {
            var itemsObject = GameObject.Find("ItemsButton");
            if (itemsObject != null)
                itemsButton = itemsObject.GetComponent<Button>();
        }

        if (backButton == null && characteristicsPanel != null)
            backButton = FindButtonInChildren(characteristicsPanel.transform, "BackButton");

        if (uiInputActions == null)
        {
            var pauseMenu = GetComponent<PauseMenuController>();
            if (pauseMenu != null)
                uiInputActions = pauseMenu.GetUiInputActions();
        }
    }

    static GameObject FindInactiveByName(string objectName)
    {
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name != objectName || t.hideFlags != HideFlags.None)
                continue;

            if (t.gameObject.scene.IsValid())
                return t.gameObject;
        }

        return null;
    }

    static Button FindButtonInChildren(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.GetComponent<Button>();
        }

        return null;
    }

    void BindButtons()
    {
        if (itemsButton != null)
        {
            itemsButton.onClick.RemoveAllListeners();
            itemsButton.onClick.AddListener(OpenPanel);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ClosePanel);
        }
    }

    void OpenPanel()
    {
        if (isOpen || IsBlocked())
            return;

        var pauseMenu = GetComponent<PauseMenuController>();
        if (pauseMenu != null)
            pauseMenu.ForceClosePauseMenu();

        isOpen = true;
        Time.timeScale = 0f;
        EnsureUiInput();

        if (characteristicsPanel != null)
            characteristicsPanel.SetActive(true);

        RefreshPanelStats();

        if (itemsButton != null)
            itemsButton.interactable = false;
    }

    void ClosePanel()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Time.timeScale = 1f;
        HidePanel();

        if (itemsButton != null)
            itemsButton.interactable = true;
    }

    void HidePanel()
    {
        if (characteristicsPanel != null)
            characteristicsPanel.SetActive(false);
    }

    void RefreshPanelStats()
    {
        if (characteristicsPanel == null)
            return;

        var view = characteristicsPanel.GetComponentInChildren<CharacteristicsPanelView>(true);
        if (view != null)
            view.Refresh();
    }

    bool IsBlocked()
    {
        if (LevelDeathController.Instance != null && LevelDeathController.Instance.IsDead)
            return true;

        return LevelWinController.Instance != null && LevelWinController.Instance.HasWon;
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
    }
}
