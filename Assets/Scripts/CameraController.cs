using UnityEngine;
using UnityEngine.InputSystem;
/// <summary>
/// CameraController: follow player or board center, toggle iso/top-down, smooth follow.
/// Attach to an empty GameObject; set camTransform to Main Camera in inspector.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("References")]
    public Transform camTransform; // main camera transform
    public HexGridGenerator grid;
    public Transform followTarget; // player transform (optional)

    [Header("Offsets")]
    public Vector3 isoOffset = new Vector3(-6f, 8f, -6f);
    public Vector3 topOffset = new Vector3(0f, 20f, 0f);

    [Header("Follow")]
    public float smoothTime = 0.25f;
    public bool startTopDown = false;

    bool topDown;
    Vector3 velocity = Vector3.zero;
    Vector3 boardCenter = Vector3.zero;

    void Awake()
    {
        if (camTransform == null && Camera.main != null)
            camTransform = Camera.main.transform;
    }

    void Start()
    {
        // Auto-find followTarget if not provided
        if (followTarget == null)
        {
            var p = FindObjectOfType<PlayerPawn>();
            if (p != null)
            {
                followTarget = p.transform;
                Debug.Log("CameraController: auto-found PlayerPawn as followTarget.");
            }
        }

        topDown = startTopDown;
        RecalculateBoardCenter();
        CenterImmediate();
    }

    void LateUpdate()
    {
        if (grid != null)
            RecalculateBoardCenter();

        Vector3 desired;
        if (topDown)
        {
            desired = (followTarget != null) ? followTarget.position + topOffset : boardCenter + topOffset;
        }
        else
        {
            desired = (followTarget != null) ? followTarget.position + isoOffset : boardCenter + isoOffset;
        }

        // smooth move camera
        if (camTransform != null)
            camTransform.position = Vector3.SmoothDamp(camTransform.position, desired, ref velocity, smoothTime);

        // orient camera to look at the board center (or player)
        if (camTransform != null)
        {
            Vector3 lookTarget = (followTarget != null) ? followTarget.position : boardCenter;
            camTransform.rotation = Quaternion.Slerp(camTransform.rotation,
                Quaternion.LookRotation(lookTarget - camTransform.position, Vector3.up),
                Time.deltaTime * 8f);
        }

        // Toggle camera via Tab (both new input system and old)
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleView();
        }
        else if (Keyboard.current == null && UnityEngine.Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleView();
        }

    }

    public void ToggleView()
    {
        topDown = !topDown;
        CenterImmediate();
    }

    public void CenterImmediate()
    {
        RecalculateBoardCenter();

        if (camTransform == null) return;

        if (topDown)
        {
            Vector3 pos = (followTarget != null) ? followTarget.position + topOffset : boardCenter + topOffset;
            camTransform.position = pos;
            camTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            Vector3 pos = (followTarget != null) ? followTarget.position + isoOffset : boardCenter + isoOffset;
            camTransform.position = pos;
            camTransform.rotation = Quaternion.Euler(45f, 45f, 0f);
        }
        velocity = Vector3.zero;
    }

    void RecalculateBoardCenter()
    {
        if (grid == null) return;
        var tiles = grid.GetAllTiles();
        if (tiles == null || tiles.Count == 0) return;

        Vector3 sum = Vector3.zero;
        foreach (var t in tiles) sum += t.transform.position;
        boardCenter = sum / tiles.Count;
    }
}
