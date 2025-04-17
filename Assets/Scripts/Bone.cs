using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Bone : Projectile
{	
	public float speed = 10f;
	public int dir = 0;
	private float destroyTime = 5f;

	[SerializeField] private AudioSource boneHitSfx;

	public override void HandleMessage(string flag, string value)
	{		
		if(flag == "HIDE")
        {
			if(IsClient)
            {
				boneHitSfx.Play();
				spriteRender.enabled = false;
				GetComponent<Collider2D>().enabled = false;
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

	private IEnumerator DestroyAfterSfx()
    {
		while (boneHitSfx.isPlaying)
			yield return new WaitForSeconds(0.1f);

		MyCore.NetDestroyObject(this.NetId);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(IsServer)
        {			
			Player playerHit = other.GetComponentInParent<Player>();
			bool hitWall = other.gameObject.tag == "WALL";
			if(playerHit != null)
            {
				playerHit.TakeDamage(1);
				spriteRender.enabled = false;
				GetComponent<Collider2D>().enabled = false;
				SendUpdate("HIDE", "");
			}
			else if (hitWall)
            {
				spriteRender.enabled = false;
				GetComponent<Collider2D>().enabled = false;
				SendUpdate("HIDE", "");
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
