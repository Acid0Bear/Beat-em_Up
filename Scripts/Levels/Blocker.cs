using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class Blocker : MonoBehaviour
{
    private ConfigurableJoint joint = null;
    private float InitialPos;

    [SerializeField] private int MotorValue = 0;
    public bool ForceStart;
    public float Delay;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        StartCoroutine(waitTillAllReady());
    }

    private IEnumerator waitTillAllReady()
    {
        while (true)
        {
            if (NetClient.netClient && (NetClient.PlayersReady || !NetClient.netClient.IsConnected || ForceStart))
            {
                joint = GetComponent<ConfigurableJoint>();
                InitialPos = joint.connectedAnchor.y;
                var yDrive = joint.yDrive;
                yDrive.maximumForce = MotorValue;
                joint.yDrive = yDrive;
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
            var lim = joint.linearLimit;
            if (this.transform.position.y - InitialPos >= lim.limit - 0.5f)
            {
                yield return new WaitForSecondsRealtime(Delay);
                joint.targetPosition = new Vector3(0, Mathf.Abs(joint.targetPosition.y), 0);
            }
            else if (this.transform.position.y - InitialPos <= -lim.limit + 0.5f)
            {
                yield return new WaitForSecondsRealtime(Delay);
                joint.targetPosition = new Vector3(0, Mathf.Abs(joint.targetPosition.y) * -1, 0);
            }
            yield return null;
        }
    }
}
