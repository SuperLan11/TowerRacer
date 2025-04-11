using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Slime : Enemy
{       
    public override void HandleMessage(string flag, string value)
    {
        if(flag == "FLIP")
        {
            if (IsClient)
            {
                spriteRender.flipX = bool.Parse(value);
            }
        }
        else if (flag == "DEBUG")
        {
            Debug.Log(value);
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
        else if (!OTHER_FLAGS.ContainsKey(flag))
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
    }    

    public override void NetworkedStart()
    {        
        spriteRender.flipX = true;
        health = 1;
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            base.OnCollisionEnter2D(collision);
            
            if (collision.gameObject.GetComponent<Enemy>() != null)
            {
                dir *= -1;
                spriteRender.flipX = !spriteRender.flipX;
                SendUpdate("FLIP", spriteRender.flipX.ToString());
            }
        }
    } 

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {
                SendUpdate("FLIP", spriteRender.flipX.ToString());
                IsDirty = false;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            Move();
        }
    }
}
