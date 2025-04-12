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

	private AudioSource ladderSfx;

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

		ladderSfx = GetComponent<AudioSource>();
	}


	public override void NetworkedStart()
	{

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
		if (!IsLocalPlayer)
			return;

		//cut sound when player not moving, only do on local player
		if (attachedPlayer == null)
		{
			ladderSfx.Pause();
			return;
		}		

		Vector2 playerInput = attachedPlayer.moveInput;
		if (Mathf.Abs(playerInput.y) > 0.01f)
		{
			if (!ladderSfx.isPlaying)
				ladderSfx.Play();
		}
		else
        {
			ladderSfx.Pause();
        }        
    }
}
