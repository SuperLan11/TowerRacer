using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public abstract class Character : NetworkComponent
{
    //sync vars
    protected int health;        
    [SerializeField] protected float speed;

    //non-sync vars
    protected int attackCooldown;
    protected int maxAttackCooldown;
    protected Rigidbody2D myRig;

    protected void Attack()
    {
        //do something
    }

    //has HandleMessage    
    //has NetworkedStart()
    //has SlowUpdate()
}
