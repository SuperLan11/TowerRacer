/*
@Authors - Patrick
@Description - Sword is in motion by default, and will stop being in motion when coming in contact with anything
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NETWORK_ENGINE;

public class RopeArrow : NetworkComponent
{
    private const int ROPE_SPAWN_PREFAB_INDEX = Idx.ROPE; //7; 
    private AudioSource spawnRopeSfx;
    
    public override void HandleMessage(string flag, string value)
    {
        if(flag == "HIDE")
        {
            if(IsClient)
            {
                spawnRopeSfx.Play();
                GetComponent<SpriteRenderer>().enabled = false;
                GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    private void Start()
    {
        spawnRopeSfx = GetComponent<AudioSource>();
    }

    public override void NetworkedStart()
    {
        GetComponent<Rigidbody2D>().gravityScale = 0f;
    }

    public override IEnumerator SlowUpdate(){
        while (IsConnected){
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    /*
    void OnCollisionEnter2D(Collision2D collision){
        if (IsServer){
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;

            MyCore.NetCreateObject(ROPE_SPAWN_PREFAB_INDEX, Owner, this.transform.position, Quaternion.identity);
            MyCore.NetDestroyObject(this.NetId);
        }
    }
    */

    private IEnumerator DestroyAfterSfx()
    {
        while(spawnRopeSfx.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }

        MyCore.NetDestroyObject(this.NetId);
    }

    void OnTriggerEnter2D (Collider2D collider){
        if (IsServer){            
            MyCore.NetCreateObject(ROPE_SPAWN_PREFAB_INDEX, Owner, this.transform.position, Quaternion.identity);

            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<Collider2D>().enabled = false;
            SendUpdate("HIDE", "");

            spawnRopeSfx.Play();
            StartCoroutine(DestroyAfterSfx());
        }
    }
}