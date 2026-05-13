using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class FireballCardTargeting : MonoBehaviour
{
    [Header("Fireball Card Tracking")]
    [SerializeField] ObserverBehaviour fireballObserver;

    [Header("Selectable Model Roots")]
    [SerializeField] List<GameObject> selectableModels = new List<GameObject>();

    [Header("Selection Visual")]
    [SerializeField] Color highlightColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] float emissionIntensity = 2.2f;

    [Header("Animation")]
    [SerializeField] string fireballTriggerName = "Fireball";

    bool fireballCardTracked;
    readonly List<Renderer> highlightedRenderers = new List<Renderer>();

    void Awake()
    {
        if (fireballObserver == null)
            fireballObserver = GetComponent<ObserverBehaviour>();
    }

    void OnEnable()
    {
        if (fireballObserver != null)
            fireballObserver.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    void OnDisable()
    {
        if (fireballObserver != null)
            fireballObserver.OnTargetStatusChanged -= OnTargetStatusChanged;

        ClearHighlights();
        fireballCardTracked = false;
    }

    void Update()
    {
        if (!fireballCardTracked)
            return;

        if (!WasTapPressedThisFrame())
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(GetTapPosition());
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        TryTriggerFireball(hit.collider);
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isTracked =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        if (isTracked == fireballCardTracked)
            return;

        fireballCardTracked = isTracked;

        if (fireballCardTracked)
            ApplyHighlights();
        else
            ClearHighlights();
    }

    void ApplyHighlights()
    {
        ClearHighlights();

        foreach (var model in selectableModels)
        {
            if (model == null)
                continue;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                highlightedRenderers.Add(renderer);
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                renderer.SetPropertyBlock(block);
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var renderer in highlightedRenderers)
        {
            if (renderer == null)
                continue;

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(block);
        }

        highlightedRenderers.Clear();
    }

    void TryTriggerFireball(Collider tappedCollider)
    {
        foreach (var model in selectableModels)
        {
            if (model == null)
                continue;

            Transform root = model.transform;
            if (tappedCollider.transform != root && !tappedCollider.transform.IsChildOf(root))
                continue;

            var animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null && !string.IsNullOrWhiteSpace(fireballTriggerName))
                animator.SetTrigger(fireballTriggerName);

            return;
        }
    }

    bool WasTapPressedThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Began;

        return Input.GetMouseButtonDown(0);
    }

    Vector3 GetTapPosition()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).position;

        return Input.mousePosition;
    }
}