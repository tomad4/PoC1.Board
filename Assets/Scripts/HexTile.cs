using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// HexTile - prosty komponent dla pola heksagonalnego.
/// Zawiera bezpieczne tworzenie instancji materiałów, flip, reset i reakcję na koniec tury.
/// </summary>
public class HexTile : MonoBehaviour, ITurnListener
{
    [TextArea] public string description = "Puste pole";
    public Material defaultMat;
    public Material flippedMat;
    public bool isFlipped = false;
    public int lastVisitedTurn = -1;

    // runtime renderer & instanced materials
    private Renderer rend;
    private Material defaultMatInstance;
    private Material flippedMatInstance;

    void Awake()
    {
        rend = GetComponent<Renderer>();

        // Prepare default instance (priority: explicit defaultMat, else sharedMaterial)
        if (defaultMat != null)
        {
            defaultMatInstance = new Material(defaultMat);
        }
        else if (rend != null && rend.sharedMaterial != null)
        {
            defaultMatInstance = new Material(rend.sharedMaterial);
        }

        // Prepare flipped instance only if user provided flippedMat
        if (flippedMat != null)
        {
            flippedMatInstance = new Material(flippedMat);
        }

        // Assign default instance to renderer so runtime changes are safe
        if (rend != null && defaultMatInstance != null)
        {
            rend.material = defaultMatInstance;
            isFlipped = false;
            lastVisitedTurn = -1;
        }
    }

    // Proste flipowanie: przełącz instancje materiałów
    public void Flip()
    {
        if (rend == null) rend = GetComponent<Renderer>();

        // jeśli nie masz flippedMatInstance, utwórz ją z flippedMat (jeśli podane)
        if (flippedMatInstance == null && flippedMat != null)
            flippedMatInstance = new Material(flippedMat);

        if (!isFlipped)
        {
            if (flippedMatInstance != null)
                rend.material = flippedMatInstance;
            else
                Debug.LogWarning($"{name}: flippedMat nie przypisany.");
            isFlipped = true;
        }
        else
        {
            if (defaultMatInstance != null)
                rend.material = defaultMatInstance;
            isFlipped = false;
        }
    }
    /// <summary>
    /// Mark tile as visited on given turn and optionally apply visual feedback.
    /// </summary>
    public void SetVisited(int turn)
    {
        lastVisitedTurn = turn;

        if (rend == null) rend = GetComponent<Renderer>();
        var mat = rend?.material;
        if (mat != null)
        {
            // Slight visual feedback: tint a bit toward white, or slightly brighten
            Color baseC = mat.color;
            Color target = Color.Lerp(baseC, Color.white, 0.08f); // small brighten
            mat.color = target;
        }
    }

    // Przywraca tile do stanu początkowego (używane przy ResetGame)
    public void ResetTile()
    {
        isFlipped = false;
        lastVisitedTurn = -1;

        if (rend == null) rend = GetComponent<Renderer>();

        if (defaultMatInstance == null)
        {
            if (defaultMat != null) defaultMatInstance = new Material(defaultMat);
            else if (rend != null && rend.sharedMaterial != null) defaultMatInstance = new Material(rend.sharedMaterial);
        }

        if (rend != null && defaultMatInstance != null)
        {
            rend.material = defaultMatInstance;
            transform.localRotation = Quaternion.identity;
        }
    }

    public string GetDescription() => description;

    // Prosta reakcja na zakończenie tury - delikatne przyciemnienie instancyjnego materiału
    public void OnTurnEnd(int globalTurn)
    {
        // debug
        // Debug.Log($"HexTile.OnTurnEnd {name} turn={globalTurn}");

        if (rend == null) rend = GetComponent<Renderer>();
        var mat = rend?.material;
        if (mat != null)
        {
            Color c = mat.color;
            c *= 0.99f; // delikatne przyciemnienie
            mat.color = c;
        }
    }
}
