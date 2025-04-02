using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Vampire : Enemy
{		
	private STATE state = STATE.MOVING;
	
	enum STATE
    {
		MOVING,
		LADDER
    }

	public override void HandleMessage(string flag, string value)
	{
		if (flag == "FLIP")
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

	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(IsServer)
        {			
			//the index of the layer on the layers list
			int floorLayer = 6;
			if(collision.gameObject.layer == floorLayer)
            {
				state = STATE.MOVING;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
	{
		if (IsServer)
		{
			bool hitDismount = collision.GetComponentInParent<LadderObj>() != null;
			if (hitDismount)
			{

			}
		}
    }    

    public override IEnumerator SlowUpdate()
	{
		while (IsConnected)
		{
			if (IsServer)
			{
				IsDirty = false;
			}
			yield return new WaitForSeconds(0.05f);
		}
	}

    private void Update()
    {
		if (IsServer)
		{
			Move();
		}
	}
}
