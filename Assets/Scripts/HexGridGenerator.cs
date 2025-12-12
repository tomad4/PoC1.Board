using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prostokątny generator siatki "offset" dla PoC.
/// Uwaga: to nie jest axial grid — ale wystarcza do PoC.
/// </summary>
public class HexGridGenerator : MonoBehaviour
{
    [Header("Grid")]
    public GameObject hexPrefab;
    public int width = 7;
    public int height = 7;
    public float hexRadius = 1f;

    // collected tile refs
    private List<HexTile> tiles = new List<HexTile>();

    void Start()
    {
        GenerateGrid();
    }

    public void GenerateGrid()
    {
        ClearGrid();

        if (hexPrefab == null)
        {
            Debug.LogError("HexGridGenerator.GenerateGrid: hexPrefab is null! Assign prefab in inspector.");
            return;
        }

        float xOffset = hexRadius * 1.5f;
        float zOffset = hexRadius * Mathf.Sqrt(3f);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                float xPos = x * xOffset;
                // offset every second column (odd-r)
                if (z % 2 == 1) xPos += hexRadius * 0.75f;
                float zPos = z * (zOffset * 0.5f);

                Vector3 pos = new Vector3(xPos, 0f, zPos);
                GameObject go = Instantiate(hexPrefab, pos, Quaternion.identity, transform);
                go.name = $"Hex_{x}_{z}";

                // ensure renderer & collider exist
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                {
                    Debug.LogWarning($"HexGridGenerator: prefab {hexPrefab.name} has no Renderer on instance {go.name}");
                }

                var col = go.GetComponent<Collider>();
                if (col == null)
                {
                    // add a BoxCollider if none exists (safe default)
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = Vector3.zero;
                    bc.size = new Vector3(1f, 0.2f, 1f);
                }
                else
                {
                    if (col is BoxCollider bc)
                    {
                        bc.center = Vector3.zero;
                        bc.size = new Vector3(1f, 0.2f, 1f);
                    }
                }

                // ensure HexTile component exists and set defaultMat if missing
                HexTile tile = go.GetComponent<HexTile>();
                if (tile == null) tile = go.AddComponent<HexTile>();

                // If tile.defaultMat not set, try to take material from renderer
                if (tile.defaultMat == null && renderer != null && renderer.sharedMaterial != null)
                {
                    tile.defaultMat = renderer.sharedMaterial;
                }

                // IMPORTANT: ensure each instance uses an instanced material (avoid editing sharedMaterial)
                if (renderer != null)
                {
                    // assign a new instance of the material so changes won't affect the asset
                    renderer.material = new Material(renderer.sharedMaterial);
                }

                tiles.Add(tile);
            }
        }

        // Center grid around origin
        float centerX = (width - 1) * xOffset / 2f;
        float centerZ = (height - 1) * (zOffset * 0.5f) / 2f;
        transform.position = new Vector3(-centerX, 0f, -centerZ);
    }

    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        tiles.Clear();
    }

    public List<HexTile> GetAllTiles() => tiles;
}
