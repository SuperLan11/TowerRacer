using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{
    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();
    protected Player[] players;

    // Start is called before the first frame update
    private void Start()
    {
        myRig = GetComponent<Rigidbody2D>();        
        spriteRender = GetComponent<SpriteRenderer>();
        sprite = spriteRender.sprite;
        players = FindObjectsOfType<Player>();

        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;
    }

    protected abstract void Move();    
}
