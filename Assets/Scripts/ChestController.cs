using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class ChestController : MonoBehaviour
{
    private const string OpenAnimatorBool = "Open";
    private const string ClosedStateName = "Golden Chest 1 Closed";

    private static readonly List<ChestController> ActiveChests = new();

    [SerializeField] int diamondReward = 10;
    [SerializeField] GameObject actionRoot;
    [SerializeField] GameObject actionButton;
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Animator chestAnimator;

    Button button;
    InputAction interactAction;
    int playersInRange;
    bool isOpenedThisRun;

    public static void ResetAllForNewRun()
    {
        foreach (var chest in ActiveChests)
            chest.ResetForNewRun();
    }

    void Awake()
    {
        EnsureInteractTrigger();
        AutoFindReferences();
        ApplyClosedVisual();
        HideButton();
    }

    void EnsureInteractTrigger()
    {
        foreach (var collider in GetComponents<Collider2D>())
        {
            if (collider.isTrigger)
                return;
        }

        var trigger = gameObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.offset = new Vector2(0.0067860484f, -0.0339289f);
        trigger.size = new Vector2(4f, 4f);
    }

    void AutoFindReferences()
    {
        if (chestAnimator == null)
            chestAnimator = GetComponent<Animator>();

        if (actionRoot == null)
            actionRoot = transform.Find("Action")?.gameObject;

        if (actionButton == null && actionRoot != null)
            actionButton = FindChildByName(actionRoot.transform, "ActionButton");

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
        ActiveChests.Add(this);
        interactAction?.Enable();

        if (button != null)
        {
            button.onClick.RemoveListener(OpenChest);
            button.onClick.AddListener(OpenChest);
        }
    }

    void OnDisable()
    {
        ActiveChests.Remove(this);
        interactAction?.Disable();

        if (button != null)
            button.onClick.RemoveListener(OpenChest);
    }

    void Update()
    {
        if (isOpenedThisRun || playersInRange <= 0 || IsUiBlocked())
            return;

        if (interactAction != null && interactAction.WasPressedThisFrame())
            OpenChest();
        else if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            OpenChest();
    }

    static bool IsUiBlocked()
    {
        return Time.timeScale == 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || isOpenedThisRun)
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

    void OpenChest()
    {
        if (isOpenedThisRun || playersInRange <= 0 || IsUiBlocked())
            return;

        isOpenedThisRun = true;
        HideButton();
        PlayerProgress.AddDiamonds(diamondReward);

        if (chestAnimator != null)
            chestAnimator.SetBool(OpenAnimatorBool, true);
    }

    void ResetForNewRun()
    {
        isOpenedThisRun = false;
        ApplyClosedVisual();
        HideButton();

        if (playersInRange > 0)
            ShowButton();
    }

    void ApplyClosedVisual()
    {
        if (chestAnimator == null)
            return;

        chestAnimator.SetBool(OpenAnimatorBool, false);
        chestAnimator.Play(ClosedStateName, 0, 0f);
        chestAnimator.Update(0f);
    }
}
