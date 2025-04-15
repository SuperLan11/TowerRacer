using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Slime : Enemy
{       
    public override void HandleMessage(string flag, string value)
    {
        if(flag == "FLIP")
        {
            if (IsClient)
            {
                spriteRender.flipX = bool.Parse(value);
            }
        }
        else if (flag == "START_HIT_EFFECT")
        {
            if (IsClient)
            {
                StartHitEffect(hitColor);
            }
        }
        else if (flag == "HIDE_HP")
        {
            int health = int.Parse(value);
            this.transform.GetChild(health).GetComponent<SpriteRenderer>().enabled = false;
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
                SendCommand(flag, value);
            }
        }
    }

    public override void NetworkedStart()
    {        
        spriteRender.flipX = true;
        health = 1;

        Vector2 belowFeet = transform.position;
        belowFeet.y -= GetComponent<Collider2D>().bounds.size.y;
        
        //teleport slime to platform below it to avoid fall off issues
        RaycastHit2D hit = Physics2D.Raycast(belowFeet, Vector2.down, Mathf.Infinity, floorLayer);        
        float standingY = GetTileUpperY(hit);
        Vector2 standingPos = transform.position;
        standingPos.y = standingY + GetComponent<Collider2D>().bounds.size.y / 2;
        transform.position = standingPos;        
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            base.OnCollisionEnter2D(collision);
            
            /*if (collision.gameObject.GetComponent<Enemy>() != null)
            {
                dir *= -1;
                spriteRender.flipX = !spriteRender.flipX;
                SendUpdate("FLIP", spriteRender.flipX.ToString());
            }*/
        }
    } 

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {
                SendUpdate("FLIP", spriteRender.flipX.ToString());
                IsDirty = false;
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (GameManager.inCountdown)
            {
                myRig.gravityScale = 0f;
                myRig.velocity = Vector2.zero;
            }
            else
            {
                myRig.gravityScale = 1f;
                Move();
            }
        }
    }
}
