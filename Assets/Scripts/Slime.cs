using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Slime : Enemy
{
    int dir = 1;
    float leftmostX = -Mathf.Infinity;
    float rightmostX = Mathf.Infinity;

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
        else
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }

    }


    // Start is called before the first frame update
    void Start()
    {
        myRig = GetComponent<Rigidbody2D>();
    }    

    public override void NetworkedStart()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "FLOOR")
        {
            leftmostX = collision.collider.bounds.min.x;
            rightmostX = collision.collider.bounds.max.x;
        }
    }

    protected override void Move()
    {
        myRig.velocity = new Vector2(dir * speed, myRig.velocity.y);
        if (myRig.position.x > rightmostX)
        {
            dir = -1;
            spriteRender.flipX = true;
            transform.position = new Vector2(rightmostX - speed/10, transform.position.y);
        }
        else if (myRig.position.x < leftmostX)
        {
            dir = 1;
            spriteRender.flipX = false;
            transform.position = new Vector2(leftmostX + speed/10, transform.position.y);
        }
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
        if (IsServer)
        {
            Move();
        }
    }
}
