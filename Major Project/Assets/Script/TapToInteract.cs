using UnityEngine.InputSystem;
using UnityEngine;

public class TapToInteract : MonoBehaviour
{
    [Header("Raycast")]
    public Camera arCamera;
    public LayerMask interactLayer = ~0; // everything by default

    void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;
    }

    void Update()
    {
        if (arCamera == null) return;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                TryHit(touch.position.ReadValue());
            }
        }

    #if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHit(Mouse.current.position.ReadValue());
        }
    #endif
    }

    void TryHit(Vector2 screenPos)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactLayer))
        {
            Debug.Log("Hit: " + hit.collider.name);

            var interactable = hit.collider.GetComponentInParent<CreatureInteractable>();
            if (interactable != null)
            {
                Debug.Log("Creature tapped!");
                interactable.OnTapped();
            }
        }
    }
}
