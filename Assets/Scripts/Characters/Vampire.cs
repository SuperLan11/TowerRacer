using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Vampire : Enemy
{
	private STATE state = STATE.MOVING;	
	[SerializeField] private float ladderSpeed = 8f;
	private int startingLayer;
	private LayerMask ladderLayer;
	private float ladderBottomY;


	enum STATE
	{
		MOVING,
		LADDER_DOWN,
		LADDER_UP
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
		startingLayer = gameObject.layer;
		ladderLayer = LayerMask.NameToLayer("Ladder");
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (IsServer)
		{
			//the index of the layer on the layers list
			int floorLayer = 6;
			if (collision.gameObject.layer == floorLayer)
			{
				Debug.Log("hit floor");
				gameObject.layer = startingLayer;
				state = STATE.MOVING;
				myRig.gravityScale = 1f;
			}
		}
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
					gameObject.layer = ladderLayer;
					state = STATE.LADDER_DOWN;
					transform.position = new Vector2(dismountHit.transform.position.x, this.transform.position.y);
					myRig.gravityScale = 0f;
				}
				else if(state == STATE.LADDER_UP)
                {
					gameObject.layer = startingLayer;
					state = STATE.MOVING;
					transform.position = dismountHit.transform.position;
					myRig.gravityScale = 1f;
				}
			}
			else if (ladderHit != null)
			{
				gameObject.layer = ladderLayer;
				state = STATE.LADDER_UP;
				transform.position = new Vector2(ladderHit.transform.position.x, this.transform.position.y);
				myRig.gravityScale = 0f;
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
