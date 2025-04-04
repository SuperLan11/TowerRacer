using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Vampire : Enemy
{
	public STATE state = STATE.MOVING;	
	[SerializeField] private float ladderSpeed = 8f;	
	private bool phasingThroughFloor = false;

	public enum STATE
	{
		MOVING,
		LADDER_DOWN,
		LADDER_UP
	}

	public override void HandleMessage(string flag, string value)
	{
		if(flag == "DISMOUNT")
        {
			if(IsClient)
            {
				Vector2 dismountPos = PlayerController.Vector2FromString(value);
				transform.position = dismountPos;
            }
        }
		else if(flag == "GRAVITY")
        {
			if(IsClient)
            {
				myRig.gravityScale = float.Parse(value);
            }
        }		
		else if(flag == "COLLIDER")
        {
			if(IsClient)
            {
				GetComponent<Collider2D>().enabled = bool.Parse(value);
            }
        }
		else if (flag == "FLIP")
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
				SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
			}
		}
	}

	public override void NetworkedStart()
	{
		
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (IsServer)
		{
			//the index of the layer on the layers list			
			bool hitFloor = collision.gameObject.layer == 6;	
			
			if (hitFloor && state == STATE.LADDER_DOWN)
			{
				if (phasingThroughFloor)
				{
					//if player is still inside floor when re-enabling collider, teleport just beneath floor
					Vector2 teleportPos = collision.collider.bounds.min;
					teleportPos.y -= GetComponent<Collider2D>().bounds.size.y/2;
					transform.position = teleportPos;
				}
				else
				{					
					state = STATE.MOVING;
					myRig.gravityScale = 1f;
				}
			}
		}
	}

	private IEnumerator PhaseThroughFloor(float seconds)
    {
		phasingThroughFloor = true;
		GetComponent<Collider2D>().enabled = false;
		SendUpdate("COLLIDER", "false");
		yield return new WaitForSeconds(seconds);
		
		GetComponent<Collider2D>().enabled = true;
		phasingThroughFloor = false;
		SendUpdate("COLLIDER", "true");
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (IsServer)
		{
			DismountTrigger dismountHit = collision.GetComponent<DismountTrigger>();
			LadderObj ladderHit = collision.GetComponent<LadderObj>();			

			if (dismountHit != null)
			{
				if (state == STATE.MOVING)
				{										
					state = STATE.LADDER_DOWN;
					transform.position = new Vector2(dismountHit.transform.position.x, this.transform.position.y);
					myRig.gravityScale = 0f;
					
					StartCoroutine(PhaseThroughFloor(0.5f));
					SendUpdate("GRAVITY", "0");
				}
				else if(state == STATE.LADDER_UP)
                {					
					myRig.velocity = Vector2.zero;					
					state = STATE.MOVING;
					
					float height = GetComponent<Collider2D>().bounds.size.y;
					//raycast to floor instead
					RaycastHit2D floor = Physics2D.Raycast(dismountHit.transform.position, Vector2.down, height, floorLayer);
					Vector2 dismountPos = floor.point;
					dismountPos.y += height / 2;
					SendUpdate("DISMOUNT", dismountPos.ToString());

					transform.position = dismountPos;
					//transform.position = dismountHit.transform.position;
					myRig.gravityScale = 1f;
					SendUpdate("GRAVITY", "1");
				}
			}
			else if (ladderHit != null)
			{
				bool hitLadderBottom = transform.position.y < collision.bounds.center.y;
				if (state == STATE.MOVING && hitLadderBottom)
				{					
					state = STATE.LADDER_UP;
					transform.position = new Vector2(ladderHit.transform.position.x, this.transform.position.y);
					myRig.gravityScale = 0f;
					SendUpdate("GRAVITY", "0");
				}
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
			switch (state)
			{
				case STATE.MOVING:
				{
					Move();
					break;
				}
				case STATE.LADDER_UP:
				{
					myRig.velocity = new Vector2(0, ladderSpeed);
					break;
				}
				case STATE.LADDER_DOWN:
				{
					myRig.velocity = new Vector2(0, -ladderSpeed);					
					break;
				}
			}
		}
	}
}
