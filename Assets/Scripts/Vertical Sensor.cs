using UnityEngine;
using System;

public class VerticalSensor : MonoBehaviour
{
    public event Action OnHitCeiling;
    public event Action OnHitGround;

    private Transform rootEnemy;

    private void Awake()
    {
        rootEnemy = transform.root;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root == rootEnemy) return; // ignore self

        // You can refine detection tags, but this works for everything
        //if (other.CompareTag("Wall"))
      //  {
     //       OnHitCeiling?.Invoke();
   //     }
  //      else if (other.CompareTag("Ground"))
 //       {
// //           OnHitGround?.Invoke();
//        else
  //      {
            // Generic bounce logic:
            Vector3 localPos = transform.InverseTransformPoint(other.ClosestPoint(transform.position));

            if (localPos.y > 0)
                OnHitCeiling?.Invoke();
            else
                OnHitGround?.Invoke();
 //       }
    }
}
