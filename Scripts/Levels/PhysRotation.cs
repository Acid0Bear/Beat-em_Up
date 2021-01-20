using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HingeJoint))]
public class PhysRotation : MonoBehaviour
{
    private HingeJoint joint = null;
    public int MotorValue;
    public bool UseLimits;
    public bool ForceStart;
    public int Delay { get; set; }

    private void Start()
    {
        joint = GetComponent<HingeJoint>();
        StartCoroutine(waitTillAllReady());
    }

    private IEnumerator waitTillAllReady()
    {
        while (true)
        {
            if (NetClient.netClient && (NetClient.PlayersReady || !NetClient.netClient.IsConnected || ForceStart))
            {
                joint = GetComponent<HingeJoint>();
                var mot = joint.motor;
                mot.force = MotorValue;
                joint.motor = mot;
                if(UseLimits)
                    StartCoroutine(AngleLimits());
                yield break;
            }
            yield return null;
        }
    }

    private IEnumerator AngleLimits()
    {

        while (true)
        {
            if (joint.GetType() == typeof(HingeJoint))
            {
                var lim = joint.limits;
                if (joint.angle >= lim.max)
                {
                    var mot = joint.motor;
                    mot.targetVelocity = Mathf.Abs(mot.targetVelocity) * -1;
                    joint.motor = mot;
                }
                else if (joint.angle <= lim.min)
                {
                    var mot = joint.motor;
                    mot.targetVelocity = Mathf.Abs(mot.targetVelocity);
                    joint.motor = mot;
                }
            }
            yield return null;
        }
        
    }
}
