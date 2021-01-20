using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionMarker : MonoBehaviour
{
    [SerializeField] private CharControls charControls = null;
    [SerializeField] private string colliderTag => this.gameObject.name;
    [Tooltip("Minimal impact to turn into ragdoll")]
    [SerializeField] private float minImpact = 7;
    [SerializeField] private float maxImpactMagnitude = 100;
    [SerializeField] private float SupressImpact = 100;


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.gameObject.layer == LayerMask.NameToLayer("Ground")) return;
        //Debug.Log(collision.gameObject.name);
        Debug.Log(collision.gameObject + "-" + charControls.name);
        Vector3 Impact = collision.impulse;
        if (charControls != null && Impact.magnitude >= minImpact)
        {
            if(Impact.magnitude >= maxImpactMagnitude)
            {
                Impact = new Vector3(Mathf.Max(Impact.x, maxImpactMagnitude), Mathf.Max(Impact.y, maxImpactMagnitude), Mathf.Max(Impact.z, maxImpactMagnitude));
            }
            charControls.ApplyImpact(colliderTag, Impact * SupressImpact);
        }
        else if(charControls == null)
            Debug.LogError("charControls not setted up in " + colliderTag);
    }
}
