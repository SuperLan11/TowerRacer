using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;


[RequireComponent(typeof(Rigidbody))]
public class NetworkRB : NetworkComponent
{
    //synch vars
    public Vector3 lastPos;
    public Vector3 lastRot;
    public Vector3 lastVel;
    public Vector3 lastAngVel;

    //non-synch vars
    public float threshold;
    public float eThreshold;
    public bool useAdjustVel;
    public Vector3 adjustVel;
    public Rigidbody myRig;

    public override void HandleMessage(string flag, string value)
    {
        if (IsClient && flag == "POS")
        {
            lastPos = NetworkCore.Vector3FromString(value);
            if (useAdjustVel)
            {
                // lastPos is where object should be on server. myRig.position is where object actually is
                adjustVel = lastPos - myRig.position;
            }
            if ((lastPos - myRig.position).magnitude > eThreshold)
            {
                myRig.position = lastPos;
                // to prevent further desynch issues
                adjustVel = Vector3.zero;
            }
        }
        else if (IsClient && flag == "ROT")
        {
            lastRot = NetworkCore.Vector3FromString(value);
            if ((lastRot - myRig.rotation.eulerAngles).magnitude > eThreshold)
            {
                myRig.rotation = Quaternion.Euler(lastRot);
            }
        }
        else if (IsClient && flag == "VEL")
        {
            lastVel = NetworkCore.Vector3FromString(value);
            if (lastVel.magnitude < 0.01f)
            {
                adjustVel = Vector3.zero;
            }
        }
        else if (IsClient && flag == "ANG")
        {
            lastAngVel = NetworkCore.Vector3FromString(value);
        }
    }

    public override void NetworkedStart()
    {
        // don't normally do control code in start        
    }

    //return here
    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {                
                if ((myRig.position - lastPos).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("POS", myRig.position.ToString());
                    lastPos = myRig.position;
                }

                if ((myRig.rotation.eulerAngles - lastRot).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("ROT", myRig.rotation.eulerAngles.ToString());
                    lastRot = myRig.rotation.eulerAngles;
                }

                if ((myRig.velocity - lastVel).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("VEL", myRig.velocity.ToString());
                    lastVel = myRig.velocity;
                }

                if ((myRig.angularVelocity - lastAngVel).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("ANG", myRig.angularVelocity.ToString());
                    lastAngVel = myRig.angularVelocity;
                }

                if (IsDirty)
                {
                    SendUpdate("POS", myRig.position.ToString());
                    SendUpdate("ROT", myRig.rotation.ToString());
                    SendUpdate("VEL", myRig.velocity.ToString());
                    SendUpdate("ANG", myRig.angularVelocity.ToString());
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // can use Start for non-network related stuff. client/server doesn't matter
        myRig = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsClient)
        {
            myRig.velocity = lastVel;
            if (useAdjustVel)
            {
                myRig.velocity += adjustVel;
            }
            myRig.angularVelocity = lastAngVel;
        }
    }
}
