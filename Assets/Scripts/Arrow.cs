using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Arrow : Projectile
{	
	public float speed = 10f;
	public int dir = 0;
	private float destroyTime = 5f;

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

	public override void NetworkedStart()
	{
		if (dir == -1)
			spriteRender.flipX = true;

		if (IsServer)
		{
			StartCoroutine(TTL(destroyTime));
		}
	}    

	//shouldn't be necessary since there will be walls, but just in case
	private IEnumerator TTL(float seconds)
    {
		yield return new WaitForSeconds(seconds);
		MyCore.NetDestroyObject(this.NetId);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(IsServer)
        {			
			bool hitPlayer = other.GetComponentInParent<Player>() != null;
			bool hitWall = other.gameObject.tag == "WALL";
			if (hitPlayer || hitWall)
            {
				MyCore.NetDestroyObject(this.NetId);
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
    private void Update()
    {
        if(IsServer)
        {
			myRig.velocity = new Vector2(dir * speed, 0);
        }
    }
}
