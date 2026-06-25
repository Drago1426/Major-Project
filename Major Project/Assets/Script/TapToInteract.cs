using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine;

public class TapToInteract : MonoBehaviour
{
    [Header("Input")]
    public bool allowTouchInput = true;
    public bool allowMouseInput = true;
    public bool ignoreWhenPointerOverUi = true;

    [Header("Raycast")]
    public Camera arCamera;
    public LayerMask interactLayer = ~0; // everything by default
    public float maxRayDistance = 1000f;
    public bool logHits = true;

    readonly List<RaycastResult> _uiRaycastResults = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoInstallOnMainCamera()
    {
        if (FindAnyObjectByType<TapToInteract>() != null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            camera = FindAnyObjectByType<Camera>();

        if (camera == null)
            return;

        var input = camera.gameObject.AddComponent<TapToInteract>();
        input.arCamera = camera;
        Debug.Log($"TapToInteract auto-installed on '{camera.gameObject.name}'.");
    }

    void Awake()
    {
        TryResolveCamera();
    }

    void Update()
    {
        if (arCamera == null && !TryResolveCamera())
            return;

        if (allowTouchInput && Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                TryHit(touch.position.ReadValue());
            }
        }

        if (allowMouseInput && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHit(Mouse.current.position.ReadValue());
        }
    }

    bool TryResolveCamera()
    {
        if (arCamera != null)
            return true;

        arCamera = Camera.main;
        if (arCamera == null)
            arCamera = FindAnyObjectByType<Camera>();

        return arCamera != null;
    }

    void TryHit(Vector2 screenPos)
    {
        if (IsPointerOverUi(screenPos))
            return;

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, interactLayer, QueryTriggerInteraction.Collide))
        {
            if (logHits)
                Debug.Log("Hit: " + hit.collider.name);

            var interactable = hit.collider.GetComponentInParent<CreatureInteractable>();
            if (interactable != null)
            {
                if (logHits)
                    Debug.Log("Creature tapped!");

                interactable.OnTapped(hit.point);
            }
        }
    }

    bool IsPointerOverUi(Vector2 screenPos)
    {
        if (!ignoreWhenPointerOverUi || EventSystem.current == null)
            return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }
}
