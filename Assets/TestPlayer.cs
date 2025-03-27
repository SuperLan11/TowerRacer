using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    private Rigidbody2D rig;
    public ManualRope grabbedRope = null;
    private string state = "NORMAL";

    public Vector3 vineVel;
    private Transform swingPos;
    private bool canGrabRope = true;
    private float launchVel;
    private Vector2 launchVec;

    public string holdingDir = "";
    private bool isGrounded = false;

    public float forceAmt = 20f;
    public float maxSpeed = 20f;    

    // Start is called before the first frame update
    void Start()
    {
        rig = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.name == "Floor")
        {
            isGrounded = true;
            state = "NORMAL";
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {        
        if (collision.gameObject.name.Contains("Rope") && state == "NORMAL" && canGrabRope)
        {
            grabbedRope = collision.gameObject.GetComponentInParent<ManualRope>();
            grabbedRope.playerPresent = true;
            //this needs to be in this order to prevent null error in Update!!
            swingPos = grabbedRope.swingPos;
            state = "SWINGING";
        }
    }

    private IEnumerator GrabCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        canGrabRope = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = Vector2.zero;

        if(Input.GetKey((KeyCode.D)))
        {
            input.x += 1;
            holdingDir = "left";
        }
        if (Input.GetKey((KeyCode.A)))
        {
            input.x -= 1;
            holdingDir = "right";
        }
        if (Input.GetKey((KeyCode.W)))
        {
            input.y += 1;            
        }
        if (Input.GetKey((KeyCode.S)))
        {
            input.y -= 1;            
        }        

        if(input.x == 0)
        {
            holdingDir = "";
        }

        if (state == "SWINGING")
        {
            rig.velocity = Vector3.zero;
            transform.position = swingPos.position;
            if(Input.GetKeyDown(KeyCode.Space))
            {
                canGrabRope = false;
                StartCoroutine(GrabCooldown(1f));
                state = "LAUNCHING";

                float ropeAngVel = grabbedRope.transform.GetChild(0).GetComponent<Rigidbody2D>().angularVelocity;
                float pivotRotZ = grabbedRope.transform.GetChild(0).localEulerAngles.z;                
                if (pivotRotZ > 180f)                
                    pivotRotZ -= 180f;                

                /*Debug.Log("ropeAngVel: " + ropeAngVel);
                Debug.Log("pivotRotZ: " + pivotRotZ);*/
                /*Debug.Log("xJumpForce: " + xJumpForce);
                Debug.Log("yJumpForce: " + yJumpForce);*/
                //Debug.Log("pivotRotZ: " + pivotRotZ);

                float xJumpForce = Mathf.Cos(pivotRotZ * Mathf.Deg2Rad) * forceAmt;
                float yJumpForce = Mathf.Sin(pivotRotZ * Mathf.Deg2Rad) * forceAmt;                
                Vector2 launchVector = new Vector2(xJumpForce, yJumpForce);
                //launchVel = launchVector.magnitude;

                //rig.velocity += launchVector;
                Vector2 forcePos = transform.position;
                Vector2 downDir = new Vector2(transform.up.x, transform.up.y) * -1;
                forcePos += downDir * GetComponent<Collider2D>().bounds.size.y;
                rig.velocity += launchVector;
                launchVec = launchVector;
                //rig.AddForceAtPosition(launchVector, forcePos);                
                grabbedRope.playerPresent = false;
                grabbedRope = null;
                Debug.Log("velY: " + rig.velocity.y);
            }
        }
        else if(state == "LAUNCHING")
        {            
            Vector2 newVel = rig.velocity;
            newVel.x += input.x * 100f * Time.deltaTime;
            newVel.x = Mathf.Clamp(newVel.x, -maxSpeed, maxSpeed);

            rig.velocity = newVel;
            //Debug.Log("xVel: " + rig.velocity.x);
        }
        else if(state == "NORMAL")
        {           
            rig.velocity = new Vector2(input.x, 0) * 5 + new Vector2(0, rig.velocity.y);
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rig.velocity += new Vector2(0, 8);
                isGrounded = false;
            }
        }        
    }
}
