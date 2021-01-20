using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class CharControls : MonoBehaviour
{
    [Header("Properties")]
    public float Speed = 5f;
    public float Gravity = -9.81f, GroundDistance = 0.4f, LookRotationSpeed = 200, SlopeMult = 25, JumpPower = 5;
    public int respawnBounder = -5;
    [SerializeField] private Transform GroundingPoint = null;
    [SerializeField] private LayerMask GroundLayer = new LayerMask();
    [SerializeField] private Collider[] mecColliders = new Collider[9];

    [Header("Animation")]
    public Animator PlayerAnimator = null;
    [SerializeField] private float AnimTransition = 25;
    [SerializeField] private GameObject ragDoll = null, mechanimDoll = null;
    public GameObject RagDoll => this.ragDoll;

    public static Transform LocalPlayerPawn { get; set; }

    public KeyValuePair<int,Vector3> LastCheckPoint { get; set; }
    public bool IsGrounded { get; private set; }
    public bool IsKnockedDown { get; private set; }

    private List<Collider> GroundCollidersInCloseProximity = new List<Collider>();
    float DownForce, shouldJump, t;
    int prevDir = 0;
    bool isJumping = false;
    Vector3 SyncedVelocity;
    private Rigidbody rb = null;
    private Grab connectedGrab = null;
    private Coroutine ragDollTimeout = null;
    private SkinnedMeshRenderer[] mecSkinnedMeshes;
    private MeshRenderer[] mecMeshRenderers;

    private void Awake()
    {
        mecSkinnedMeshes = mechanimDoll.GetComponentsInChildren<SkinnedMeshRenderer>();
        mecMeshRenderers = mechanimDoll.GetComponentsInChildren<MeshRenderer>();
        IsKnockedDown = false;
        rb = GetComponent<Rigidbody>();
        connectedGrab = GetComponent<Grab>();
        NetPlayer netPiece = null;
        if((netPiece = GetComponent<NetPlayer>()) != null)
        {
            if (netPiece.OwnerID != NetClient.PlayerID)
                this.enabled = true;
        }
    }

    private void ApplyDownForce()
    {
        GroundCollidersInCloseProximity.Clear();
        GroundCollidersInCloseProximity = new List<Collider>(Physics.OverlapSphere(GroundingPoint.position, GroundDistance, GroundLayer));
        IsGrounded = GroundCollidersInCloseProximity.Count != 0;
        PlayerAnimator.SetBool("IsGrounded", IsGrounded);
        DownForce =(IsGrounded)? Jump() : DownForce + Gravity * Time.fixedDeltaTime;
        if (IsGrounded)
        {
            if (GroundCollidersInCloseProximity.Find(Collider => Collider.transform.gameObject.tag == "Ramp"))
                DownForce += (SlopeMult * Time.fixedDeltaTime);
            else if(DownForce < 0)
                DownForce = 0;
        }
        SyncedVelocity.y = DownForce;
        rb.velocity = SyncedVelocity;
    }

    public Vector3 GetVelocity()
    {
        Vector3 direction = Vector3.zero;
        if (IsKnockedDown) return direction;
        float x = (!connectedGrab.isGrabbing) ? Input.GetAxis("Horizontal") : 0, z = Input.GetAxis("Vertical");
        if(x != 0|| z != 0)
        {
            direction = transform.forward * z + transform.right * x;
            direction.y = shouldJump;
        }
        else
        {
            direction = rb.velocity;
            direction.x += (direction.x < 0) ? 0.01f : -0.01f;
            direction.z += (direction.z < 0) ? 0.01f : -0.01f;
            direction.y = shouldJump;
            if (Mathf.Abs(direction.x) < 0.1f)
                direction.x = 0;
            if (Mathf.Abs(direction.z) < 0.1f)
                direction.z = 0;
            direction /= Speed;
        }
        
        return direction;
    }

    public void ApplyVelocity(Vector3 Velocity)
    {
        int Dir = (Vector3.Dot(Velocity, this.transform.forward) > -0.5f) ? 1 : -1;
        if(Dir != prevDir)
        {
            t = 0;
            prevDir = Dir;
        }
        t += Time.deltaTime;
        if (Velocity.y != 0 && IsGrounded)
        {
            if(!isJumping)
                isJumping = true;
            shouldJump = 0;
        }
        SyncedVelocity = Velocity * Speed;
        SyncedVelocity.y = rb.velocity.y;
        if (Velocity.x != 0 || Velocity.z != 0)
        {
            if (Dir == 1 && (!(connectedGrab.GrabbedObj is NetPlayer) || !connectedGrab.isGrabbing))
            {
                Vector3 difference = Velocity + this.transform.forward - this.transform.forward;
                float rotationZ = Mathf.Atan2(difference.x, difference.z) * Mathf.Rad2Deg;
                Quaternion newRot = Quaternion.Euler(0, rotationZ, 0);
                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, newRot, LookRotationSpeed * Time.fixedDeltaTime));
            }
            else
            {
                if(connectedGrab.GrabbedObj != null)
                transform.LookAt(connectedGrab.GrabbedObj.transform);
            }
            if(!connectedGrab.isGrabbing && (SyncedVelocity - rb.velocity).magnitude > 0.5f)
            rb.velocity = SyncedVelocity;
            PlayerAnimator.SetFloat("RunninSpeed", Mathf.Lerp(PlayerAnimator.GetFloat("RunninSpeed"), Mathf.Clamp(Velocity.magnitude * Dir, -1, 1), AnimTransition * t));
        }
        else
        {
            if (connectedGrab.GrabbedObj is NetPlayer)
            {
                transform.LookAt(connectedGrab.GrabbedObj.transform);
            }
            PlayerAnimator.SetFloat("RunninSpeed", Mathf.Lerp(PlayerAnimator.GetFloat("RunninSpeed"), 0, AnimTransition * t));
        }
    }

    public void RespawnPawn()
    {
        DownForce = 0;
        rb.MovePosition(LastCheckPoint.Value + Vector3.up);
    }

    private float Jump()
    {
        if (isJumping)
        {
            isJumping = false;
            return JumpPower;
        }
        else
            return SyncedVelocity.y;
    }

    public void TryJump()
    {
        shouldJump = 1;
    }

    private void FixedUpdate()
    {
        ApplyDownForce();
    }

    public void ApplyImpact(string ColliderTag, Vector3 impact)
    {
        IsKnockedDown = true;
        if(ragDollTimeout != null)
        {
            StopCoroutine(ragDollTimeout);
            ragDollTimeout = StartCoroutine(RagDollToMechanim());
        }
        else
        {
            mechanimDoll.SetActive(false);
            ragDoll.SetActive(true);
            rb.isKinematic = true;

            var ragColliders = ragDoll.GetComponentsInChildren<Collider>();
            for (int i = 0; i < ragColliders.Length; i++)
            {
                if (ragColliders[i].gameObject.name == ColliderTag)
                {
                    ragColliders[i].attachedRigidbody.AddForce(impact, ForceMode.Impulse);
                }
            }
            ragDollTimeout = StartCoroutine(RagDollToMechanim());
        }
    }

    private void CopyTransform(Transform target, Transform copyFrom) {
            target.position = copyFrom.position;
            target.rotation = copyFrom.rotation;
    }

    private void ResetDoll(Collider []mecColliders, Collider[] ragColliders)
    {
        for (int i = 0; i < ragColliders.Length; i++)
        {
            ragColliders[i].attachedRigidbody.isKinematic = true;
            ragColliders[i].attachedRigidbody.inertiaTensor = Vector3.one;
            ragColliders[i].attachedRigidbody.velocity = Vector3.zero;
            ragColliders[i].attachedRigidbody.angularVelocity = Vector3.zero;
            ragColliders[i].attachedRigidbody.ResetCenterOfMass();
            CopyTransform(ragColliders[i].transform, mecColliders[i].transform);
            ragColliders[i].attachedRigidbody.isKinematic = false;
        }
    }

    public static void AddRotationCurveToClip(ref AnimationClip clip, Transform target,string path)
    {
        AnimationCurve curve;
        Keyframe[] keys = new Keyframe[1];
        keys[0] = new Keyframe(0.0f, target.localEulerAngles.x);
        curve = new AnimationCurve(keys);
        curve.SmoothTangents(0, 10);
        clip.SetCurve(path, typeof(Transform), "localEulerAngles.x", curve);
        keys[0] = new Keyframe(0.0f, target.localEulerAngles.y);
        curve = new AnimationCurve(keys);
        curve.SmoothTangents(0, 10);
        clip.SetCurve(path, typeof(Transform), "localEulerAngles.y", curve);
        keys[0] = new Keyframe(0f, target.localEulerAngles.z);
        curve = new AnimationCurve(keys);
        curve.SmoothTangents(0, 10);
        clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", curve);
    }

    private IEnumerator RagDollToMechanim()
    {
        var ragColliders = ragDoll.GetComponentsInChildren<Collider>();
        float timeout = 0;
        while (true)
        {
            timeout += Time.deltaTime;
            if(ragColliders[0].attachedRigidbody.IsSleeping() || timeout >= 3f)
            {
                //Exit from ragdoll
                break;
            }
            yield return null;
        }

        //Rotate towards doll
        var temp = ragColliders[0].transform.parent;
        ragColliders[0].transform.parent = null;
        Vector3 difference = ragColliders[6].transform.position - this.transform.position;
        float rotationZ = Mathf.Atan2(difference.x, difference.z) * Mathf.Rad2Deg;
        //if (difference.x < 0) rotationZ = -rotationZ;
        this.transform.localRotation = Quaternion.Euler(0f, rotationZ, 0);
        this.transform.position = ragColliders[0].transform.position;
        ragColliders[0].transform.parent = temp;

        //Creating clip
        AnimationClip clip = new AnimationClip();
        clip.legacy = false;
        clip.name = "StandUpBlend";
        string path = "";
        for (int i = 0; i < ragColliders.Length; i++)
        {
            path = "";
            Transform tmp = ragColliders[i].transform;
            while (tmp == null || tmp.gameObject.name != "Armature")
            {
                path = "/" + tmp.gameObject.name + path;
                tmp = tmp.parent;
            }
            path = "Armature" + path;
            AddRotationCurveToClip(ref clip, ragColliders[i].transform, path);
        }
        //UnityEditor.AssetDatabase.CreateAsset(clip, "Assets/MyAnim.anim");
        //UnityEditor.AssetDatabase.SaveAssets();
        //Applying new clip
        AnimatorOverrideController animatorOverride = new AnimatorOverrideController(PlayerAnimator.runtimeAnimatorController);
        animatorOverride.name = "kusokSala";
        animatorOverride["StandUpBlend"] = clip;
        PlayerAnimator.runtimeAnimatorController = animatorOverride;
        PlayerAnimator.SetTrigger("StandUp");

        //Reset doll
        ResetDoll(mecColliders, ragColliders);
        ragDoll.SetActive(false);

        IsKnockedDown = false;
        rb.isKinematic = false;
        mechanimDoll.SetActive(true);
        foreach (var @skin in mecSkinnedMeshes)
            skin.enabled = false;
        foreach (var @rend in mecMeshRenderers)
            rend.enabled = false;
        yield return new WaitForSeconds(0.001f);
        foreach (var @skin in mecSkinnedMeshes)
            skin.enabled = true;
        foreach (var @rend in mecMeshRenderers)
            rend.enabled = true;

        ragDollTimeout = null;
    }
}
