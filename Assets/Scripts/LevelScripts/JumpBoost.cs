using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpBoost : Item
{ 
	public override void NetworkedStart()
	{

	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsServer)
        {
			Player player = collision.GetComponentInParent<Player>();
			if(player != null)
            {
				//add one to player jumps
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

	private void Update()
	{

	}
}
