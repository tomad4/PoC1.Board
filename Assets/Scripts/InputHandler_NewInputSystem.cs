using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI; // GraphicRaycaster

public class InputHandler_NewInputSystem : MonoBehaviour
{
    public Camera cam;
    public LayerMask tileLayer = ~0; // Everything for testing
    public TurnManager turnManager;

    // cache the GraphicRaycasters in the scene (Canvas UI)
    private List<GraphicRaycaster> graphicRaycasters = new List<GraphicRaycaster>();

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        // find all GraphicRaycasters (if you have multiple canvases)
        graphicRaycasters.Clear();
        var found = FindObjectsOfType<GraphicRaycaster>();
        foreach (var g in found) graphicRaycasters.Add(g);

        if (graphicRaycasters.Count == 0)
            Debug.LogWarning("InputHandler: No GraphicRaycaster found in scene. UI-hit detection will be limited.");
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (turnManager == null) return;

        // Handle mouse left click (new input system)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Debug.Log($"InputHandler: Mouse pressed at {screenPos}");

            // 1) Check UI under cursor using GraphicRaycaster(s)
            if (IsPointerOverUI(screenPos))
            {
                Debug.Log("InputHandler: Click over UI - ignored");
                return;
            }

            // 2) Physics raycast to world
            Ray r = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(r, out RaycastHit hit, 100f, tileLayer))
            {
                Debug.Log("InputHandler: Physics hit: " + hit.collider.name + " (layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer) + ")");
                HexTile tile = hit.collider.GetComponent<HexTile>();
                if (tile != null)
                {
                    turnManager.RequestMoveTo(tile);
                }
                else
                {
                    Debug.Log("InputHandler: Hit collider has no HexTile component.");
                }
            }
            else
            {
                Debug.Log("InputHandler: Raycast missed — screenPos: " + screenPos);
            }
        }

        // Toggle camera via Tab — call instance ToggleView
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            var camCtrl = FindObjectOfType<CameraController>();
            if (camCtrl != null) camCtrl.ToggleView();
            else Debug.LogWarning("InputHandler: CameraController not found to toggle view.");
        }
    }

    // Use GraphicRaycaster + PointerEventData to detect UI elements under pointer
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var ped = new PointerEventData(EventSystem.current) { position = screenPosition };
        var results = new List<RaycastResult>();

        if (graphicRaycasters.Count == 0)
        {
            EventSystem.current.RaycastAll(ped, results);
        }
        else
        {
            foreach (var gr in graphicRaycasters)
            {
                results.Clear();
                gr.Raycast(ped, results);
                if (results.Count > 0)
                {
                    foreach (var r in results)
                        Debug.Log($"InputHandler: UI hit -> {r.gameObject.name} (canvas:{gr.gameObject.name})");
                    return true;
                }
            }
            return false;
        }

        if (results.Count > 0)
        {
            foreach (var r in results) Debug.Log($"InputHandler: UI hit (fallback) -> {r.gameObject.name}");
            return true;
        }

        return false;
    }
}
