using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : Enemy
{
    int dir = 1;    
    float leftmostX = -Mathf.Infinity;
    float rightmostX = Mathf.Infinity;    
    private STATE state = STATE.MOVING;
    [SerializeField] private float shootTime = 1f;
    [SerializeField] private float sightRange = 10f;
    private bool canShoot = true;
    private int ARROW_IDX = 2;
    //private bool playerSighted = false;
    private List<Player> playersInRange = new List<Player>();

    enum STATE
    {
        MOVING,
        SHOOTING
    }

    public override void HandleMessage(string flag, string value)
    {        
        if (flag == "DEBUG")
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
        if(IsServer)
        {
            MyCore.NetCreateObject(1, Owner, new Vector3(-5, 1, 0), Quaternion.identity);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.tag == "FLOOR")
            {
                leftmostX = collision.collider.bounds.min.x;
                rightmostX = collision.collider.bounds.max.x;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsServer)
        {
            Player playerHit = collision.gameObject.GetComponentInParent<Player>();
            if (playerHit != null && !playersInRange.Contains(playerHit))
            {
                if (playersInRange.Count == 0)
                {
                    state = STATE.SHOOTING;
                    FacePlayer(playerHit);
                }

                playersInRange.Add(playerHit);
                Debug.Log("adding player. playersInRange: " + playersInRange.Count);
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
                Debug.Log("removing player. playersInRange: " + playersInRange.Count);
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
            spriteRender.flipX = false;
        }
        else if (player.transform.position.x < transform.position.x)
        {
            dir = -1;
            spriteRender.flipX = true;
        }
    }

    protected override void Move()
    {
        myRig.velocity = new Vector2(dir * speed, myRig.velocity.y);
        if (myRig.position.x > rightmostX)
        {
            dir *= -1;
            transform.position = new Vector2(rightmostX - speed / 10, transform.position.y);
        }
        else if (myRig.position.x < leftmostX)
        {
            dir *= -1;
            transform.position = new Vector2(leftmostX + speed / 10, transform.position.y);
        }
    }

    /*private bool PlayerSighted()
    {        
        Vector2 rayDir = new Vector3(dir, 0);
        Vector2 rayOrigin = transform.position;
        rayOrigin.x += dir * GetComponent<Collider2D>().bounds.size.x;

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDir, sightRange);
        if (hit.collider != null && hit.collider.GetComponentInParent<Player>() != null)
        {            
            return true;
        }
        else
        {
            state = STATE.MOVING;
            return false;
        }
    }*/

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

        GameObject arrow = MyCore.NetCreateObject(ARROW_IDX, Owner, arrowPos, Quaternion.identity);
        arrow.GetComponent<Arrow>().dir = dir;

        arrow.transform.eulerAngles = new Vector3(0, 0, 90);
        Vector3 arrowRot = arrow.GetComponent<Arrow>().transform.eulerAngles;
        if (dir == -1)
            arrowRot.z += 180f;
        arrow.transform.eulerAngles = arrowRot;

        arrow.GetComponent<Arrow>().SendUpdate("SPAWN", dir.ToString() + ";" + arrowRot);
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

    // Update is called once per frame
    void Update()
    {
        if(IsServer)
        {
            //playerSighted = PlayerSighted();

            if (state == STATE.MOVING)
            {
                Move();
            }
            else if(state == STATE.SHOOTING)
            {
                myRig.velocity = new Vector2(0, myRig.velocity.y);
                if(canShoot)
                    Shoot();
            }
        }
    }
}
