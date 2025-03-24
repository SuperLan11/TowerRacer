using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class NetworkTransform : NetworkComponent
{
    //synch vars
    public Vector3 lastPosition;
    public Vector3 lastRotation;
    public Vector3 lastScale;

    //non-synch vars
    public float Threshold;
    //Ethreshold is emergency threshold for teleporting
    public float Ethreshold;
    public float speed = 5;
    public float rotationSpeed = 5f;
    public bool Smooth;

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "POS")
        {
            Vector3 temp = NetworkCore.Vector3FromString(value);
            if ((temp - this.transform.position).magnitude > Ethreshold && !Smooth)
            {
                this.transform.position = temp;
            }
            lastPosition = temp;
        }
        else if (flag == "ROT")
        {
            Vector3 temp = NetworkCore.Vector3FromString(value);
            if ((temp - this.transform.rotation.eulerAngles).magnitude < Ethreshold || !Smooth)
            {
                Quaternion qt = new Quaternion();
                qt.eulerAngles = temp;
                this.transform.rotation = qt;
            }
            lastRotation = temp;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    public override void NetworkedStart()
    {
        if (IsServer)
        {
            //this.transform.position = new Vector3(-10, 0, 0);
            lastPosition = transform.position;
            lastRotation = transform.rotation.eulerAngles;
        }
    }

    public override IEnumerator SlowUpdate()
    {
        // don't need network transform unless object needs to move
        while (MyCore.IsConnected)
        {
            // only transmits movement from server to client. server is unaffected
            if (IsServer)
            {
                float distance = (this.transform.position - lastPosition).magnitude;
                // only client deals with Ethreshold
                if (distance > Threshold)
                {
                    // reduce number of bytes to send
                    SendUpdate("POS", this.transform.position.ToString("F2"));
                    lastPosition = this.transform.position;
                }
                if ((this.transform.rotation.eulerAngles - lastRotation).magnitude > Threshold)
                {
                    lastRotation = this.transform.rotation.eulerAngles;
                    SendUpdate("ROT", lastRotation.ToString());
                }
                if ((this.transform.localScale - lastScale).magnitude > Threshold)
                {
                    SendUpdate("SCALE", this.transform.localScale.ToString("F3"));
                    lastScale = this.transform.localScale;
                }

                if (IsDirty)
                {
                    SendUpdate("POS", lastPosition.ToString("F2"));
                    SendUpdate("ROT", lastRotation.ToString("F2"));
                    SendUpdate("SCALE", lastScale.ToString("F3"));
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }    

    // Update is called once per frame
    void Update()
    {
        // not strictly following the server, so performance is better
        if (IsClient && Smooth)
        {
            this.transform.position = Vector3.Lerp(this.transform.position, lastPosition, 0.2f); // Time.deltaTime * speed
            Quaternion qt = new Quaternion();
            qt.eulerAngles = Vector3.Lerp(this.transform.rotation.eulerAngles, lastRotation, rotationSpeed * Time.deltaTime);
            this.transform.rotation = qt;
        }
    }
}
