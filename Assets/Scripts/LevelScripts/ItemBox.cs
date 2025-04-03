using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class ItemBox : NetworkComponent
{
	public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

	private Sprite sprite;
	private SpriteRenderer spriteRender;
	
	private GameObject itemUI;
	[SerializeField] private const int NUM_ITEMS = 3;
	[SerializeField] private GameObject[] itemPrefabs;

	private bool doDestroy = false;

	public override void HandleMessage(string flag, string value)
	{
		if(flag == "DEL")
        {
			if(IsServer)
            {
				Debug.Log("destroying item box");
				MyCore.NetDestroyObject(this.NetId);
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
				SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
			}
		}
	}

	private void Start()
	{
		spriteRender = GetComponent<SpriteRenderer>();
		sprite = spriteRender.sprite;

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
		itemUI = GameObject.FindGameObjectWithTag("ITEM_UI");
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
		if (IsClient)
		{
			PlayerController playerHit = collision.GetComponentInParent<PlayerController>();
			if (playerHit != null && playerHit.IsLocalPlayer)
			{
				int randIdx = Random.Range(0, NUM_ITEMS);
				//only spawn the player's item on that player's screen
				//GameObject itemImage = Instantiate(itemPrefabs[randIdx], itemUI.transform.position, Quaternion.identity);
				GameObject itemImage = Instantiate(itemPrefabs[2], itemUI.transform.position, Quaternion.identity);
				playerHit.hasBomb = true;

				itemImage.transform.SetParent(itemUI.transform);

				//bomb
				if (randIdx == 2)
					playerHit.hasBomb = true;
			}
		}
		
		if (IsServer)
		{
			//wait to destroy item box so that clients can catch up and touch it before it is destroyed
			// maybe not the cleanest solution, but hopefully it works well enough
			// checking isclient in triggerenter didn't work because can't send command because Owner is from game manager, which creates the itemboxes
			// checking isclient and isserver without a delay didn't work because server destroys item box before client touches it			

			//fix later to only despawn when specifically players touch the box
			StartCoroutine(WaitToDestroyBox(0.1f));
		}
    }

	private IEnumerator WaitToDestroyBox(float seconds)
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