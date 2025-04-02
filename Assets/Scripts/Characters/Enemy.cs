using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{    
    protected Player[] players;
    protected LayerMask floorLayer;
    protected int dir = 1;

    // Start is called before the first frame update
    private void Start()
    {
        myRig = GetComponent<Rigidbody2D>();
        spriteRender = GetComponent<SpriteRenderer>();
        sprite = spriteRender.sprite;
        players = FindObjectsOfType<Player>();
        floorLayer = LayerMask.GetMask("Floor");

        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;
    }

    protected void Move()
    {
        myRig.velocity = new Vector2(dir * speed, myRig.velocity.y);
        float playerHeight = GetComponent<Collider2D>().bounds.size.y;
        bool floorBelow = Physics2D.Raycast(transform.position, Vector2.down, playerHeight * 1.5f, floorLayer);
        
        if (!floorBelow && dir == 1)
        {
            dir = -1;
            spriteRender.flipX = true;
            SendUpdate("FLIP", true.ToString());
            transform.position -= new Vector3(speed / 10, 0, 0);
        }
        else if (!floorBelow && dir == -1)
        {
            dir = 1;
            spriteRender.flipX = false;
            SendUpdate("FLIP", false.ToString());
            transform.position += new Vector3(speed / 10, 0, 0);
        }
    }
}
