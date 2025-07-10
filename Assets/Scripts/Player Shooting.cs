using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public Transform shootPoint;
    public int maxArrows = 3;
    private int currentArrows;
    public GameObject[] arrowDots; // Assign 3 dots in Inspector

    [Header("Stickable Layers")]
    public LayerMask stickableLayers; // ← Platforms + Walls assigned in Inspector

    [Header("Aiming Line")]
    public LineRenderer lineRenderer;
    public float aimLineLength = 20f;


    private Camera mainCam;
    private CameraFollow cam;

    private void Start()
    {
        currentArrows = maxArrows;
        mainCam = Camera.main;
        cam = Camera.main.GetComponent<CameraFollow>();
        UpdateArrowUI();


        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.positionCount = 2;
        lineRenderer.enabled = true;
    }

    private void Update()
    {
        UpdateAimingLine();

        if (Input.GetMouseButtonDown(0) && currentArrows > 0)
        {
            ShootArrow();
         
            cam.Shake(0.1f, 0.05f); // small shake
            UpdateArrowUI();
        }
    }
    private void UpdateArrowUI()
    {
        for (int i = 0; i < arrowDots.Length; i++)
        {
            arrowDots[i].SetActive(i < currentArrows);
        }
    }
    private void UpdateAimingLine()
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

        lineRenderer.SetPosition(0, shootPoint.position);
        lineRenderer.SetPosition(1, shootPoint.position + shootDir * aimLineLength);
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
        UpdateArrowUI();
    }
}
