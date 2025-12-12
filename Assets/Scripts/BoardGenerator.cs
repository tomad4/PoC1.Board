using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject hingeRodPrefab; // cylinder etc.
    public int rows = 10;
    public int cols = 10;
    public float hexSpacing = 1.0f; // adjust to size

    List<List<HexTile>> grid = new();

    public void Generate()
    {
        grid.Clear();
        for (int r = 0; r < rows; r++)
        {
            List<HexTile> rowList = new();
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = CalculateHexPosition(r, c);
                GameObject go = Instantiate(hexPrefab, pos, Quaternion.identity, transform);
                HexTile ht = go.GetComponent<HexTile>();
                rowList.Add(ht);
            }
            grid.Add(rowList);
        }

        // connect rows with hinge rods (drążki) — example: connect each hex in row r to hex in r+1 at same column
        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                HexTile a = grid[r][c];
                HexTile b = grid[r + 1][c];

                Vector3 mid = (a.transform.position + b.transform.position) / 2f;
                Vector3 dir = b.transform.position - a.transform.position;
                float length = dir.magnitude;

                GameObject rod = Instantiate(hingeRodPrefab, mid, Quaternion.identity, transform);
                // align rod to direction
                rod.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                // scale rod to length (assumes rod's up points along its length)
                Vector3 rodScale = rod.transform.localScale;
                rodScale.y = length * 0.5f; // depends on model orientation
                rod.transform.localScale = rodScale;

                // optional: assign pivot to both hexes to ensure consistent hinge orientation
                // set a.hingePivot and b.hingePivot to points on rod if desired
            }
        }
    }

    Vector3 CalculateHexPosition(int r, int c)
    {
        // axial flat-top layout
        float xOffset = (c + r * 0.5f - Mathf.Floor(r / 2f)) * hexSpacing;
        float zOffset = r * (hexSpacing * 0.8660254f); // sqrt(3)/2 for hex vertical spacing
        return new Vector3(xOffset, 0f, zOffset);
    }
}
