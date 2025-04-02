using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public abstract class Character : NetworkComponent
{
    //sync vars
    protected int health;        
    [SerializeField] protected float speed;

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    //non-sync vars
    protected int attackCooldown;
    protected int maxAttackCooldown;
    // public for testing
    public Rigidbody2D myRig;

    protected Sprite sprite;
    protected SpriteRenderer spriteRender;    

    protected void Attack()
    {
        //do something
    }

    //has HandleMessage    
    //has NetworkedStart()
    //has SlowUpdate()
}
