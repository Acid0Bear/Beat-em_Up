using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grab : MonoBehaviour
{
    [SerializeField] Transform GrabPoint = null;

    [SerializeField] private LayerMask AccesibleLayers = new LayerMask();
    [SerializeField] private static float breakForce = 10000, breakTorque = 10000;

    private Collider[] ObjectsInCloseProximity;
    [HideInInspector] public bool isGrabbing = false;
    private SpringJoint springJoint;
    public NetPlayer GrabbedObj;

    private void FixedUpdate()
    {
        if((springJoint != null && springJoint.connectedBody == null) || (springJoint == null && GrabbedObj != null))
        {
            NetClient.SendForm(new GrabForm(GetComponent<NetPlayer>().PieceID, GrabbedObj.PieceID, 0));
        } 
    }

    public void TryGrabObj(int id)
    {
        if(id == 0)
            ObjectsInCloseProximity = Physics.OverlapBox(GrabPoint.position, new Vector3(0.4f,1,0.6f),Quaternion.identity, AccesibleLayers);
        if(ObjectsInCloseProximity.Length > id && ObjectsInCloseProximity.Length != 0)
        {
            if (ObjectsInCloseProximity[id].tag != "Prop" && ObjectsInCloseProximity[id].attachedRigidbody == GetComponent<Rigidbody>())
            {
                TryGrabObj(id + 1);
                return;
            }
            if (!isGrabbing)
            {
                if(ObjectsInCloseProximity[id].gameObject.tag != "Player")
                {
                    //GrabAndAssign(ObjectsInCloseProximity[id], this.gameObject);
                    NetClient.SendForm(new GrabForm(this.gameObject.GetComponent<NetPlayer>().PieceID, ObjectsInCloseProximity[id].GetComponentInParent<NetProp>().PieceID, 1));
                }
                else
                {
                    NetClient.SendForm(new GrabForm(this.gameObject.GetComponent<NetPlayer>().PieceID, ObjectsInCloseProximity[id].GetComponentInParent<NetPlayer>().PieceID, 1));
                }
            }
            else
            {
                NetClient.SendForm(new GrabForm(GetComponent<NetPlayer>().PieceID, ObjectsInCloseProximity[id].GetComponentInParent<NetPlayer>().PieceID, 0));
                //Destroy(fixedJoint);
            }
        }
    }

    public static void GrabAndAssign(Collider obj, NetPlayer Player)
    {
        var NetPiece = obj.GetComponentInParent<NetPlayer>();
        var Grab = Player.GetComponent<Grab>();
        NetPiece.PickedBy = Player;
        Grab.GrabbedObj = NetPiece;
        if (NetPiece is NetProp)
        {
            
            Destroy(obj.attachedRigidbody);
            obj.transform.parent.position = Grab.GrabPoint.position;
            obj.transform.parent.SetParent(Grab.GrabPoint);
        }
        else
        {
            Grab.springJoint = Grab.gameObject.AddComponent<SpringJoint>();
            Grab.springJoint.autoConfigureConnectedAnchor = false;
            Grab.springJoint.connectedAnchor = Grab.GrabPoint.localPosition;
            Grab.springJoint.damper = 100000;
            Grab.springJoint.spring = 100000;
            Grab.springJoint.minDistance = 0.8f;
            Grab.springJoint.enableCollision = true;
            Grab.springJoint.breakForce = breakForce;
            Grab.springJoint.breakTorque = breakTorque;
            Grab.springJoint.connectedBody = obj.attachedRigidbody;
        }
        Grab.isGrabbing = true;
        Grab.gameObject.GetComponent<CharControls>().PlayerAnimator.SetBool("IsCarrying", true);
    }

    public static void ReleaseObj(GameObject obj, NetPlayer Player)
    {
        var NetPiece = obj.GetComponent<NetPlayer>();
        var Grab = Player.GetComponent<Grab>();
        NetPiece.PickedBy = null;
        if (NetPiece is NetProp)
        {
            obj.transform.SetParent(null);
            var rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1;
            rb.drag = 1;
            rb.angularDrag = 1;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        else
        {
            if(Grab.springJoint != null)
                Destroy(Grab.springJoint);
        }
        Grab.GrabbedObj = null;
        Grab.isGrabbing = false;
        Grab.gameObject.GetComponent<CharControls>().PlayerAnimator.SetBool("IsCarrying", false);
    }
}
