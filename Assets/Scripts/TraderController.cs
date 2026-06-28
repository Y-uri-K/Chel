using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TraderController : MonoBehaviour
{
    [SerializeField] GameObject actionRoot;
    [SerializeField] GameObject actionButton;
    [SerializeField] GameObject shopRoot;
    [SerializeField] InputActionAsset inputActions;

    Button button;
    Button shopBackButton;
    InputAction interactAction;
    int playersInRange;
    bool shopOpen;

    public bool IsShopOpen => shopOpen;

    void Awake()
    {
        AutoFindReferences();
        BindTriggerColliders();
        HideButton();

        if (shopRoot != null)
            shopRoot.SetActive(false);
    }

    void AutoFindReferences()
    {
        if (actionRoot == null)
            actionRoot = transform.Find("Action")?.gameObject;

        if (actionButton == null && actionRoot != null)
            actionButton = FindChildByName(actionRoot.transform, "ActionButton");

        if (shopRoot == null)
            shopRoot = transform.Find("Shop")?.gameObject;

        if (actionButton != null)
            button = actionButton.GetComponent<Button>();

        if (shopBackButton == null && shopRoot != null)
            shopBackButton = FindButtonInChildren(shopRoot.transform, "BackButton");

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

    void BindTriggerColliders()
    {
        DisableMisplacedChildTriggers();

        var collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.offset = Vector2.zero;
            box.size = new Vector2(320f, 140f);
            return;
        }

        collider.isTrigger = true;
    }

    void DisableMisplacedChildTriggers()
    {
        foreach (var collider in GetComponentsInChildren<Collider2D>(true))
        {
            if (collider.gameObject == gameObject)
                continue;

            if (actionRoot != null && collider.transform.IsChildOf(actionRoot.transform))
                continue;

            if (shopRoot != null && collider.transform.IsChildOf(shopRoot.transform))
                continue;

            collider.enabled = false;
        }
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

    static Button FindButtonInChildren(Transform root, string objectName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child.GetComponent<Button>();
        }

        return null;
    }

    void OnEnable()
    {
        interactAction?.Enable();

        if (button != null)
        {
            button.onClick.RemoveListener(OpenShop);
            button.onClick.AddListener(OpenShop);
        }

        if (shopBackButton != null)
        {
            shopBackButton.onClick.RemoveListener(CloseShop);
            shopBackButton.onClick.AddListener(CloseShop);
        }
    }

    void OnDisable()
    {
        interactAction?.Disable();

        if (button != null)
            button.onClick.RemoveListener(OpenShop);

        if (shopBackButton != null)
            shopBackButton.onClick.RemoveListener(CloseShop);
    }

    void Update()
    {
        if (shopOpen)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseShop();
            return;
        }

        if (IsUiBlocked())
            return;

        if (playersInRange <= 0)
            return;

        if (interactAction != null && interactAction.WasPressedThisFrame())
            OpenShop();
        else if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenShop();
    }

    static bool IsUiBlocked()
    {
        return Time.timeScale == 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleTriggerEnter(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        HandleTriggerExit(other);
    }

    public void HandleTriggerEnter(Collider2D other)
    {
        if (shopOpen || !IsPlayerCollider(other))
            return;

        playersInRange++;
        ShowButton();
    }

    public void HandleTriggerExit(Collider2D other)
    {
        if (!IsPlayerCollider(other))
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

    static bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<PlayerController>() != null;
    }

    void OpenShop()
    {
        if (playersInRange <= 0 || shopRoot == null)
            return;

        if (!shopOpen && Time.timeScale > 0f)
            Time.timeScale = 0f;

        shopOpen = true;
        HideButton();
        shopRoot.SetActive(true);

        var pauseMenu = FindFirstObjectByType<PauseMenuController>();
        pauseMenu?.EnsureUiReady();
    }

    public void CloseShop()
    {
        shopOpen = false;

        if (shopRoot != null)
            shopRoot.SetActive(false);

        var pauseMenu = FindFirstObjectByType<PauseMenuController>();
        if (pauseMenu == null || !pauseMenu.IsPaused)
            Time.timeScale = 1f;

        if (playersInRange > 0)
            ShowButton();
    }
}
