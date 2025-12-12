using System.Collections;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Side Objects")]
    public GameObject frontSide;
    public GameObject backSide;

    [Header("Flip Pivot")]
    public Transform hingePivot;

    [Header("State")]
    public bool isFlipped = false;
    public bool isWalkable = true;

    [Header("Flip Settings")]
    public float flipDuration = 0.45f;
    private bool flipping = false;

    void Start()
    {
        UpdateSides();
    }

    void UpdateSides()
    {
        if (frontSide) frontSide.SetActive(!isFlipped);
        if (backSide) backSide.SetActive(isFlipped);
    }

    public void FlipAnimated()
    {
        if (!flipping)
            StartCoroutine(DoFlip());
    }

    IEnumerator DoFlip()
    {
        flipping = true;

        float elapsed = 0f;

        Quaternion startRot = transform.rotation;
        Vector3 pivot = hingePivot.position;
        Vector3 axis = hingePivot.right; // oś obrotu – możesz zmienić na .up lub .forward

        float totalAngle = 180f;

        while (elapsed < flipDuration)
        {
            float t = elapsed / flipDuration;
            float eased = Mathf.SmoothStep(0, 1, t);
            float angle = Mathf.Lerp(0, totalAngle, eased);

            transform.rotation = startRot;
            transform.RotateAround(pivot, axis, angle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = startRot;
        transform.RotateAround(pivot, axis, totalAngle);

        isFlipped = !isFlipped;
        UpdateSides();

        flipping = false;
    }

    // To wywołujemy gdy pionek schodzi z pola
    public void OnPawnExit()
    {
        FlipAnimated();
    }
}
