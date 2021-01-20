using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public bool IsFinish = false;
    public int CheckPointID {get;set;}

    private bool IsSended = false; 
    private void OnTriggerEnter(Collider other)
    {
        CharControls @char;
        if((@char = other.GetComponentInParent<CharControls>()) != null)
        {
            if (IsFinish)
            {
                if (NetClient.netClient.IsConnected && !IsSended)
                {
                    NetClient.SendServerCode(22);
                    IsSended = true;
                }
            }
            else if(!IsFinish && @char.LastCheckPoint.Key < CheckPointID)
                @char.LastCheckPoint = new KeyValuePair<int, Vector3> (CheckPointID, GetRandomizedPos());
        }
    }

    private Vector3 GetRandomizedPos()
    {
        Vector3 offset = Random.Range(-5,6) * this.transform.forward;
        return this.transform.position + offset;
    }
}
