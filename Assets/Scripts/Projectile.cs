using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public abstract class Projectile : NetworkComponent
{
	[System.NonSerialized] public Rigidbody2D myRig;
    protected Sprite sprite;
	[System.NonSerialized] public SpriteRenderer spriteRender;
	protected Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

	// Start is called before the first frame update
	private void Start()
	{
		myRig = GetComponent<Rigidbody2D>();		
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
}
