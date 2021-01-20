using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetProp : NetPlayer
{
    public override NetType netType => NetType.Prop;
    public override void UpdateVelocity(Vector3 Velocity, Vector3 netPos)
    {
        if (EnableTeleport && PickedBy == null)
        {
            float Distance = Vector3.Distance(netPos, this.gameObject.transform.position);
            if (Distance >= DistanceToTeleport)
            {
                this.rb.MovePosition(netPos);
                return;
            }
        }
        Velocity.y = rb.velocity.y;
        rb.velocity = Velocity;
    }

    private void Start()
    {
        try { rb = GetComponent<Rigidbody>(); }
        catch { Debug.LogError("NetPiece on " + PieceID + " couldn't be initalized!: Rigidbody not found!"); }

    }

    private void FixedUpdate()
    {
        if(PickedBy == null && rb == null)
        {
            TryGetComponent<Rigidbody>(out rb);
        }
        if (isOwner)
        {
            if (lastSyncedPos != this.transform.position && PickedBy == null)
            {
                NetClient.SendForm(new Movement(PieceID, this.transform.position, rb.velocity));
                lastSyncedPos = this.transform.position;
            }
        }
    }

    public static NetProp InstantiateNetProp(GameObject prefab, int PieceID, Vector3 pos)
    {
        if (FindPiece(PieceID) != null) return (NetProp)FindPiece(PieceID);
        var obj = GameObject.Instantiate(prefab, pos, Quaternion.identity);
        NetProp netProp = obj.GetComponent<NetProp>();
        if (netProp == null) { Debug.LogError("Failed to instantiate piece: Prefab does not have NetPiece attached!"); return null; }
        var charControls = obj.GetComponent<CharControls>();
        netProp.PieceID = PieceID;
        ActivePieces.Add(PieceID, netProp);
        return netProp;
    }

    public void SpawnProp()
    {
        NetProp.InstantiateNetProp(NetClient.netClient.PropPrefab, NetClient.PlayerID * 100 + 2, Vector3.zero);
    }
}
