using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public sealed class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [Min(1)] public int maxTurns = 5;
    public int currentTurn = 0;

    [Header("UI (TMP)")]
    public TMP_Text turnText;
    public TMP_Text tileDescriptionText;
    public GameObject endGamePanel;
    public Button endTurnButton;

    [Header("References")]
    public HexGridGenerator grid;
    public PlayerPawn player;

    // internal listeners
    private readonly List<ITurnListener> listeners = new();

    private void Start()
    {
        Debug.Log("TurnManager.Start()");

        // Auto-discovery (optional, safe)
        if (grid == null)
        {
            grid = FindAnyObjectByType<HexGridGenerator>();
            if (grid != null) Debug.Log("TurnManager: Auto-found Grid.");
        }

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerPawn>();
            if (player != null) Debug.Log("TurnManager: Auto-found PlayerPawn.");
        }

        // UI init
        UpdateUI();
        if (endGamePanel != null) endGamePanel.SetActive(false);

        // Safety checks
        if (grid == null) Debug.LogWarning("TurnManager: grid is NULL. Assign Grid in Inspector.");
        if (player == null) Debug.LogWarning("TurnManager: player is NULL. Assign Player in Inspector.");

        // Register listeners
        RegisterAllListeners();
        // ensure player has reference back to this TurnManager
        if (player != null)
        {
            player.turnManager = this;
            Debug.Log("TurnManager: assigned self to player.turnManager");
        }
        else
        {
            Debug.LogWarning("TurnManager: player is null during Start(); cannot assign turnManager to player.");
        }

        // ===== Auto-place player on first tile =====
        if (grid != null && player != null)
        {
            var allTiles = grid.GetAllTiles();

            if (allTiles != null && allTiles.Count > 0)
            {
                Debug.Log("TurnManager: auto-placing player on tile " + allTiles[0].name);
                player.ForcePlaceOnTile(allTiles[0], currentTurn);
            }
            else
            {
                Debug.LogWarning("TurnManager: No tiles found to place player on.");
            }
        }

        Debug.Log("TurnManager: listeners count = " + listeners.Count);
    }

    /// <summary>Finds all MonoBehaviours that implement ITurnListener.</summary>
    public void RegisterAllListeners()
    {
        listeners.Clear();

        var allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (var mb in allObjects)
        {
            if (mb is ITurnListener listener)
                listeners.Add(listener);
        }
    }

    /// <summary>Called from UI button or code to advance the turn.</summary>
    public void EndTurn()
    {
        if (currentTurn >= maxTurns) return;
        currentTurn++;
        Debug.Log($"EndTurn -> {currentTurn} (notifying {listeners.Count} listeners)");
        foreach (var l in listeners)
        {
            // bezpieczne wywołanie z try/catch na wypadek problemów w listenerze
            try { l.OnTurnEnd(currentTurn); }
            catch (System.Exception e) { Debug.LogError($"Listener threw in OnTurnEnd: {e}"); }
        }
        UpdateUI();
        if (currentTurn >= maxTurns) OnEndGame();
    }

    private void UpdateUI()
    {
        if (turnText != null)
            turnText.text = $"Turn: {currentTurn}/{maxTurns}";
    }

    private void OnEndGame()
    {
        Debug.Log("OnEndGame()");
        if (endGamePanel != null) endGamePanel.SetActive(true);
        if (endTurnButton != null) endTurnButton.interactable = false;
    }

    public void RequestMoveTo(HexTile tile)
    {
        if (tile == null)
        {
            Debug.LogWarning("RequestMoveTo called with null tile");
            return;
        }

        // If game ended or we've exhausted turns, ignore
        if (currentTurn >= maxTurns)
        {
            Debug.Log("RequestMoveTo ignored: max turns reached or game ended");
            return;
        }

        // If no player assigned — nothing to do
        if (player == null)
        {
            Debug.LogWarning("RequestMoveTo: player is null");
            return;
        }

        // If player is in the middle of moving — ignore clicks
        if (player.IsMoving)
        {
            Debug.Log("RequestMoveTo ignored: player is moving");
            return;
        }

        Debug.Log("RequestMoveTo: " + tile.name);

        // Perform move
        player.TeleportTo(tile, currentTurn);

        // Immediately consume the turn after requesting move (one move == one turn)
        // EndTurn();

        // Update tile description
        if (tileDescriptionText != null)
            tileDescriptionText.text = tile.GetDescription();
    }
    
    public void NotifyMoveComplete()
    {
        // jeśli tura już osiągnęła max, zignoruj
        if (currentTurn >= maxTurns) return;
        // zakończ turę
        EndTurn();
    }
    
    public void ResetGame()
    {
        Debug.Log("TurnManager: ResetGame()");

        // 1) Reset turn counter
        currentTurn = 0;
        UpdateUI();

        // 2) Hide endgame panel and re-enable endTurnButton
        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (endTurnButton != null) endTurnButton.interactable = true;

        // 3) Reset all tiles
        if (grid != null)
        {
            var tiles = grid.GetAllTiles();
            if (tiles != null)
            {
                foreach (var t in tiles)
                {
                    if (t != null) t.ResetTile();
                }
            }
        }

        // 4) Re-register listeners (clean slate)
        RegisterAllListeners();

        // 5) Place/Reset player to starting tile (first tile if exists)
        if (player != null)
        {
            HexTile start = null;
            if (grid != null)
            {
                var tiles = grid.GetAllTiles();
                if (tiles != null && tiles.Count > 0) start = tiles[0];
            }

            player.ResetState(start, currentTurn);
        }
    }
    void Update()
    {
        // Obsługa skrótów (Reset) przez nowy Input System (bez wyjątku)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Debug.Log("TurnManager: R pressed -> ResetGame()");
                ResetGame();
            }
        }
        else
        {
            // Fallback - tylko jeśli projekt używa starego InputManager (rzadko)
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("TurnManager: R pressed (fallback) -> ResetGame()");
                ResetGame();
            }
        }
    }

}
