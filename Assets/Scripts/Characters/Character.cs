using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public abstract class Character : NetworkComponent
{
    //sync vars
    public int health;        
    [SerializeField] protected float speed;    

    //non-sync vars
    protected int attackCooldown;
    protected int maxAttackCooldown;

    protected Animator anim;
    
    [System.NonSerialized] public Rigidbody2D myRig;

    protected Sprite sprite;
    protected SpriteRenderer spriteRender;

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    protected void Attack()
    {
        //do something
    }

    public abstract void TakeDamage(int damage);
}
