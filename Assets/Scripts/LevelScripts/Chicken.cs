using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Chicken : Item
{
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
				SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
		if (IsServer)
		{
			Player playerHit = collision.GetComponentInParent<Player>();
			if (playerHit != null)
            {				
				//add one to player hp
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

	}

    protected override void UseItem()
    {
        Debug.LogError("Thou hasn't yet implementeth thine Chicken powerup!");
    }
}