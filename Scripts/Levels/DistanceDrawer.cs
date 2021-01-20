using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceDrawer : MonoBehaviour
{
    [SerializeField] private int DistanceToDraw = 10;
    [SerializeField] private MeshRenderer ConnectedMeshRender = null;

    private void Awake()
    {
        if(ConnectedMeshRender == null)
        {
            Debug.Log("Mesh renderer was not setted up! Drawer is shutting down!");
            this.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (CharControls.LocalPlayerPawn == null) return;
        float CurDistance = Vector3.Distance(this.transform.position, CharControls.LocalPlayerPawn.position);
        if (ConnectedMeshRender.enabled && CurDistance > DistanceToDraw)
            ConnectedMeshRender.enabled = false;
        else if(!ConnectedMeshRender.enabled && CurDistance < DistanceToDraw)
            ConnectedMeshRender.enabled = true;
    }
}
