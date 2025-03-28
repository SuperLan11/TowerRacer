/*
@Description - Translation of Dr. Towle's Networked Rigidbody code to Networked Rigidbody2D
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;


[RequireComponent(typeof(Rigidbody2D))]
public class NetworkRB2D : NetworkComponent
{
    //synch vars
    [System.NonSerialized] public Vector2 lastPos;
    [System.NonSerialized] public Vector3 lastRot;
    [System.NonSerialized] public Vector2 lastVel;
    [System.NonSerialized] public float lastAngVel;

    //non-synch vars
    public float threshold;
    public float eThreshold;    
    [System.NonSerialized] public Vector2 adjustVel;
    [System.NonSerialized] public float adjustAngVel;
    [System.NonSerialized] public Rigidbody2D myRig;
    public bool useAdjustVel;
    public bool useAdjustAngVel;

    public Dictionary<string, string> NET_FLAGS = new Dictionary<string, string>()
    {
        { "POS2D","POS2D" },
        { "ROT2D","ROT2D" },
        { "VEL2D", "VEL2D" },
        { "ANG2D", "ANG2D"}
    };

    public override void HandleMessage(string flag, string value)
    {                
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
        else if(flag == "DEBUG")
        {
            Debug.Log(value);
            if(IsClient)
            {
                SendCommand("DEBUG", value);
            }
        }
        else if (IsClient && flag == NET_FLAGS["ANG2D"])            
        {                           
            lastAngVel = float.Parse(value);
            /*if (useAdjustAngVel)
            {
                adjustAngVel = lastAngVel - myRig.angularVelocity;
            }
            if (Mathf.Abs(lastAngVel - myRig.angularVelocity) > eThreshold)
            {
                myRig.transform.eulerAngles = lastRot;
                adjustAngVel = 0f;
            }*/            
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //thought: could you drag in a different rigidbody than the one on this script to network it?
        myRig = GetComponent<Rigidbody2D>();
        if(GetComponent<NetRope>() != null)
            myRig = transform.GetChild(0).GetComponent<Rigidbody2D>();
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
                    SendUpdate("POS2D", myRig.position.ToString());
                    lastPos = myRig.position;
                }

                if ((myRig.transform.eulerAngles - lastRot).magnitude > threshold)
                {                    
                    SendUpdate("ROT2D", myRig.transform.eulerAngles.ToString());
                    lastRot = myRig.transform.eulerAngles;
                }

                if ((myRig.velocity - lastVel).magnitude > threshold)
                {                    
                    SendUpdate("VEL2D", myRig.velocity.ToString());
                    lastVel = myRig.velocity;
                }
                
                if (Mathf.Abs(myRig.angularVelocity - lastAngVel) > threshold)
                {
                    //Debug.Log("SendUpdate, angDiff = " + (myRig.angularVelocity - lastAngVel));                    
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
            /*if (useAdjustAngVel)
            {
                myRig.angularVelocity += adjustAngVel;
            }*/
        }
    }
}
