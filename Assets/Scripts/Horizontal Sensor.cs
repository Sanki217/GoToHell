using UnityEngine;
using System;

public class HorizontalSensor : MonoBehaviour
{
    public event Action OnHitLeft;
    public event Action OnHitRight;

    private Transform rootEnemy;

    private void Awake()
    {
        rootEnemy = transform.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore self
        if (other.transform.root == rootEnemy)
            return;

    
        if (other.CompareTag("Spawner"))
            return;
        if (other.CompareTag("Looter"))
            return;

        Vector3 hitPoint = other.ClosestPoint(rootEnemy.position);
        float deltaX = hitPoint.x - rootEnemy.position.x;

        if (Mathf.Abs(deltaX) < 0.01f)
            return;

        if (deltaX > 0f)
            OnHitRight?.Invoke(); // obstacle on right → go left
        else
            OnHitLeft?.Invoke();  // obstacle on left → go right
    }
}
