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
        health = 1;
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
        Vector2 feetPos = GetComponent<Collider2D>().bounds.min;
        feetPos.x = transform.position.x;
        bool floorBelow = Physics2D.Raycast(feetPos, Vector2.down, 0.1f, floorLayer);

        if (!floorBelow && !raycastingPaused)
        {
            dir *= -1;
            //transform.position += new Vector3(dir / 2, 0, 0);
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

    protected float GetTileUpperY(RaycastHit2D hit)
    {        
        Tilemap tilemap = hit.collider.GetComponent<Tilemap>();
        if (tilemap == null)
            return -1;

        Vector3 hitWorldPos = hit.point;
        Vector3Int cellPosition = tilemap.WorldToCell(hitWorldPos);

        Vector3 tileWorldPos = tilemap.CellToWorld(cellPosition);
        float tileHeight = tilemap.cellSize.y;

        return tileWorldPos.y + tileHeight;
    }

    public override void TakeDamage(int damage){
        health -= damage;        

        if (health <= 0){            
            //enemy drops item box on death
            MyCore.NetCreateObject(Idx.ITEM_BOX, Owner, this.transform.position, Quaternion.identity);
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
            bool hitWall = collider.gameObject.tag == "WALL";
            bool hitEnemy = collider.gameObject.GetComponent<Enemy>() != null;
            ItemBox itemHit = collider.gameObject.GetComponent<ItemBox>();

            if (hitPlayer)
                collider.gameObject.GetComponentInParent<Player>().TakeDamage(1);

            if (hitPlayer || hitWall || hitEnemy)
            {
                dir *= -1;
                spriteRender.flipX = !spriteRender.flipX;
                SendUpdate("FLIP", spriteRender.flipX.ToString());
                StartCoroutine(PauseRaycasting(0.3f));
            }

            if (itemHit != null)
            {
                //sometimes chests don't turn into triggers on hitting ground, so fix when enemy collides with it
                itemHit.GetComponent<Collider2D>().isTrigger = true;
            }
        }
    }
}