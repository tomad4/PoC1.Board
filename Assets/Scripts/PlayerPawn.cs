using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class PlayerPawn : MonoBehaviour, ITurnListener
{
    public HexTile currentTile;
    public float moveSpeed = 4f; // units per second
    public TurnManager turnManager;
    Renderer rend;
    Color baseColor = Color.white;
    int lastColorChangeTurn = 0;

    // informacja czy aktualnie się poruszamy
    public bool IsMoving { get; private set; } = false;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend != null) baseColor = rend.material.color;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }
    public void ResetState(HexTile startTile, int currentTurn = 0)
    {
        StopAllCoroutines();
        // Reset color
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = baseColor;

        // Reset movement flag
        IsMoving = false;

        // Place on start tile (if provided)
        if (startTile != null)
        {
            currentTile = startTile;
            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            transform.position = startTile.transform.position + Vector3.up * 0.6f;
            startTile.SetVisited(currentTurn);
        }
    }

    public void ForcePlaceOnTile(HexTile tile, int currentTurn)
    {
        if (tile == null) return;
        StopAllCoroutines();
        currentTile = tile;
        Vector3 target = GetTileTargetPosition(tile);
        Debug.Log($"ForcePlaceOnTile -> from {transform.position} to {target} on tile {tile.name}");
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        transform.position = target;
        tile.SetVisited(currentTurn);
    }

    public void TeleportTo(HexTile tile, int currentTurn)
    {
        if (tile == null)
        {
            Debug.LogWarning("TeleportTo called with null tile");
            return;
        }

        // jeśli już się poruszamy — ignoruj
        if (IsMoving)
        {
            Debug.Log("TeleportTo ignored: Player is already moving");
            return;
        }

        StopAllCoroutines();
        currentTile = tile;
        Vector3 target = GetTileTargetPosition(tile);
        Debug.Log($"TeleportTo requested -> from {transform.position} to {target} (tile: {tile.name})");

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Important: set IsMoving BEFORE starting coroutine, to prevent re-entry
        IsMoving = true;
        StartCoroutine(MoveTo(target, tile, currentTurn));
    }


    Vector3 GetTileTargetPosition(HexTile tile)
    {
        return tile.transform.position + Vector3.up * 0.6f;
    }

    IEnumerator MoveTo(Vector3 target, HexTile tile, int currentTurn)
    {
        Debug.Log($"MoveTo START -> from {transform.position} to {target}");
        Vector3 start = transform.position;
        float distance = Vector3.Distance(start, target);
        float duration = Mathf.Max(0.05f, distance / moveSpeed);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        transform.position = target;
        tile.SetVisited(currentTurn);

        IsMoving = false;
        Debug.Log("MoveTo COMPLETE -> arrived at " + tile.name);

        // powiadom TurnManager, że ruch się zakończył
        if (turnManager != null)
        {
            Debug.Log("PlayerPawn: notifying TurnManager about move complete");
            turnManager.NotifyMoveComplete();
        }
        else
        {
            Debug.LogWarning("PlayerPawn: turnManager is null on move complete");
        }
    }

    public void OnTurnEnd(int globalTurn)
    {
        Debug.Log($"PlayerPawn.OnTurnEnd turn={globalTurn}");
        lastColorChangeTurn = globalTurn;
        StopCoroutine(nameof(FadeColorRoutine));
        StartCoroutine(FadeColorRoutine(globalTurn));
    }
    IEnumerator FadeColorRoutine(int turn)
    {
        float duration = 0.6f;
        float elapsed = 0f;
        // używamy instancyjnego materialu, żeby nie modyfikować assetu
        var mat = rend.material;
        Color from = mat.color;
        float factor = Mathf.Clamp01(1f - (turn - 1) * 0.15f);
        Color to = baseColor * factor;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mat.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        mat.color = to;
    }
}
