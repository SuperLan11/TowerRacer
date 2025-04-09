/*
@Authors - Patrick
@Description - Speed boost powerup
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.Tilemaps;

public class SpeedBoost : Item
{
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
				SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
			}
		}
	}

	public override void NetworkedStart()
	{
		//
	}	

	public override IEnumerator SlowUpdate(){
		while (IsConnected){
			yield return new WaitForSeconds(0.05f);
		}
	}

    protected override void UseItem()
    {
        
    }

    private void Update(){
		
	}
}