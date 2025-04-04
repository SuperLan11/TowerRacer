using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;

public class LadderTest : NetworkComponent
{
	public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();
	public PlayerController attachedPlayer = null;
	public float ladderSpeed;
	private LayerMask floorLayer;

	public override void HandleMessage(string flag, string value)
	{
		if (flag == "NOGRAVITY")
		{
			if (IsClient)
			{
				GameObject.Find(value).GetComponent<PlayerController>().myRig.gravityScale = 0f;
			}
		}
		else if (flag == "GRAVITY")
		{
			if (IsClient)
			{
				GameObject.Find(value).GetComponent<PlayerController>().myRig.gravityScale = 1f;
			}
		}
		if (flag == "DEBUG")
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

	private void Start()
	{
		if (GetComponent<NetworkRB2D>() != null)
			OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
		else if (GetComponentInChildren<NetworkRB2D>() != null)
			OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
		else if (GetComponent<NetworkTransform>() != null)
			OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
		else if (GetComponentInChildren<NetworkTransform>() != null)
			OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;

		 floorLayer = LayerMask.GetMask("Floor");
	}


	public override void NetworkedStart()
	{

	}

	private void OnTriggerStay2D(Collider2D collision)
	{
		if (IsServer)
		{
			PlayerController player = collision.gameObject.GetComponentInParent<PlayerController>();
			bool touchingPlayer = player != null;

			if(touchingPlayer)
				Debug.Log("touching player");

			if (touchingPlayer && !player.onLadder && (player.holdingDir == "up" || player.holdingDir == "down"))
			{
				Debug.Log("getting on ladder");

				Vector2 grabPos = transform.position;
				grabPos.y = player.transform.position.y;
				player.state = "LADDER";
				player.grabbedLadder = this;
				attachedPlayer = player;

				player.transform.position = grabPos;
				player.onLadder = true;
				player.myRig.gravityScale = 0f;
				SendUpdate("NOGRAVITY", player.name.ToString());
			}
		}
	}

	private void OnTriggerExit2D(Collider2D collision)
	{
		if (IsServer)
		{
			PlayerController playerExited = collision.gameObject.GetComponentInParent<PlayerController>();
            if (playerExited != null)
            {
                if (attachedPlayer == null)
                    return;

                //means player got to top of ladder
                if (attachedPlayer.onLadder)
                {
                    attachedPlayer.state = "NORMAL";
                    attachedPlayer.myRig.velocity = Vector2.zero;
                    attachedPlayer.onLadder = false;
                    attachedPlayer.grabbedLadder = null;

					float height = playerExited.GetComponent<Collider2D>().bounds.size.y;
					//raycast to floor instead
					Vector3 playerTop = playerExited.transform.position + new Vector3(0, height / 2, 0);
					RaycastHit2D floor = Physics2D.Raycast(playerTop, Vector2.down, height*2, floorLayer);
					Vector2 dismountPos = floor.point;
					dismountPos.y += height / 2;
					Debug.Log("dismount pos: " + dismountPos);
					playerExited.SendUpdate("DISMOUNT", dismountPos.ToString());

					attachedPlayer.myRig.gravityScale = 1f;
                    SendUpdate("GRAVITY", attachedPlayer.name.ToString());
                    attachedPlayer = null;
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

	// Update is called once per frame
	void Update()
	{

	}
}