using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//this is not supposed to be good code for testing reasons
public class TestPlayer : MonoBehaviour
{
    public Rigidbody2D rig;
    public Rope grabbedRope = null;
    public string state = "NORMAL";
    [SerializeField] private float speed = 5f;
    
    public Transform swingPos;
    public bool canGrabRope = true;    
    public Vector2 launchVec;

    public string holdingDir = "";
    private bool isGrounded = false;
    public float jumpStrength = 8f;

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
        if (collision.gameObject.name.Contains("Rope") && canGrabRope)
        {
            //using a wrapper so all rope variables can be modified on the rope script
            //collision.gameObject.GetComponentInParent<Rope>().GrabRope(this);
        }
    }

    public void MouseMove(InputAction.CallbackContext mm)
    {
        Vector2 delta = mm.ReadValue<Vector2>();
        Debug.Log("mouse delta: " + delta);
        if (mm.started)
        {
            Debug.Log("mouse delta started");
        }
        else if(mm.performed)
        {
            Debug.Log("mouse delta performed");
        }
        else if(mm.canceled)
        {
            Debug.Log("mouse delta canceled");
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
            holdingDir = "right";
        }
        if (Input.GetKey((KeyCode.A)))
        {
            input.x -= 1;
            holdingDir = "left";
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

                //grabbedRope.BoostPlayer(this);
                grabbedRope.playerPresent = false;
                grabbedRope = null;
            }
        }
        else if(state == "LAUNCHING")
        {            
            Vector2 newVel = rig.velocity;
            newVel.x += input.x;
            //launchVec.x = Mathf.Max(launchVec.x, speed);
            float allowedXSpeed = Mathf.Max(launchVec.x, speed);
            newVel.x = Mathf.Clamp(newVel.x, -allowedXSpeed, allowedXSpeed);

            rig.velocity = newVel;            
        }
        else if(state == "NORMAL")
        {           
            rig.velocity = new Vector2(input.x, 0) * speed + new Vector2(0, rig.velocity.y);
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rig.velocity += new Vector2(0, jumpStrength);
                isGrounded = false;
            }
        }        
    }
}
