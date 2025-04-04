using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;

public class LadderObj : NetworkComponent
{
	public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();
	public Player attachedPlayer = null;
	public float ladderSpeed;

	public override void HandleMessage(string flag, string value)
	{
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
	}


	public override void NetworkedStart()
	{

	}

    private void OnTriggerStay2D(Collider2D collision)
    {
		if (IsServer)
		{			
			
		}
    }

	public void InitializeLadderVariables(Player p){
		if (IsServer){
			Player player = p;
			bool touchingPlayer = (player != null);
			
			if (touchingPlayer){
				Vector2 grabPos = new Vector2(GetComponent<BoxCollider2D>().bounds.center.x, player.transform.position.y);
				player.currentLadder = this;
				attachedPlayer = player;

				player.transform.position = grabPos;
			}
		}
	}

    private void OnTriggerExit2D(Collider2D collision)
    {
		if (IsServer)
		{
			bool playerExited = collision.gameObject.GetComponentInParent<PlayerController>() != null;

			//comment from here
            /*if (playerExited)
            {
                if (attachedPlayer == null)
                    return;

                //means player got to top of ladder
                if (attachedPlayer.currentLadder != null)
                {                    
                    attachedPlayer.myRig.velocity = Vector2.zero;                    
                    attachedPlayer.currentLadder = null;
                    attachedPlayer.myRig.gravityScale = 1f;
					attachedPlayer.transform.position

					float height = GetComponent<Collider2D>().bounds.size.y;
					//raycast to floor instead
					RaycastHit2D floor = Physics2D.Raycast(dismountHit.transform.position, Vector2.down, height, floorLayer);
					Vector2 dismountPos = floor.point;
					dismountPos.y += height / 2;
					SendUpdate("DISMOUNT", dismountPos.ToString());

					SendUpdate("GRAVITY", attachedPlayer.name.ToString());
                    attachedPlayer = null;
                }
            }*/
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
