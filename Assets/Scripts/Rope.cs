using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class Rope : NetworkComponent
{
    private Rigidbody2D myRig;
    public HingeJoint2D hangJoint;
    public HingeJoint2D grabJoint;
    public GameObject grabSpot;
    private float initialMinAngle;
    private float initialMaxAngle;

    public Transform swingLoc;

    public override void HandleMessage(string flag, string value)
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void NetworkedStart()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsServer)
        {
            Debug.Log("rope hit something");
            if (collision.gameObject.GetComponent<PlayerController>() != null)
            {
                collision.gameObject.GetComponent<PlayerController>().GrabRope(this);    
            }
        }
    }

    public override IEnumerator SlowUpdate()
    {
        yield return new WaitForSeconds(0.05f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
