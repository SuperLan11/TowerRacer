using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class NetlessRope : MonoBehaviour
{
    private Rigidbody2D myRig;
    public HingeJoint2D hangJoint;
    public HingeJoint2D grabJoint;
    public GameObject grabSpot;
    private float initialMinAngle;
    private float initialMaxAngle;

    public GameObject testPlayer;
    private HingeJoint2D[] ropes;

    // Start is called before the first frame update
    void Start()
    {        
        ropes = GetComponentsInChildren<HingeJoint2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
         
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
