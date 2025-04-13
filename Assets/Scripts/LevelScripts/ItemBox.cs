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

	private AudioSource getItemSfx;

	public override void HandleMessage(string flag, string value)
	{
		if(flag == "GET_BOX")
        {
			if (IsClient)
			{
				Debug.Log("play get item sfx");
				getItemSfx.Play();
				GetComponent<Collider2D>().enabled = false;
				spriteRender.enabled = false;
			}
		}
		else if (flag == "ITEM_UI")
		{
			if (IsClient)
			{
				string[] args = value.Split(";");
				Vector2 itemBoxPos = Player.Vector2FromString(args[0]);
				int itemIdx = int.Parse(args[1]);

				Player playerHit = Player.ClosestPlayerToPos(itemBoxPos);
				if (playerHit.IsLocalPlayer)
				{
					GameObject itemImage = Instantiate(itemPrefabs[itemIdx], itemUI.transform.position, Quaternion.identity);
					itemImage.transform.SetParent(itemUI.transform);
				}
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

		getItemSfx = GetComponent<AudioSource>();
	}

	public override void NetworkedStart()
	{
		itemUI = GameObject.FindGameObjectWithTag("ITEM_UI");
	}	

	private void OnTriggerEnter2D(Collider2D collision)
    {		
		if (IsServer)
		{
			if (itemUI == null)
			{
				Debug.Log("item box collided before setting item ui!");
				return;
			}

			Player playerHit = collision.GetComponentInParent<Player>();
			
			//not accurate for hasItem since on server			
			//bool hasItem = itemUI.transform.childCount >= 1;
			bool hasItem = playerHit.HasItem();
			if (playerHit != null && !hasItem)
			{
				int randIdx = Random.Range(0, NUM_ITEMS);
				SendUpdate("ITEM_UI", this.transform.position.ToString());
				playerHit.SendUpdate("ITEM", randIdx.ToString());
				//MyCore.NetDestroyObject(this.NetId);
				
				//disable instead of destroying item box so that sfx can play
				GetComponent<Collider2D>().enabled = false;
				spriteRender.enabled = false;
				SendUpdate("GET_BOX", "");				
			}
		}
	}

	public static Bomb ClosestBombToPos(Vector2 pos)
    {
		Bomb[] bombs = FindObjectsOfType<Bomb>();
		float minDist = Mathf.Infinity;

		if (bombs.Length == 0)
		{
			Debug.LogWarning("ClosestBombToPos() found no bombs");
			return null;
		}

		int closestIdx = -1;
		for (int i = 0; i < bombs.Length; i++)
		{
			float distToPos = Vector2.Distance(bombs[i].transform.position, pos);
			if (distToPos < minDist)
			{
				minDist = distToPos;
				closestIdx = i;
			}
		}

		return bombs[closestIdx];
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