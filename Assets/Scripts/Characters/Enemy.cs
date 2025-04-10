/*
@Authors - Landon
@Description - Abstract class for enemies.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class Enemy : Character
{    
    protected Player[] players;
    protected LayerMask floorLayer;
    protected LayerMask jumpThruLayer;
    protected int dir = 1;
    protected bool raycastingPaused = false;    

    // Start is called before the first frame update
    private void Start()
    {
        myRig = GetComponent<Rigidbody2D>();
        
        //serialize this in the prefab for each enemy to prevent null reference exceptions
        //spriteRender = GetComponent<SpriteRenderer>();

        sprite = spriteRender.sprite;
        players = FindObjectsOfType<Player>();
        //combine both Floor and JumpThru so enemy can turn around on both types of platforms
        floorLayer = LayerMask.GetMask("Floor", "JumpThru");        

        anim = GetComponent<Animator>();

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

        if (raycastingPaused)
            return;

        // if(floorBelow)
        // {
        //     Debug.Log("floor raycasted!");
        // }

        if (!floorBelow && dir == 1)
        {
            dir = -1;
            spriteRender.flipX = !spriteRender.flipX;
            SendUpdate("FLIP", true.ToString());
            StartCoroutine(PauseRaycasting(0.1f));
            //transform.position -= new Vector3(speed / 5, 0, 0);
        }
        else if (!floorBelow && dir == -1)
        {
            dir = 1;
            spriteRender.flipX = !spriteRender.flipX;
            SendUpdate("FLIP", false.ToString());
            StartCoroutine(PauseRaycasting(0.1f));
            //transform.position += new Vector3(speed / 5, 0, 0);
        }        
    }
    protected IEnumerator PauseRaycasting(float seconds)
    {
        raycastingPaused = true;
        yield return new WaitForSeconds(seconds);
        raycastingPaused = false;
    }

    public override void TakeDamage(int damage){
        health -= damage;
        
        if (health <= 0){
            MyCore.NetDestroyObject(this.NetId);
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collider){
        if (IsServer){
            if (collider.gameObject.GetComponentInParent<Player>() != null){
                collider.gameObject.GetComponentInParent<Player>().TakeDamage(1);
            }
        }
    }
}