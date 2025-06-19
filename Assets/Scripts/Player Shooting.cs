using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public int maxArrows = 3;
    private int currentArrows;

    [Header("Stickable Layers")]
    public LayerMask stickableLayers; // ← Platforms + Walls assigned in Inspector

    private Camera mainCam;

    private void Start()
    {
        currentArrows = maxArrows;
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && currentArrows > 0)
        {
            ShootArrow();
        }
    }

    private void ShootArrow()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Ray ray = mainCam.ScreenPointToRay(mouseScreenPos);

        Vector3 mouseWorldPos;
        Plane plane = new Plane(Vector3.forward, Vector3.zero); // Z = 0 plane
        if (plane.Raycast(ray, out float distance))
        {
            mouseWorldPos = ray.GetPoint(distance);
        }
        else
        {
            return; // Fail-safe
        }

        Vector3 shootDir = (mouseWorldPos - shootPoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);
        arrow.GetComponent<Arrow>().Initialize(shootDir, stickableLayers);
        currentArrows--;
    }

    public void RestoreArrow()
    {
        currentArrows = Mathf.Clamp(currentArrows + 1, 0, maxArrows);
    }
}
