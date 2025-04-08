/*
@Authors - Patrick
@Description - Sword is in motion by default, and will stop being in motion when coming in contact with anything
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NETWORK_ENGINE;

public class Sword : NetworkComponent
{
    public override void HandleMessage(string flag, string value)
    {
        //throw new System.NotImplementedException();
    }

    public override void NetworkedStart()
    {
        //throw new System.NotImplementedException();
    }

    public override IEnumerator SlowUpdate(){
        while (IsConnected){
            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    void OnCollisionEnter2D(Collision2D collision){
        if (IsServer){
            Debug.Log("this prints");
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }
    }
}