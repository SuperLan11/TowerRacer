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

    // May not need these if we use inheritance for player classes
    //[System.NonSerialized] public SpriteRenderer spriteRender;
    //[System.NonSerialized] public Sprite sprite;

    //nonsync vars    
    public Rope grabbedRope;    
    private Color[] colors;

    private Vector2 lastMoveInput;
    public string holdingDir = "none";
    public string state = "NORMAL";
        
    public Transform swingPos;
    public Vector2 launchVec = Vector2.zero;
    private bool isGrounded = true;
    private bool canGrabRope = true;
    public int swingPosHeight = 0;

    public bool onLadder = false;
    public LadderObj grabbedLadder = null;
    public bool inDismount = false;
    
    [SerializeField] private float launchCorrectionSpeed = 8f;
    public float jumpStrength = 10f;
    [SerializeField] private float airSlowdownMult = 1f;

    public bool hasBomb = false;
    private Vector2 lastAimDir;
    private GameObject arrowPivot;
    private GameObject aimArrow;    


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
            {
                if (holdingDir != "right" && state == "SWINGING")
                {
                    grabbedRope.pivotRig.AddTorque(grabbedRope.dirChangeTorque);
                }
                holdingDir = "right";
            }
            else if (lastMoveInput.x < 0)
            {
                if (holdingDir != "left" && state == "SWINGING")
                {
                    grabbedRope.pivotRig.AddTorque(-grabbedRope.dirChangeTorque);
                }
                holdingDir = "left";
            }
            else if (lastMoveInput.y > 0)
            {
                holdingDir = "up";
            }
            else if (lastMoveInput.y < 0)
            {
                holdingDir = "down";
            }
            else
            {
                holdingDir = "none";
            }
        }
        else if (flag == "JUMP")
        {
            isGrounded = false;
            //initially called on server
            if (state == "SWINGING")
            {
                canGrabRope = false;
                state = "LAUNCHING";
                grabbedRope.BoostPlayer(this);
                grabbedRope.playerPresent = false;
                grabbedRope = null;
                StartCoroutine(GrabCooldown(1f));
            }
            else if (state == "LADDER")
            {
                state = "NORMAL";
                grabbedLadder = null;
                onLadder = false;
                myRig.velocity += new Vector2(0, jumpStrength);
            }
            else
            {
                myRig.velocity += new Vector2(0, jumpStrength);
            }
        }
        else if (flag == "GRAVITY")
        {
            if (IsClient)
            {
                myRig.gravityScale = float.Parse(value);
            }
        }
        else if (flag == "AIM")
        {
            if (IsServer)
            {                
                Vector2 newAimDir = Vector2FromString(value);
                if (newAimDir.magnitude < 0.7f)
                {
                    //don't set aim dir if less a threshold so bomb does not have 0 velocity
                    aimArrow.GetComponent<SpriteRenderer>().enabled = false;
                }
                else if (newAimDir.magnitude >= 0.7f)
                {
                    lastAimDir = Vector2FromString(value);
                    aimArrow.GetComponent<SpriteRenderer>().enabled = true;
                    Vector3 aimDir = new Vector3(lastAimDir.x, lastAimDir.y, 0);                    
                    Vector3 newRot = arrowPivot.transform.eulerAngles;
                    newRot.z = DirToDegrees(aimDir);
                    arrowPivot.transform.eulerAngles = newRot;
                }
            }
        }
        else if(flag == "BOMB")
        {
            if(IsServer)
            {                
                Vector2 bombPos = transform.position;
                bombPos.y += GetComponent<Collider2D>().bounds.size.y / 2;
                GameObject bomb = MyCore.NetCreateObject(14, Owner, bombPos, Quaternion.identity);
                bomb.GetComponent<Bomb>().launchVec = lastAimDir * 15;
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
                SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            }
        }
    }    

    // Start is called before the first frame update
    private void Start()
    {
        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;

        arrowPivot = transform.GetChild(0).GetChild(1).gameObject;
        aimArrow = arrowPivot.transform.GetChild(0).gameObject;

        myRig = GetComponent<Rigidbody2D>();
        colors = new Color[3];
        colors[0] = new Color(255, 0, 0, 255); //red
        colors[1] = new Color(0, 0, 255, 255); //blue
        colors[2] = new Color(0, 255, 0, 255); //green
    }

    public override void NetworkedStart()
    {
        GetComponent<SpriteRenderer>().flipX = true;
    }

    private float DirToDegrees(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //return -(angle + 360) % 360;
        return angle + 180;
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

    public float GetSpeed()
    {
        return this.speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {               
        if(IsServer)
        {
            if(collision.gameObject.name.Contains("Floor"))
            {                
                isGrounded = true;
                launchVec = Vector2.zero;
                state = "NORMAL";                
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
                collision.gameObject.GetComponentInParent<Rope>().GrabRope(this);
                state = "SWINGING";
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

    public void Aim(InputAction.CallbackContext aim)
    {
        if (IsLocalPlayer)
        {
            if (aim.started || aim.performed)
            {
                lastAimDir = aim.ReadValue<Vector2>();
                SendCommand("AIM", lastAimDir.ToString());

                //do this on local player
                aimArrow.GetComponent<SpriteRenderer>().enabled = true;
                Vector3 aimDir = new Vector3(lastAimDir.x, lastAimDir.y, 0);
                Vector3 newRot = arrowPivot.transform.eulerAngles;
                newRot.z = DirToDegrees(aimDir);
                arrowPivot.transform.eulerAngles = newRot;
            }            
            else if (aim.canceled)
            {
                if (hasBomb)
                {
                    SendCommand("BOMB", "");
                }
                
                lastAimDir = Vector2.zero;
                aimArrow.GetComponent<SpriteRenderer>().enabled = false;
                SendCommand("AIM", lastAimDir.ToString());
            }                        
        }
    }

    private IEnumerator GrabCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);        
        canGrabRope = true;        
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
                myRig.velocity = (swingPos.position - transform.position) * grabbedRope.swingSnapMult;
            }
            else if (state == "LAUNCHING")
            {
                Vector2 newVel = myRig.velocity;
                newVel.x += lastMoveInput.x * Time.deltaTime * launchCorrectionSpeed;
                if (lastMoveInput.x == 0)
                    newVel.x *= 1 - (Time.deltaTime * airSlowdownMult);

                if (Mathf.Abs(newVel.x) < Mathf.Abs(launchVec.x))
                    launchVec.x = newVel.x;

                float allowedXSpeed = Mathf.Max(Mathf.Abs(launchVec.x), speed);
                newVel.x = Mathf.Clamp(newVel.x, -Mathf.Abs(allowedXSpeed), Mathf.Abs(allowedXSpeed));

                myRig.velocity = newVel;
            }
            else if (state == "LADDER")
            {
                if (holdingDir == "up")
                {
                    myRig.velocity = new Vector2(0, grabbedLadder.ladderSpeed);
                }
                else if (holdingDir == "down")
                {
                    myRig.velocity = new Vector2(0, -grabbedLadder.ladderSpeed);
                }
                else
                {
                    myRig.velocity = Vector2.zero;
                }

                if (GetComponent<Collider2D>().bounds.min.y >
                    grabbedLadder.GetComponent<Collider2D>().bounds.max.y)
                { 
                    state = "NORMAL";                    
                    myRig.velocity = Vector2.zero;
                    onLadder = false;
                    grabbedLadder.attachedPlayer = null;
                    grabbedLadder = null;                    
                    myRig.gravityScale = 1f;
                    SendUpdate("GRAVITY", "1");
                }
            }
            else if(state == "NORMAL")
            {
                myRig.velocity = new Vector2(lastMoveInput.x, 0) * speed + new Vector2(0, myRig.velocity.y);    
            }            
        }
    }       
}
