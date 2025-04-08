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
    private const int ROPE_SPAWN_PREFAB_INDEX = 7;
    
    public override void HandleMessage(string flag, string value)
    {
        //throw new System.NotImplementedException();
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

    void OnCollisionEnter2D(Collision2D collision){
        if (IsServer){
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;

            MyCore.NetCreateObject(ROPE_SPAWN_PREFAB_INDEX, Owner, this.transform.position, Quaternion.identity);
            MyCore.NetDestroyObject(this.NetId);
        }
    }
}