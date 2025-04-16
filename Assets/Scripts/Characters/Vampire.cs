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
		else if (flag == "START_HIT_EFFECT")
		{
            if (IsClient)
			{
                StartHitEffect(hitColor);
            }
        }
		else if (flag == "WALK_ANIM")
        {
            if (IsServer)
            {
                SendUpdate("WALK_ANIM", "");
            }
            else if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("VampireWalk"))
                    anim.Play("VampireWalk", -1, 0f);
            }
        }
		else if (flag == "CLIMB_ANIM")
        {
            if (IsServer)
            {
                SendUpdate("CLIMB_ANIM", "");
            }
            else if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("VampireClimb"))
                    anim.Play("VampireClimb", -1, 0f);
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
		else if(flag == "HIDE_HP")
        {
			int health = int.Parse(value);
			this.transform.GetChild(health).GetComponent<SpriteRenderer>().enabled = false;
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
		health = 3;
		
		Vector2 belowFeet = transform.position;
		belowFeet.y -= GetComponent<Collider2D>().bounds.size.y;

		anim = GetComponent<Animator>();

		//teleport slime to platform below it to avoid fall off issues
		RaycastHit2D hit = Physics2D.Raycast(belowFeet, Vector2.down, Mathf.Infinity, floorLayer);
		float standingY = GetTileUpperY(hit);
		Vector2 standingPos = transform.position;
		standingPos.y = standingY + GetComponent<Collider2D>().bounds.size.y / 2;
		transform.position = standingPos;
	}

	protected override void OnCollisionEnter2D(Collision2D collision)
	{
		if (IsServer)
		{
            base.OnCollisionEnter2D(collision);

			//the index of the layer on the layers list			
			bool hitFloor = collision.gameObject.layer == 6 || collision.gameObject.layer == 7;

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
					SendUpdate("WALK_ANIM", "GoodMorning");
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
		SendUpdate("COLLIDER", "true");
		phasingThroughFloor = false;
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
					SendUpdate("CLIMB_ANIM", "GoodMorning");
					transform.position = new Vector2(dismountHit.transform.position.x, this.transform.position.y);
					myRig.gravityScale = 0f;
					
					StartCoroutine(PhaseThroughFloor(0.5f));
					SendUpdate("GRAVITY", "0");
				}
				else if(state == STATE.LADDER_UP)
                {					
					myRig.velocity = Vector2.zero;					
					state = STATE.MOVING;
					SendUpdate("WALK_ANIM", "GoodMorning");
					
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
					SendUpdate("CLIMB_ANIM", "GoodMorning");
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
				SendUpdate("FLIP", spriteRender.flipX.ToString());
				IsDirty = false;
			}
			yield return new WaitForSeconds(0.05f);
		}
	}

	private void Update()
	{
		if (IsServer)
		{
			if (GameManager.inCountdown)
			{
				myRig.velocity = Vector2.zero;
				return;
			}

			switch (state)
			{
				case STATE.MOVING:
				{
					Debug.Log("vampire moving");
					Move();
					break;
				}
				case STATE.LADDER_UP:
				{
					Debug.Log("vampire moving up");
					myRig.velocity = new Vector2(0, ladderSpeed);
					break;
				}
				case STATE.LADDER_DOWN:
				{
					Debug.Log("vampire moving down");
					myRig.velocity = new Vector2(0, -ladderSpeed);				
					break;
				}
			}

			Debug.Log("state: " + state);
		}
	}
}
