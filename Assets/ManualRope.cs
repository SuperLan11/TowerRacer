using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualRope : MonoBehaviour
{
    private GameObject pivot;
    private Rigidbody2D pivotRig;
    public Transform swingPos;
    private TestPlayer player;
    public bool playerPresent = false;

    [SerializeField] private GameObject rope;
    [SerializeField] private float slowdownMult;    
    [SerializeField] private float deadzoneLength = 10f;
    [SerializeField] private float fallStrength = 0.5f;
    [SerializeField] private float playerTorque = 1f;
    //[SerializeField] private float gravityMult = 1f;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<TestPlayer>();
        pivot = transform.GetChild(0).gameObject;
        pivotRig = pivot.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float angFromCenter = 0f;
        float ropeRotZ = pivot.transform.localEulerAngles.z;

        if(ropeRotZ > deadzoneLength && ropeRotZ < 180f)
        {           
            angFromCenter = ropeRotZ;                        
        }
        else if(ropeRotZ > 180f && ropeRotZ < (360 - deadzoneLength))
        {            
            angFromCenter = ropeRotZ - 360f;                        
        }
        pivotRig.AddTorque(-fallStrength * angFromCenter);
        pivotRig.angularVelocity *= (1 - (Time.deltaTime*slowdownMult));

        if(player.holdingDir == "left" && playerPresent)
        {
            pivotRig.AddTorque(playerTorque);
        }
        else if(player.holdingDir == "right" && playerPresent)
        {
            pivotRig.AddTorque(-playerTorque);
        }
    }
}
