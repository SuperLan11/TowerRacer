using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : Enemy
{    
    private STATE state = STATE.MOVING;
    [SerializeField] private float shootTime = 1f;    
    private bool canShoot = true;      
    private List<Player> playersInRange = new List<Player>();

    [SerializeField] private AudioSource shootArrowSfx;

    enum STATE
    {
        MOVING,
        SHOOTING
    }

    public override void HandleMessage(string flag, string value)
    {
        if (flag == "FLIP")
        {
            if (IsClient)
            {
                spriteRender.flipX = bool.Parse(value);
            }
        }
        else if(flag == "SHOOT_SFX")
        {
            if(IsClient)
            {
                shootArrowSfx.Play();
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
        else if (flag == "START_HIT_EFFECT")
        {
            if (IsClient)
            {
                StartHitEffect(hitColor);
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
        health = 2;        

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

            /*if(collision.gameObject.GetComponent<Enemy>() != null)
            {
                dir *= -1;
                spriteRender.flipX = !spriteRender.flipX;
                SendUpdate("FLIP", spriteRender.flipX.ToString());
            }*/
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsServer)
        {            
            Player playerHit = collision.gameObject.GetComponentInParent<Player>();
            //Debug.Log("was player hit: " + (playerHit != null));
            if (playerHit != null && !playersInRange.Contains(playerHit))
            {
                if (playersInRange.Count == 0)
                {
                    state = STATE.SHOOTING;
                    FacePlayer(playerHit);
                }

                playersInRange.Add(playerHit);                
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsServer)
        {
            Player playerExited = collision.gameObject.GetComponentInParent<Player>();
            if (playerExited != null && playersInRange.Contains(playerExited))
            {
                playersInRange.Remove(playerExited);

                if (playersInRange.Count > 0)
                {
                    Player closestPlayer = ClosestPlayer();
                    FacePlayer(closestPlayer);
                }
                else if(playersInRange.Count == 0)
                {
                    state = STATE.MOVING;
                }                
            }
        }
    }

    private Player ClosestPlayer()
    {
        float minDist = Mathf.Infinity;
        int minIdx = -1;
        Player[] players = FindObjectsOfType<Player>();
        for (int i = 0; i < players.Length; i++)
        {
            float distToPlayer = Vector2.Distance(transform.position, players[i].transform.position);
            if (distToPlayer < minDist)
            {
                minDist = distToPlayer;
                minIdx = i;
            }
        }
        return players[minIdx];
    }

    private void FacePlayer(Player player)
    {        
        if (player.transform.position.x > transform.position.x)
        {
            dir = 1;
            SendUpdate("FLIP", false.ToString());
            spriteRender.flipX = false;
        }
        else if (player.transform.position.x < transform.position.x)
        {
            dir = -1;
            SendUpdate("FLIP", true.ToString());
            spriteRender.flipX = true;
        }
    }    

    private IEnumerator ShootCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        canShoot = true;
    }

    protected void Shoot()
    {
        canShoot = false;
        StartCoroutine(ShootCooldown(shootTime));
        Vector2 arrowPos = transform.position;
        arrowPos.x += dir * GetComponent<Collider2D>().bounds.size.x;    
        Quaternion arrowDirection = (dir == 1 ? Quaternion.Euler(0f, 0f, 0f) : Quaternion.Euler(0f, 0f, 180f));     

        GameObject arrow = MyCore.NetCreateObject(Idx.SKELETON_ARROW, Owner, arrowPos, arrowDirection);
        arrow.GetComponent<Arrow>().dir = dir;
        arrow.GetComponent<Arrow>().SendUpdate("SPAWN", "");
        SendUpdate("SHOOT_SFX", "");        
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
        if(IsServer)
        {
            if(GameManager.inCountdown)
            {
                myRig.velocity = Vector2.zero;
            }
            else if (state == STATE.MOVING)
            {
                Move();
            }
            else if(state == STATE.SHOOTING)
            {
                myRig.velocity = new Vector2(0, myRig.velocity.y);
                if(canShoot)
                    Shoot();
            }
            /*Debug.Log("state: " + state);
            Debug.Log("playersInRange: " + playersInRange.Count);*/
        }
    }
}
