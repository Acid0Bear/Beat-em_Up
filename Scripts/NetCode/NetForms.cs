using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public enum Forms { Movement = 1, Grab = 2 };

[System.Serializable]
public class Vector3S
{
    public float x, y, z;
    public Vector3 Get => new Vector3(this.x, this.y, this.z);
    public Vector3S(Vector3 vec)
    {
        this.x = vec.x;
        this.y = vec.y;
        this.z = vec.z;
    }
}

/*[XmlInclude(typeof(Movement))]
[XmlInclude(typeof(GrabForm))]*/
[System.Serializable]
public class NetForms
{
    public Forms FormID;
    public int PieceID;

    public NetForms()
    {
    }

    public static void ProceedForm(NetForms rawForm)
    {
        switch (rawForm.FormID)
        {
            case Forms.Movement:
                Movement movForm = (Movement)rawForm;
                if (movForm.PieceID >= (movForm.PieceID / 100) * 100 + 1)
                {
                    NetPlayer target = NetPlayer.InstantiateNetPlayer(NetClient.netClient.PlayerPrefab, movForm.PieceID, movForm.Pos.Get);
                    if (target == null) { 
                        Debug.LogError("Something went wrong when trying to apply movement to " + movForm.PieceID); 
                        break; }
                    //Debug.Log("Updating player with id : " + movForm.PieceID);
                    target.UpdateVelocity(movForm.Vel.Get, movForm.Pos.Get);
                }
                else
                {
                    NetProp target = NetProp.InstantiateNetProp(NetClient.netClient.PropPrefab, movForm.PieceID, movForm.Pos.Get);
                    if (target == null || target.PickedBy != null) break;
                    //Debug.Log("Updating prop with id : " + movForm.PieceID);
                    target.UpdateVelocity(movForm.Vel.Get, movForm.Pos.Get);
                }
                break;
            case Forms.Grab:
                GrabForm grabForm = (GrabForm)rawForm;
                NetPlayer netPlayer; 
                NetPlayer netTarget;
                if ((netTarget = NetPlayer.FindPiece(grabForm.TargetID)) != null && (netPlayer = NetPlayer.FindPiece(grabForm.PieceID)) != null)
                {
                    try
                    {
                        if (grabForm.State == 1)
                            Grab.GrabAndAssign(netTarget.GetComponentInChildren<Collider>(), netPlayer);
                        else
                            Grab.ReleaseObj(netTarget.gameObject, netPlayer);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                    }
                }
                break;
        }
    }

    public static Type[] GetInheritedTypes()
    {
        return new [] {typeof(Movement), typeof(GrabForm)};
    }
}
[System.Serializable]
public class Movement : NetForms
{
    //public float x, z;
    public Vector3S Pos, Vel;

    public Movement()
    {
        this.FormID = Forms.Movement;
        this.PieceID = 0;
        //this.x = 0;
        //this.z = 0;
        this.Pos = new Vector3S(Vector3.zero);
        this.Vel = new Vector3S(Vector3.zero);
    }
    public Movement(int PieceID,Vector3 newPos, Vector3 Velocity)
    {
        this.FormID = Forms.Movement;
        this.PieceID = PieceID;
        //this.x = xAxis;
        //this.z = zAxis;
        this.Pos = new Vector3S(newPos);
        this.Vel = new Vector3S(Velocity);
    }
}

[System.Serializable]
public class GrabForm : NetForms
{
    public int TargetID, State;
    public GrabForm()
    {
        this.FormID = Forms.Grab;
        this.PieceID = 0;
        this.TargetID = 0;
        this.State = 0;
    }

    public GrabForm(int PieceID, int TargetID, int State)
    {
        this.FormID = Forms.Grab;
        this.PieceID = PieceID;
        this.TargetID = TargetID;
        this.State = State;
    }
}
