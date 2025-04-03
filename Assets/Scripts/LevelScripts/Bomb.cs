using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Bomb : Projectile
{
	public Vector2 launchVec;

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

	public override void NetworkedStart()
	{
		//need to set velocity indirectly in networkedstart because
		//setting it after creation in player handle message executes before Start()
		if (IsServer)
		{
			myRig.velocity = launchVec;
		}
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {        
		if (IsServer)
		{			
			if (collision.GetComponent<NetworkComponent>() != null)
			{				
				int collidedOwner = collision.GetComponent<NetworkComponent>().Owner;
				//means the bomb hit an object besides the current player
				if (collidedOwner != this.Owner)
				{
					Debug.Log("destroying bomb since it hit a different owner");
					StartCoroutine(WaitToDestroyBomb(0.1f));
				}
			}
			else
            {
				//hit an non-networked object, so the object was not the current player
				StartCoroutine(WaitToDestroyBomb(0.1f));
            }
		}
    }

	private IEnumerator WaitToDestroyBomb(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		MyCore.NetDestroyObject(this.NetId);
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
}