using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;

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
				getItemSfx.Play();
				GetComponent<Collider2D>().enabled = false;				
				spriteRender.enabled = false;
			}
		}		
		else if(flag == "TRIGGER")
        {
			if(IsClient)
            {
				GetComponent<Collider2D>().isTrigger = true;
				GetComponent<Rigidbody2D>().gravityScale = 0f;
				GetComponent<Rigidbody2D>().velocity = Vector2.zero;
				GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
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

	private void OnCollisionEnter2D(Collision2D collision)
    {		
		if (IsServer)
		{
			if (itemUI == null)
			{				
				return;
			}

			Player playerHit = collision.gameObject.GetComponentInParent<Player>();
			bool enemyHit = collision.gameObject.GetComponent<Enemy>() != null;

			if (playerHit != null || enemyHit)
			{
				GetComponent<Collider2D>().isTrigger = true;
				GetComponent<Rigidbody2D>().gravityScale = 0f;
				GetComponent<Rigidbody2D>().velocity = Vector2.zero;
				GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
				SendUpdate("TRIGGER", "");
			}

			if (playerHit == null)
			{
				Debug.Log("player hit was null");
				return;
			}

			bool hasItem = playerHit.hasBomb || playerHit.hasChicken || playerHit.hasSpeedBoost;
			if (playerHit != null && !hasItem)
			{
				Debug.Log("player hit without item!");
				//items are in the following order: 0-chicken, 1-speedbost, 2-bomb
				int randIdx = Random.Range(0, itemPrefabs.Length);
				playerHit.SendUpdate("ITEM", randIdx.ToString());

				//wait to destroy item box so that sfx can play, so need to play sfx on server
				getItemSfx.Play();
				GetComponent<Collider2D>().enabled = false;
				spriteRender.enabled = false;
				SendUpdate("GET_BOX", "");
				StartCoroutine(DestroyAfterSfx());
			}
			else
            {
				Debug.Log("playerHit: " + (playerHit != null));
				Debug.Log("hasItem: " + hasItem);
			}
		}
	}

	//need two collision functions for chest since it changes to a trigger when it hits the ground
    private void OnTriggerEnter2D(Collider2D collision)
    {
		if (IsServer)
		{			
			Player playerHit = collision.gameObject.GetComponentInParent<Player>();

			if (playerHit == null)
				return;

			bool hasItem = playerHit.hasBomb || playerHit.hasChicken || playerHit.hasSpeedBoost;
			if (playerHit != null && !hasItem)
			{
				//items are in the following order: 0-chicken, 1-speedbost, 2-bomb
				int randIdx = Random.Range(0, itemPrefabs.Length);
				playerHit.SendUpdate("ITEM", randIdx.ToString());

				//wait to destroy item box so that sfx can play, so need to play sfx on server
				getItemSfx.Play();
				GetComponent<Collider2D>().enabled = false;
				spriteRender.enabled = false;
				SendUpdate("GET_BOX", "");
				StartCoroutine(DestroyAfterSfx());
			}
		}
	}

    private IEnumerator DestroyAfterSfx()
    {
		while (getItemSfx.isPlaying)
		{
			yield return new WaitForSeconds(0.1f);
		}

		MyCore.NetDestroyObject(this.NetId);
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