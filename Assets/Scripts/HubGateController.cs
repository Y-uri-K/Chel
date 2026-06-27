using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class HubGateController : MonoBehaviour
{
    [SerializeField] GameObject actionRoot;
    [SerializeField] GameObject actionButton;
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] string targetSceneName = "level1";

    Button button;
    InputAction interactAction;
    int playersInRange;

    void Awake()
    {
        var collider = GetComponent<Collider2D>();
        collider.isTrigger = true;

        AutoFindReferences();
        HideButton();
    }

    void AutoFindReferences()
    {
        if (actionRoot == null)
            actionRoot = transform.Find("Action")?.gameObject;

        if (actionRoot == null)
            actionRoot = GameObject.Find("Action");

        if (actionButton == null && actionRoot != null)
            actionButton = FindChildByName(actionRoot.transform, "ActionButton");

        if (actionButton == null)
            actionButton = GameObject.Find("ActionButton");

        if (actionButton != null)
            button = actionButton.GetComponent<Button>();

        if (inputActions == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var controller = player.GetComponent<PlayerController>();
                inputActions = controller != null ? controller.GetInputActions() : null;
            }
        }

        if (inputActions != null)
            interactAction = inputActions.FindActionMap("Player")?.FindAction("Interact");
    }

    static GameObject FindChildByName(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    void OnEnable()
    {
        interactAction?.Enable();

        if (button != null)
        {
            button.onClick.RemoveListener(EnterLevel);
            button.onClick.AddListener(EnterLevel);
        }
    }

    void OnDisable()
    {
        interactAction?.Disable();

        if (button != null)
            button.onClick.RemoveListener(EnterLevel);
    }

    void Update()
    {
        if (playersInRange <= 0 || IsUiBlocked())
            return;

        if (interactAction != null && interactAction.WasPressedThisFrame())
            EnterLevel();
        else if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            EnterLevel();
    }

    static bool IsUiBlocked()
    {
        return Time.timeScale == 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInRange++;
        ShowButton();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playersInRange = Mathf.Max(0, playersInRange - 1);
        if (playersInRange == 0)
            HideButton();
    }

    void ShowButton()
    {
        if (actionRoot == null && actionButton == null)
            return;

        var pauseMenu = FindFirstObjectByType<PauseMenuController>();
        pauseMenu?.EnsureUiReady();
        EnsureActionCanvasCamera();

        if (actionRoot != null)
            actionRoot.SetActive(true);

        if (actionButton != null)
            actionButton.SetActive(true);
    }

    void HideButton()
    {
        if (actionRoot != null)
        {
            actionRoot.SetActive(false);
            return;
        }

        if (actionButton != null)
            actionButton.SetActive(false);
    }

    void EnsureActionCanvasCamera()
    {
        if (actionRoot == null)
            return;

        var canvas = actionRoot.GetComponent<Canvas>();
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
            return;

        if (canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    void EnterLevel()
    {
        if (playersInRange <= 0 || IsUiBlocked())
            return;

        SceneNav.LoadLevel(targetSceneName);
    }
}
