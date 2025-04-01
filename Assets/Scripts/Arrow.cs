using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Arrow : NetworkComponent
{
	public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();
	public float speed = 10f;
	private Rigidbody2D myRig;
	public int dir = 0;

	public override void HandleMessage(string flag, string value)
	{
		if (flag == "SPAWN")
		{
			if (IsClient)
			{
				string[] vars = value.Split(";");
				dir = int.Parse(vars[0]);
				Vector3 rot = NetworkCore.Vector3FromString(vars[1]);
				transform.eulerAngles = rot;
			}
		}
		else if (flag == "DEBUG")
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

	private void Start()
	{
		myRig = GetComponent<Rigidbody2D>();

		if (GetComponent<NetworkRB2D>() != null)
			OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
		else if (GetComponentInChildren<NetworkRB2D>() != null)
			OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
		else if (GetComponent<NetworkTransform>() != null)
			OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
		else if (GetComponentInChildren<NetworkTransform>() != null)
			OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;
	}

	public override void NetworkedStart()
	{

	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(IsServer)
        {
			MyCore.NetDestroyObject(this.NetId);
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
    private void Update()
    {
        if(IsServer)
        {
			myRig.velocity = new Vector2(dir * speed, 0);
        }
    }
}
