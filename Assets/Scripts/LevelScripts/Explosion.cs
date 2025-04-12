using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Explosion : NetworkComponent
{
	public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();
	private float explodeTime = 1f;
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

		if(IsServer)
        {
			StartCoroutine(WaitToDestroy(explodeTime));
        }
	}

	public override void NetworkedStart()
	{

	}

	/*
    public void OnCollisionEnter2D(Collision2D collision){
        if (IsServer){
			bool characterCollision = (collision.gameObject.GetComponentInParent<Character>() != null);
			bool playerCollision = (collision.gameObject.GetComponentInParent<Player>() != null);
			bool samePlayerCollision = (playerCollision && (collision.gameObject.GetComponentInParent<Player>() == currentPlayer));

			if (characterCollision){
				if (samePlayerCollision){
					return;
				}
				
				Debug.Log("bomb is colliding with character!");
				collision.gameObject.GetComponentInParent<Character>().TakeDamage(1);
			}
		}
    }*/
	
	public void OnTriggerEnter2D(Collider2D collision){
        if (IsServer){
			bool characterCollision = (collision.gameObject.GetComponentInParent<Character>() != null);
			bool playerCollision = (collision.gameObject.GetComponentInParent<Player>() != null);
			bool samePlayerCollision = (playerCollision && (collision.gameObject.GetComponentInParent<Player>() == currentPlayer));

			if (characterCollision){
				if (samePlayerCollision){
					return;
				}
				
				Debug.Log("bomb is colliding with character!");
				collision.gameObject.GetComponentInParent<Character>().TakeDamage(1);
			}
		}
    }

    private IEnumerator WaitToDestroy(float seconds)
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