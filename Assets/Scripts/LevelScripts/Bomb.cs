using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;

public class Bomb : Projectile
{
	public Vector2 launchVec = Vector2.zero;
	public float launchSpeed = 15f;

	public Player currentPlayer;	

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
			bool hitFloor = collision.GetComponent<TilemapCollider2D>() != null;
			bool hitLadder = collision.GetComponent<LadderObj>() != null;

			if (collision.GetComponent<NetworkComponent>() != null)
			{				
				int collidedOwner = collision.GetComponent<NetworkComponent>().Owner;
				//means the bomb hit a networked object besides the current player
				if (collidedOwner != this.Owner)
				{
					Debug.Log("destroying bomb because it hit " + collision.gameObject.name);
					InitializeExplosion();
				}
			}
			//to allow bombs to go through jump throughs, only destroy them on floor when they are falling
			else if( (!hitFloor && !collision.isTrigger) || (hitFloor && myRig != null && myRig.velocity.y < 0))
            {
				//hit an non-networked object, so the object was not the current player				
				Debug.Log("destroying bomb because it hit " + collision.gameObject.name);
				InitializeExplosion();
            }
		}
    }

	private IEnumerator WaitToDestroyBomb(float seconds)
	{
		yield return new WaitForSeconds(seconds);
		MyCore.NetDestroyObject(this.NetId);
	}

	private void InitializeExplosion(){
		GameObject explosionObj = MyCore.NetCreateObject(Idx.EXPLOSION, Owner, transform.position, Quaternion.identity);
		Explosion explosion = explosionObj.GetComponent<Explosion>();
		explosion.currentPlayer = this.currentPlayer;

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