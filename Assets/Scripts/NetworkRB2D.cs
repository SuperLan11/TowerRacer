using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;


[RequireComponent(typeof(Rigidbody2D))]
public class NetworkRB2D : NetworkComponent
{
    //synch vars
    public Vector2 lastPos;
    public Vector3 lastRot;
    public Vector2 lastVel;
    public float lastAngVel;

    //non-synch vars
    public float threshold;
    public float eThreshold;
    public bool useAdjustVel;
    public Vector2 adjustVel;
    public Rigidbody2D myRig;

    public Dictionary<string, string> NET_FLAGS = new Dictionary<string, string>()
    {
        { "POS2D","POS2D" },
        { "ROT2D","ROT2D" },
        { "VEL2D", "VEL2D" },
        { "ANG2D", "ANG2D"}
    };

    public override void HandleMessage(string flag, string value)
    {        
        /*if (IsServer)
        {
            Debug.Log("server got flag " + flag + " in " + this.GetType().Name);
        }

        if(IsClient)
        {
            Debug.Log("client got flag " + flag + " in " + this.GetType().Name);
        }*/

        if (IsClient && flag == NET_FLAGS["POS2D"])
        {
            //Debug.Log("get Vector2 pos from: " + value);
            lastPos = Vector2FromString(value);
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
        else if (IsClient && flag == NET_FLAGS["ROT2D"])
        {
            //Debug.Log("get Vector2 rot from: " + value);
            lastRot = Vector2FromString(value);
            if ((lastRot - transform.eulerAngles).magnitude > eThreshold)
            {
                transform.rotation = Quaternion.Euler(lastRot);
            }
        }
        else if (IsClient && flag == NET_FLAGS["VEL2D"])
        {
            //Debug.Log("get Vector2 vel from: " + value);
            lastVel = Vector2FromString(value);
            if (lastVel.magnitude < 0.01f)
            {
                adjustVel = Vector3.zero;
            }
        }
        else if (IsClient && flag == NET_FLAGS["ANG2D"])            
        {
            //Debug.Log("ang vel is: " + value);
            lastAngVel = float.Parse(value);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // can use Start for non-network related stuff. client/server doesn't matter
        myRig = GetComponent<Rigidbody2D>();
    }

    public override void NetworkedStart()
    {
        // don't normally do control code in start        
    }

    public Vector2 Vector2FromString(string str)
    {
        //Vector2 is "(X,Y)"
        // Trim() removes whitespace chars        
        string[] args = str.Trim().Trim('(').Trim(')').Split(',');
        return new Vector2(
            float.Parse(args[0]),
            float.Parse(args[1])
            );
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
                    SendUpdate("POS2D", myRig.position.ToString());
                    lastPos = myRig.position;
                }

                if ((myRig.transform.eulerAngles - lastRot).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("ROT2D", myRig.transform.eulerAngles.ToString());
                    lastRot = myRig.transform.eulerAngles;
                }

                if ((myRig.velocity - lastVel).magnitude > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("VEL2D", myRig.velocity.ToString());
                    lastVel = myRig.velocity;                    
                }

                if (myRig.angularVelocity - lastAngVel > threshold)
                {
                    // transform.position is basically same as myRig.position
                    SendUpdate("ANG2D", myRig.angularVelocity.ToString());
                    lastAngVel = myRig.angularVelocity;
                }

                if (IsDirty)
                {
                    SendUpdate("POS2D", myRig.position.ToString());
                    SendUpdate("ROT2D", myRig.transform.eulerAngles.ToString());
                    SendUpdate("VEL2D", myRig.velocity.ToString());
                    SendUpdate("ANG2D", myRig.angularVelocity.ToString());
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
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
