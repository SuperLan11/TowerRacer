using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    private GameObject pivot;
    private Rigidbody2D pivotRig;
    public Transform swingPos;
    //private TestPlayer player;
    private TestPlayer player;
    public bool playerPresent = false;

    [SerializeField] private GameObject rope;
    [SerializeField] private float slowdownMult;    
    [SerializeField] private float deadzoneLength = 10f;
    [SerializeField] private float fallStrength = 0.5f;
    [SerializeField] private float playerTorque = 1f;
    [SerializeField] private float heightFallInfluence = 1f;
    [SerializeField] private float initialTorqueMult = 12f;
    [SerializeField] private float xJumpForce = 0.07f;
    [SerializeField] private float extraYJumpForce = 0.02f;    

    // Start is called before the first frame update
    void Start()
    {
        //player = FindObjectOfType<TestPlayer>();
        player = FindObjectOfType<TestPlayer>();
        pivot = transform.GetChild(0).gameObject;
        pivotRig = pivot.GetComponent<Rigidbody2D>();
    }
    
    public void GrabRope(TestPlayer player)
    {
        player.grabbedRope = this;
        player.grabbedRope.playerPresent = true;
        transform.GetChild(0).GetComponent<Rigidbody2D>().AddTorque(player.rig.velocity.x * initialTorqueMult);
        //this needs to be in this order to prevent null error in player Update!!
        player.swingPos = swingPos;
        player.state = "SWINGING";
    }

    public void BoostPlayer(TestPlayer player)
    {
        float ropeAngSpeed = Mathf.Abs(player.grabbedRope.transform.GetChild(0).GetComponent<Rigidbody2D>().angularVelocity);
        float pivotRotZ = player.grabbedRope.transform.GetChild(0).localEulerAngles.z;
        if (pivotRotZ > 180f)
            pivotRotZ -= 180f;

        float xJumpVel = Mathf.Cos(pivotRotZ * Mathf.Deg2Rad) * xJumpForce * ropeAngSpeed;
        float yJumpVel = player.jumpStrength + Mathf.Sin(pivotRotZ * Mathf.Deg2Rad) * extraYJumpForce * ropeAngSpeed;
        Vector2 launchVector = new Vector2(xJumpVel, yJumpVel);

        player.rig.velocity += launchVector;
        player.launchVec = launchVector;        
    }

    // Update is called once per frame
    void Update()
    {
        float angFromCenter;
        float ropeRotZ = pivot.transform.localEulerAngles.z;

        // rope on right side
        if(ropeRotZ > deadzoneLength && ropeRotZ < 180f)
        {           
            angFromCenter = ropeRotZ;            
            pivotRig.AddTorque(-fallStrength - (angFromCenter * heightFallInfluence));
        }
        // rope on left side
        else if(ropeRotZ > 180f && ropeRotZ < (360 - deadzoneLength))
        {            
            angFromCenter = ropeRotZ - 360f;            
            pivotRig.AddTorque(fallStrength - (angFromCenter * heightFallInfluence));
        }        
        pivotRig.angularVelocity *= (1 - (Time.deltaTime*slowdownMult));

        if(player.holdingDir == "left" && playerPresent)
        {
            pivotRig.AddTorque(-playerTorque);                        
        }
        else if(player.holdingDir == "right" && playerPresent)
        {
            pivotRig.AddTorque(playerTorque);
        }        
    }
}
