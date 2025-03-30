using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;

public class NetRope : NetworkComponent
{
    private GameObject pivot;
    public Rigidbody2D pivotRig;
    public Transform swingPos;    
    private PlayerController player;
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
    [SerializeField] private float baseYJumpForce = 4f;
    [SerializeField] public float swingSnapMult = 1f;
    [SerializeField] public float dirChangeTorque = 2f;

    public override void HandleMessage(string flag, string value)
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {        
        player = FindObjectOfType<PlayerController>();
        pivot = transform.GetChild(0).gameObject;
        pivotRig = pivot.GetComponent<Rigidbody2D>();
    }

    public override void NetworkedStart()
    {
        
    }

    public void GrabRope(PlayerController player)
    {                
        player.grabbedRope = this;
        player.swingPos = ClosestSwingPos(player);        
        playerPresent = true;

        //calculate initial torque using player speed variables. don't use player rigidbody velocity since that's used to connect to rope
        float playerTorque = Mathf.Max(player.GetSpeed(), Mathf.Abs(player.launchVec.x));
        if (player.myRig.velocity.x < 0)
            playerTorque *= -1;
        
        pivotRig.angularVelocity += playerTorque * initialTorqueMult;
    }

    private Transform ClosestSwingPos(PlayerController player)
    {
        float minDist = Mathf.Infinity;
        int minDistIdx = -1;                

        //assuming rope is at idx 0
        for (int i = 1; i < pivot.transform.childCount; i++)
        {
            float distToPos = Vector2.Distance(player.transform.position, pivot.transform.GetChild(i).position);
            if (distToPos < minDist)
            {
                minDist = distToPos;
                minDistIdx = i;
            }
        }
        //to make player have less velocity the higher they are on the rope, use the index of the swingPos as a multiplier
        player.swingPosHeight = minDistIdx;
        return pivot.transform.GetChild(minDistIdx);
    }
    
    public void BoostPlayer(PlayerController player)
    {
        float ropeAngSpeed = Mathf.Abs(player.grabbedRope.transform.GetChild(0).GetComponent<Rigidbody2D>().angularVelocity);
        float pivotRotZ = player.grabbedRope.transform.GetChild(0).localEulerAngles.z;
        if (pivotRotZ > 180f)
            pivotRotZ -= 180f;
        
        // if at lowest height (height=1), multiply by 1. if at 2nd lowest (height=2), get 90% speed, then 80%, etc
        float xJumpVel = (Mathf.Cos(pivotRotZ * Mathf.Deg2Rad) * xJumpForce * ropeAngSpeed) * (1.1f - 0.1f * player.swingPosHeight);
        float yJumpVel = baseYJumpForce + (Mathf.Sin(pivotRotZ * Mathf.Deg2Rad) * extraYJumpForce * ropeAngSpeed * (1.1f - 0.1f * player.swingPosHeight));        
        Vector2 launchVector = new Vector2(xJumpVel, yJumpVel);

        player.swingPosHeight = 0;
        player.myRig.velocity = launchVector;
        player.launchVec = launchVector;
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
    void Update()
    {
        if (IsServer)
        {
            float angFromCenter;
            float ropeRotZ = pivot.transform.localEulerAngles.z;

            // rope on right side
            if (ropeRotZ > deadzoneLength && ropeRotZ < 180f)
            {
                angFromCenter = ropeRotZ;                
                pivotRig.angularVelocity += -fallStrength - (angFromCenter * heightFallInfluence);
            }
            // rope on left side
            else if (ropeRotZ > 180f && ropeRotZ < (360 - deadzoneLength))
            {
                angFromCenter = ropeRotZ - 360f;                                
                pivotRig.angularVelocity += fallStrength - (angFromCenter * heightFallInfluence);
            }            

            if (player == null)
                return;

            if (!playerPresent || player.holdingDir == "none")            
                pivotRig.angularVelocity *= (1 - (Time.deltaTime * slowdownMult));            

            if (player.holdingDir == "left" && playerPresent)
            {                
                pivotRig.angularVelocity += -playerTorque;
            }
            else if (player.holdingDir == "right" && playerPresent)
            {                
                pivotRig.angularVelocity += playerTorque;
            }            
        }        
    }            
}
