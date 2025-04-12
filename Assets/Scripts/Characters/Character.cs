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
    protected bool inAttackCooldown;
    protected float ATTACK_COOLDOWN_DURATION;
    protected int MAX_HEALTH;

    protected Animator anim;
    
    [System.NonSerialized] public Rigidbody2D myRig;

    protected Sprite sprite;
    //this is serialized to prevent null reference exceptions if an enemy has to flip as soon as they spawn
    [SerializeField] protected SpriteRenderer spriteRender;
    protected Material regularMaterial;
    [SerializeField] protected Material hitMaterial;
    protected Color hitColor = Color.red;

    protected Coroutine hitCoroutine;  
    protected const float HIT_EFFECT_DURATION = 0.25f; 


    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    protected virtual void Attack()
    {
        //do something
    }

    public abstract void TakeDamage(int damage);

    //because we're not using sync vars for the coroutine, these two methods will actually run on the client only since they're for a visual effect
    protected void StartHitEffect(Color color){
        //prevents multiple of the same coroutine from running
        if (hitCoroutine != null){
            StopCoroutine(hitCoroutine);
        }

        hitCoroutine = StartCoroutine(HitRoutine(color));
    }

    protected IEnumerator HitRoutine(Color color){
        spriteRender.material = hitMaterial;
        hitMaterial.color = color;

        yield return new WaitForSeconds(HIT_EFFECT_DURATION);

        spriteRender.material = regularMaterial;
        hitCoroutine = null;
    }
}