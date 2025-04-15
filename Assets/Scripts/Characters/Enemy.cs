/*
@Authors - Landon
@Description - Abstract class for enemies.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

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
        regularMaterial = spriteRender.material;
        //unity youtube man says this is necessary for preventing side effects
        hitMaterial = new Material(hitMaterial);
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
        float enemyHeight = GetComponent<Collider2D>().bounds.size.y;
        bool floorBelow = Physics2D.Raycast(transform.position, Vector2.down, enemyHeight * 1.5f, floorLayer);        

        if(!floorBelow && !raycastingPaused)
        {
            dir *= -1;
            raycastingPaused = true;
            StartCoroutine(PauseRaycasting(0.3f));
        }

        if (myRig.velocity.x > 0.01f && spriteRender.flipX)
        {
            spriteRender.flipX = false;
            SendUpdate("FLIP", false.ToString());
        }
        else if (myRig.velocity.x < -0.01f && !spriteRender.flipX)
        {
            spriteRender.flipX = true;
            SendUpdate("FLIP", true.ToString());
        }
    }
    protected IEnumerator PauseRaycasting(float seconds)
    {        
        yield return new WaitForSeconds(seconds);
        raycastingPaused = false;
    }

    public override void TakeDamage(int damage){
        health -= damage;
        this.transform.GetChild(health).GetComponent<SpriteRenderer>().enabled = false;
        SendUpdate("HIDE_HP", health.ToString());

        if (health <= 0){
            Debug.Log("enemy is dead");
            MyCore.NetDestroyObject(this.NetId);
        }else{
            SendUpdate("START_HIT_EFFECT", "GoodMorning");
            //StartHitEffect(hitColor);
        }
    }

    protected virtual void OnCollisionEnter2D(Collision2D collider){
        if (IsServer)
        {
            //consider doing for loop for collision contacts here
            bool hitPlayer = collider.gameObject.GetComponentInParent<Player>() != null;
            bool hitTilemap = collider.gameObject.GetComponent<TilemapCollider2D>() != null;
            bool hitEnemy = collider.gameObject.GetComponent<Enemy>() != null;

            if (hitPlayer)            
                collider.gameObject.GetComponentInParent<Player>().TakeDamage(1);

            //to prevent enemies from falling off their platform
            if (hitTilemap)
                myRig.gravityScale = 0f;

            if(hitPlayer || hitTilemap || hitEnemy)
            {
                dir *= -1;
                spriteRender.flipX = !spriteRender.flipX;
                SendUpdate("FLIP", spriteRender.flipX.ToString());
                StartCoroutine(PauseRaycasting(0.3f));
            }                        
        }
    }
}