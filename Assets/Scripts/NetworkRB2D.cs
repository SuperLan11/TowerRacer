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
    [System.NonSerialized] public float lastRot;
    [System.NonSerialized] public Vector2 lastVel;
    [System.NonSerialized] public float lastAngVel;

    //non-synch vars
    public float threshold;
    public float eThreshold;    
    [System.NonSerialized] public Vector2 adjustVel;
    [System.NonSerialized] public float adjustAngVel;
    public Rigidbody2D myRig;
    public bool useAdjustVel;
    public bool useAdjustAngVel;

    public Dictionary<string, string> FLAGS = new Dictionary<string, string>()
    {
        { "POS2D","POS2D" },
        { "ROT2D","ROT2D" },
        { "VEL2D", "VEL2D" },
        { "ANG2D", "ANG2D" }
    };

    public override void HandleMessage(string flag, string value)
    {                        
        if (IsClient && flag == FLAGS["POS2D"])
        {            
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
        else if (IsClient && flag == FLAGS["ROT2D"])
        {                        
            lastRot = float.Parse(value);
            if (Mathf.Abs(lastRot - myRig.rotation) > eThreshold)
            {                
                myRig.rotation = lastRot;
            }
        }
        else if (IsClient && flag == FLAGS["VEL2D"])
        {            
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
        else if (IsClient && flag == FLAGS["ANG2D"])            
        {                           
            lastAngVel = float.Parse(value);
            /*if (useAdjustAngVel)
            {
                adjustAngVel = lastAngVel - myRig.angularVelocity;
            }*/
            /*if (Mathf.Abs(lastAngVel - myRig.angularVelocity) > eThreshold)
            {
                myRig.angularVelocity = lastAngVel - myRig.angularVelocity;
                adjustAngVel = 0f;
            }*/
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //you can set a different rigidbody than the one on this object by using the inspector to network it
        if (myRig == null)
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
                    SendUpdate("POS2D", myRig.position.ToString());
                    lastPos = myRig.position;
                }

                if (Mathf.Abs(myRig.rotation - lastRot) > threshold)
                {                    
                    SendUpdate("ROT2D", myRig.rotation.ToString());
                    lastRot = myRig.rotation;
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
                    SendUpdate("ROT2D", myRig.rotation.ToString());
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
