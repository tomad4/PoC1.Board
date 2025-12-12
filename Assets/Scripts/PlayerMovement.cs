using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public HexTile currentHex;
    public float moveSpeed = 4f;
    public bool hasExtraMove = false; // power-up

    bool moving = false;

    // Wywołaj aby spróbować ruszyć do targetHex
    public void TryMoveTo(HexTile targetHex)
    {
        if (moving) return;
        if (targetHex == null) return;
        if (!targetHex.isWalkable) { Debug.Log("Target not walkable"); return; }

        // compute distance in hex steps (use your hex coordinate system)
        int dist = HexDistance(currentHex, targetHex);

        if (dist == 1)
        {
            StartCoroutine(DoMove(targetHex));
        }
        else if (dist == 2 && hasExtraMove)
        {
            // find intermediate hex (one of neighbors of current that is neighbor of target)
            HexTile mid = FindIntermediate(currentHex, targetHex);
            if (mid == null)
            {
                Debug.Log("No valid intermediate for 2-hop");
                return;
            }
            // sequential hops: current -> mid -> target
            StartCoroutine(DoTwoHop(mid, targetHex));
        }
        else
        {
            Debug.Log("Movement too far or blocked. Dist:" + dist + " Extra:" + hasExtraMove);
        }
    }

    IEnumerator DoMove(HexTile target)
    {
        moving = true;
        HexTile origin = currentHex;

        // simple lerp
        Vector3 start = transform.position;
        Vector3 end = target.transform.position + Vector3.up * 0.5f; // pawn height
        float t = 0f;
        float duration = 1f / moveSpeed;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        transform.position = end;
        currentHex = target;

        // OnExit of origin triggers flip
        origin.OnPawnExit();

        moving = false;
    }

    IEnumerator DoTwoHop(HexTile mid, HexTile target)
    {
        // hop to mid
        yield return DoMoveCoroutine(mid);
        // small delay maybe
        yield return new WaitForSeconds(0.08f);
        // then to target
        yield return DoMoveCoroutine(target);
        // after first hop origin flip already fired for the original origin. 
        // If you want the mid to flip on leaving, it will be flipped when we leave mid (origin for second hop).
    }

    IEnumerator DoMoveCoroutine(HexTile target)
    {
        HexTile origin = currentHex;
        Vector3 start = transform.position;
        Vector3 end = target.transform.position + Vector3.up * 0.5f;
        float t = 0f;
        float duration = 1f / moveSpeed;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }

        transform.position = end;
        currentHex = target;

        // flip the origin on exit
        origin.OnPawnExit();
    }

    // Placeholder: use your hex coordinate system
    int HexDistance(HexTile a, HexTile b)
    {
        // If you have axial coords, use them here; for demo return 1 if neighbor else 2 if next-neighbor
        // For now assume you have HexCoordinate component with q,r values:
        var ca = a.GetComponent<HexCoordinate>();
        var cb = b.GetComponent<HexCoordinate>();
        if (ca == null || cb == null) return 999;
        int dq = Mathf.Abs(ca.q - cb.q);
        int dr = Mathf.Abs(ca.r - cb.r);
        int ds = Mathf.Abs((-ca.q-ca.r) - (-cb.q-cb.r)); // cube coord s
        return Mathf.Max(dq, dr, ds);
    }

    HexTile FindIntermediate(HexTile a, HexTile b)
    {
        // naive: check neighbors of 'a' and find one with distance 1 to b
        var aComp = a.GetComponent<HexNeighbors>();
        if (aComp == null) return null;
        foreach (var n in aComp.neighbors)
        {
            if (n == null) continue;
            if (HexDistance(n, b) == 1 && n.isWalkable) return n;
        }
        return null;
    }
}
