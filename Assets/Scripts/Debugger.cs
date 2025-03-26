using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Debugger : NetworkComponent
{
    public override void HandleMessage(string flag, string value)
    {
        Debug.Log("debugger got flag " + flag);
        if(flag == "DEBUG")
        {
            Debug.Log(value);
            if(IsClient)
            {
                SendCommand(flag, value);
            }
        }
    }

    public override void NetworkedStart()
    {
        
    }

    public override IEnumerator SlowUpdate()
    {
        yield return new WaitForSeconds(0.05f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(IsClient)
        {
            //Debug.Log("send update debug");
            //SendUpdate("DEBUG", "");
            SendCommand("DEBUG", "test");
        }
    }
}
