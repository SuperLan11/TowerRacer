using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public abstract class Item : NetworkComponent
{
    //non-sync vars
    protected Sprite sprite;
    protected SpriteRenderer spriteRender;

    // Start is called before the first frame update
    private void Start()
    {
        sprite = GetComponent<Sprite>();
        spriteRender = GetComponent<SpriteRenderer>();
    }

    private void UseItem()
    {

    }
}
