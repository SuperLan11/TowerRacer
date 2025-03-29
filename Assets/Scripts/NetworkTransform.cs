using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NETWORK_ENGINE;

public class NetworkTransform : NetworkComponent
{
    //[Sync Vars]  - 1. Am I on the server? 2. Have I told everyone that I've changed?
    public Vector3 lastPosition;
    public Vector3 lastRotation;

    //[NonSync Vars]
    public float threshold = 0.01f, emergencyThreshold = 1.0f;
    public float speed = 5f;

    public Dictionary<string, string> FLAGS = new Dictionary<string, string>()
    {
        { "POSITION","POSITION" },
        { "ROTATION","ROTATION" }       
    };

    public override void HandleMessage(string flag, string value)
    {
        if (flag == FLAGS["POSITION"])
        {
            if (IsClient)
            {
                lastPosition = NetworkCore.Vector3FromString(value);
            }
        }
        if (flag == FLAGS["ROTATION"])
        {
            if (IsClient)
            {
                lastRotation = NetworkCore.Vector3FromString(value);
            }
        }
    }

    public override void NetworkedStart()
    {
        if (IsServer)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation.eulerAngles;
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (true)
        {
            if (IsServer)
            {
                float distance = (this.transform.position - lastPosition).magnitude;

                bool positionOutOfSync = (distance > threshold);
                bool rotationOutOfSync = ((this.transform.rotation.eulerAngles - lastRotation).magnitude > threshold);

                if (positionOutOfSync)
                {
                    SendUpdate("POSITION", this.transform.position.ToString());
                    lastPosition = this.transform.position;
                }
                if (rotationOutOfSync)
                {
                    lastRotation = this.transform.rotation.eulerAngles;
                    SendUpdate("ROTATION", lastRotation.ToString());
                }

                if (IsDirty)
                {
                    SendUpdate("POSITION", lastPosition.ToString());
                    SendUpdate("ROTATION", lastRotation.ToString());

                    IsDirty = false;
                }
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    void Start()
    {

    }

    void Update()
    {        
        if (IsClient)
        {
            float distance = (this.transform.position - this.lastPosition).magnitude;

            if (distance > emergencyThreshold)
            {
                this.transform.position = this.lastPosition;
            }
            else
            {
                this.transform.position = Vector3.Lerp(this.transform.position, lastPosition, Time.deltaTime * speed);
            }
            this.transform.rotation = Quaternion.Euler(lastRotation);
        }
    }
}