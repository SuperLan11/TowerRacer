using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NETWORK_ENGINE;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerController : Character
{    
    //sync vars
    [System.NonSerialized] public Text PlayerName;    
    [System.NonSerialized] public int ColorSelected = -1;
    [System.NonSerialized] public string PName = "<Default>";
    public string state = "NORMAL";
    public NetRope grabbedRope;
    public string holdingDir = "";
    private bool canGrabRope = false;
    public Transform swingPos;
    public Vector2 launchVec;
    private bool isGrounded = true;
    // May not need these if we use inheritance for player classes
    //[System.NonSerialized] public SpriteRenderer spriteRender;
    //[System.NonSerialized] public Sprite sprite;

    //nonsync vars    
    private Color[] colors;
    private Vector2 lastMoveInput;          
    public float jumpStrength = 10f;
    public Dictionary<string, string> NET_FLAGS = new Dictionary<string, string>();    


    public override void HandleMessage(string flag, string value)
    {
        if (flag == "START")
        {
            if (IsClient)
            {
                PName = value.Split(";")[0];
                GetComponentInChildren<Text>().text = PName;
                ColorSelected = int.Parse(value.Split(";")[1]);

                //calling a function soon after creating object sometimes runs before Start
                Start();

                GetComponent<SpriteRenderer>().color = colors[ColorSelected];
            }
            else if (IsServer)
            {
                SendUpdate("START", value);
            }
        }
        else if (flag == "MOVE")
        {
            lastMoveInput = Vector2FromString(value);
            if (lastMoveInput.x > 0)
                holdingDir = "right";
            else if (lastMoveInput.x < 0)
                holdingDir = "left";

            if (IsServer)
            {
                SendUpdate("MOVE", value);
            }
        }
        else if (flag == "JUMP")
        {
            isGrounded = false;
            //initially called on server
            if (state == "SWINGING")
            {
                state = "FLYING";
                canGrabRope = false;
                state = "LAUNCHING";

                grabbedRope.BoostPlayer(this);
                grabbedRope.playerPresent = false;
                grabbedRope = null;
            }

            if (IsServer)
            {
                StartCoroutine(GrabCooldown(1f));
                myRig.velocity += new Vector2(0, jumpStrength);
                SendUpdate("JUMP", "");
            }
        }
        else if (flag == "SWING")
        {
            state = "SWINGING";
            grabbedRope = GameObject.Find(value).GetComponent<NetRope>();
        }
        else if(flag == "GRAB")
        {
            if(IsClient)
            {
                //improve later
                GameObject.Find(value).GetComponentInParent<NetRope>().GrabRope(this);
            }
        }
        else if(flag == "CANGRAB")
        {
            if (IsClient)
            {
                canGrabRope = true;
            }
        }
        else if(flag == "GROUNDED")
        {
            isGrounded = true;
        }
        else if (flag == "DEBUG")
        {
            Debug.Log(value);
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
        else if (!NET_FLAGS.ContainsKey(flag))
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand(flag, value);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        myRig = GetComponent<Rigidbody2D>();
        NET_FLAGS = GetComponent<NetworkRB2D>().NET_FLAGS;
        colors = new Color[3];
        colors[0] = new Color(255, 0, 0, 255); //red
        colors[1] = new Color(0, 0, 255, 255); //blue
        colors[2] = new Color(0, 255, 0, 255); //green
    }

    public override void NetworkedStart()
    {
        GetComponent<SpriteRenderer>().flipX = true;
    }

    public Vector2 Vector2FromString(string str)
    {
        //Vector2 is "(X,Y)"
        // Trim() removes whitespace chars
        string[] args = str.Trim().Trim('(').Trim(')').Split(',');
        return new Vector2(
            float.Parse(args[0]),
            float.Parse(args[1])
            );
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {               
        if(IsServer)
        {
            if(collision.gameObject.name.Contains("Floor"))
            {                
                isGrounded = true;
                //is this necessary?
                SendUpdate("GROUNDED", "");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {        
        if (IsServer)
        {
            if (collision.gameObject.name.Contains("Floor"))
            {                
                isGrounded = true;
                //is this necessary?
                SendUpdate("GROUNDED", "");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.name.Contains("Rope") && canGrabRope)
            {
                //using a wrapper so all rope variables can be modified on the rope script
                collision.gameObject.GetComponentInParent<NetRope>().GrabRope(this);
                SendUpdate("GRAB", collision.gameObject.name);
            }
        }
    }

    public void Mover(InputAction.CallbackContext mv)
    {
        if (IsLocalPlayer)
        {
            if (mv.started || mv.performed)
            {
                lastMoveInput = mv.ReadValue<Vector2>();
                SendCommand("MOVE", lastMoveInput.ToString());
            }
            else if (mv.canceled)
            {
                lastMoveInput = Vector2.zero;
                SendCommand("MOVE", lastMoveInput.ToString());
            }            
        }
    }

    public void Jump(InputAction.CallbackContext jp)
    {
        if (IsLocalPlayer)
        {
            if (jp.started)
            {
                SendCommand("JUMP", "");
            }            
        }
    }

    public void GrabRope(NetRope rope)
    {
        if (IsServer)
        {
            state = "SWINGING";
            this.transform.position = rope.swingPos.position;
            grabbedRope = rope;
            SendUpdate("SWING", rope.name);
        }
    }

    private IEnumerator GrabCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);        
        canGrabRope = true;
        SendUpdate("CANGRAB", "");
    }


    public override IEnumerator SlowUpdate()
    {
        while(IsConnected)
        { 
            if(IsServer)
            {
                if(IsDirty)
                {
                    IsDirty = false;
                }
            }
            yield return new WaitForSeconds(.1f);
        }
    }   

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {
            if (state == "SWINGING")
            {
                myRig.velocity = Vector3.zero;
                transform.position = swingPos.position;
            }
            else if (state == "LAUNCHING")
            {
                Vector2 newVel = myRig.velocity;
                newVel.x += lastMoveInput.x;
                //launchVec.x = Mathf.Max(launchVec.x, speed);
                float allowedXSpeed = Mathf.Max(launchVec.x, speed);
                newVel.x = Mathf.Clamp(newVel.x, -allowedXSpeed, allowedXSpeed);

                myRig.velocity = newVel;
            }
            else if(state == "NORMAL")
            {
                myRig.velocity = new Vector2(lastMoveInput.x, 0) * speed + new Vector2(0, myRig.velocity.y);
            }
        }
    }       
}
