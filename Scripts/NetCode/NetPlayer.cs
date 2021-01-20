using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetPlayer : MonoBehaviour
{
    public enum NetType { Player = 1, Prop = 2 }
    public int PieceID { get; set; } = 0;
    public int OwnerID { get => PieceID / 100; }
    public NetPlayer PickedBy { get; set; }

    public virtual NetType netType => NetType.Player;

    protected static Dictionary<int, NetPlayer> ActivePieces = new Dictionary<int, NetPlayer>();

    [SerializeField] private bool KeepAlive = false;
    [SerializeField] private float ThreesholdToRemovePiece = 25f;
    private float timeSinceLastUpdate;

    [SerializeField] protected bool EnableTeleport = false;
    [SerializeField] protected float DistanceToTeleport = 25f, SmoothDistance = 0.25f, SyncSpeed = 5;

    protected Rigidbody rb;
    public CharControls charControls = null;
    protected bool isOwner { get => OwnerID == NetClient.PlayerID; }
    protected Vector3 lastSyncedPos = Vector3.zero;

    private void Start()
    {
        try { rb = GetComponent<Rigidbody>(); }
        catch { Debug.LogError("NetPiece on " + PieceID + " couldn't be initalized!: Rigidbody not found!"); }
        this.name = (isOwner) ? "{localPlayer}-" + OwnerID.ToString() : "Player-" + OwnerID.ToString();
        if (isOwner)
            CharControls.LocalPlayerPawn = this.transform;
    }

    public virtual void UpdateVelocity(Vector3 Velocity, Vector3 netPos)
    {
        timeSinceLastUpdate = 0;
        if (NetClient.PlayerID == this.OwnerID)
        {
            charControls.ApplyVelocity(Velocity);
            return;
        }
        float Distance = Vector3.Distance(netPos, this.gameObject.transform.position);
        if (Distance >= SmoothDistance)
            //Debug.Log("Distance " + Distance);
            charControls.ApplyVelocity(Velocity + Vector3.Scale((netPos - this.gameObject.transform.position) * SyncSpeed, new Vector3(1,0,1)));
        else
            charControls.ApplyVelocity(Velocity);
        if (EnableTeleport && Distance >= DistanceToTeleport)
        {
            this.rb.MovePosition(netPos);
        }
        
    }

    private void FixedUpdate()
    {
        if (NetClient.PlayerID == this.OwnerID)
        {
            /*if (charControls.GetVelocity().x != 0 || lastSyncedPos != this.transform.position)
            {
                
            }*/
            NetClient.SendForm(new Movement(PieceID, this.transform.position, charControls.GetVelocity()));
            lastSyncedPos = this.transform.position;
            charControls.ApplyVelocity(charControls.GetVelocity());
            if (this.transform.position.y < charControls.respawnBounder)
            {
                charControls.RespawnPawn();
            }
        }
        else if(!KeepAlive)
        {
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= ThreesholdToRemovePiece)
            {
                ActivePieces.Remove(PieceID);
                Destroy(this.gameObject);
            }
        }
    }

    public static NetPlayer FindPiece(int PieceID)
    {
        try { ActivePieces.TryGetValue(PieceID, out NetPlayer netPiece);
            if (netPiece == null) ActivePieces.Remove(PieceID);
            return netPiece; }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
            return null;
        }
    }

    public static NetPlayer InstantiateNetPlayer (GameObject prefab, int PieceID, Vector3 pos)
    {
        if (ActivePieces.ContainsKey(PieceID)) 
            return FindPiece(PieceID);
        var obj = GameObject.Instantiate(prefab, pos, Quaternion.identity);
        NetPlayer netPiece = obj.GetComponent<NetPlayer>();
        if (netPiece == null) { Debug.LogError("Failed to instantiate piece: Prefab does not have NetPiece attached!"); return null; }
        var charControls = obj.GetComponent<CharControls>();
        netPiece.charControls = charControls;
        netPiece.PieceID = PieceID;
        ActivePieces.Add(PieceID, netPiece);
        return netPiece;
    }
}
