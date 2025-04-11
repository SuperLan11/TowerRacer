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
    [System.NonSerialized] public int CharSelected = -1;

    //nonsync vars    
    [System.NonSerialized] public Rope grabbedRope;        

    private Vector2 lastMoveInput;
    [System.NonSerialized] public string holdingDir = "none";
    public string state = "NORMAL";

    [System.NonSerialized] public Transform swingPos;
    [System.NonSerialized] public Vector2 launchVec = Vector2.zero;
    private bool isGrounded = true;
    private bool canGrabRope = true;
    [System.NonSerialized] public int swingPosHeight = 0;

    [System.NonSerialized] public bool onLadder = false;
    //public LadderObj grabbedLadder = null;
    [System.NonSerialized] public LadderTest grabbedLadder = null;
    [System.NonSerialized] public bool inDismount = false;        
    
    private Vector2 lastAimDir;
    private GameObject arrowPivot;
    private GameObject aimArrow;    

    private GameObject itemUI;
    [System.NonSerialized] public bool hasBomb = false;

    public Sprite[] heroSprites;

    [SerializeField] private float acceleration = 0.3f;
    [SerializeField] private float launchCorrectionSpeed = 8f;
    public float jumpStrength = 10f;
    [SerializeField] private float airSlowdownMult = 1f;
    private float arrowSensitivity = 0.2f;

    private Camera cam;
    [SerializeField] private float camAccel = 0.2f;
    public static float highestCamY;
    private Text placeLbl;
    private Color32[] placeColors;


    public override void HandleMessage(string flag, string value)
    {
        if (flag == "START")
        {
            if (IsClient)
            {
                PName = value.Split(";")[0];
                GetComponentInChildren<Text>().text = PName;
                ColorSelected = int.Parse(value.Split(";")[1]);
                CharSelected = int.Parse(value.Split(";")[2]);

                GetComponent<SpriteRenderer>().sprite = heroSprites[CharSelected];

                //calling a function soon after creating object sometimes runs before Start
                Start();                
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
            if (value == "pressed")
            {
                if (state == "SWINGING")
                {
                    canGrabRope = false;
                    state = "LAUNCHING";
                    //grabbedRope.BoostPlayer(this);
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
                    if (isGrounded)
                        myRig.velocity += new Vector2(0, jumpStrength);
                }
                isGrounded = false;
            }
            else if(value == "released")
            {
                myRig.velocity = new Vector2(myRig.velocity.x, 0);
            }
        }       
        else if (flag == "GRAVITY")
        {
            if (IsClient)
            {
                myRig.gravityScale = float.Parse(value);
            }
        }
        else if (flag == "AIM_STICK")
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
        else if (flag == "AIM_MOUSE")
        {
            if (IsServer)
            {
                lastAimDir = Vector2FromString(value);
            }
        }       
        else if(flag == "HAS_BOMB")
        {            
            if (IsClient)
            {
                hasBomb = true;
            }
        }
        else if (flag == "SHOOT_BOMB")
        {
            if (IsServer)
            {
                Vector2 bombPos = transform.position;
                bombPos.y += GetComponent<Collider2D>().bounds.size.y / 2;
                GameObject bombObj = MyCore.NetCreateObject(14, Owner, bombPos, Quaternion.identity);                
                Bomb bomb = bombObj.GetComponent<Bomb>();
                bomb.launchVec = lastAimDir * bomb.launchSpeed;                
                hasBomb = false;
            }                        
        }
        else if(flag == "DISMOUNT")
        {
            if(IsClient)
            {
                Vector2 dismountPos = Vector2FromString(value);
                transform.position = dismountPos;
            }
        }
        else if (flag == "CAM_END")
        {
            if (IsClient)
            {
                Debug.Log("set highest cam y to " + float.Parse(value));
                PlayerController.highestCamY = float.Parse(value);
            }
        }
        else if(flag == "PLACE")
        {
            if(IsLocalPlayer)
            {                
                if (value == "1")
                {
                    placeLbl.text = "1st";
                }
                else if(value == "2")
                {
                    placeLbl.text = "2nd";
                }
                else if(value == "3")
                {
                    placeLbl.text = "3rd";
                }
                else if(value == "4")
                {
                    placeLbl.text = "4th";
                }
                int place = (int)char.GetNumericValue(value[0]);                
                placeLbl.color = placeColors[place - 1];
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

        cam = Camera.main;
        highestCamY = GameObject.FindGameObjectWithTag("END_PIECE").transform.position.y;
        
        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();

        arrowPivot = transform.GetChild(0).GetChild(1).gameObject;
        aimArrow = arrowPivot.transform.GetChild(0).gameObject;
        spriteRender = GetComponent<SpriteRenderer>();        

        myRig = GetComponent<Rigidbody2D>();

        //needs to be color32, otherwise colors are from 0 to 1
        placeColors = new Color32[4];
        placeColors[0] = new Color32(255, 220, 0, 255); //gold for first
        placeColors[1] = new Color32(148, 148, 148, 255); //silver for second
        placeColors[2] = new Color32(196, 132, 0, 255); //bronze for third
        placeColors[3] = new Color32(255, 255, 255, 255); //white for fourth
    }

    public override void NetworkedStart()
    {
        GetComponent<SpriteRenderer>().flipX = true;
        itemUI = GameObject.FindGameObjectWithTag("ITEM_UI");  
    }

    public static PlayerController ClosestPlayerToPos(Vector2 pos)
    {
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        float minDist = Mathf.Infinity;

        if (players.Length == 0)
        {
            Debug.LogWarning("IsClosestPlayerToPos() found no players");
            return null;
        }

        int closestIdx = -1;
        for (int i = 0; i < players.Length; i++)
        {
            float distToPos = Vector2.Distance(players[i].transform.position, pos);
            if (distToPos < minDist)
            {
                minDist = distToPos;
                closestIdx = i;
            }
        }

        return players[closestIdx];
    }

    private float DirToDegrees(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //return -(angle + 360) % 360;
        return angle + 180;
    }

    //this assumes 0 degrees means the arrow is facing left
    private Vector2 RotZToDir(float rotZ)
    {
        float radianRot = rotZ * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(radianRot), -Mathf.Cos(radianRot));
        return direction;
    }

    public static Vector2 Vector2FromString(string str)
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
            if (collision.gameObject.name.Contains("Floor"))
            {                
                isGrounded = true;
                launchVec = Vector2.zero;
                state = "NORMAL";                
            }
            else if(collision.gameObject.tag == "FLOOR")
            {
                isGrounded = true;
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
                //collision.gameObject.GetComponentInParent<Rope>().GrabRope(this);
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
                SendCommand("JUMP", "pressed");
            }
            else if(jp.canceled)
            {
                SendCommand("JUMP", "released");
            }
        }
    }

    public void AimStick(InputAction.CallbackContext aim)
    {
        if (IsLocalPlayer)
        {
            if (!hasBomb)
                return;

            if (aim.started || aim.performed)
            {
                lastAimDir = aim.ReadValue<Vector2>();
                SendCommand("AIM_STICK", lastAimDir.ToString());

                //do this only on local player
                aimArrow.GetComponent<SpriteRenderer>().enabled = true;
                Vector3 aimDir = new Vector3(lastAimDir.x, lastAimDir.y, 0);
                Vector3 newRot = arrowPivot.transform.eulerAngles;
                newRot.z = DirToDegrees(aimDir);
                arrowPivot.transform.eulerAngles = newRot;
            }
            else if (aim.canceled)
            {                
                SendCommand("SHOOT_BOMB", "");
                Destroy(itemUI.transform.GetChild(0).gameObject);
                hasBomb = false;

                lastAimDir = Vector2.zero;
                aimArrow.GetComponent<SpriteRenderer>().enabled = false;
                SendCommand("AIM_STICK", lastAimDir.ToString());
            }                    
        }
    }

    public void LmbClick(InputAction.CallbackContext mc)
    {
        if (IsLocalPlayer)
        {
            if (!hasBomb)
                return;

            if (mc.started)
            {                
                aimArrow.GetComponent<SpriteRenderer>().enabled = true;
                //arrows points up initially
                arrowPivot.transform.eulerAngles = new Vector3(0, 0, 180);
                SendCommand("AIM_MOUSE", Vector3.zero.ToString());
            }
            else if (mc.canceled)
            {                
                aimArrow.GetComponent<SpriteRenderer>().enabled = false;
                SendCommand("SHOOT_BOMB", "");
                Destroy(itemUI.transform.GetChild(0).gameObject);
                hasBomb = false;
            }
        }
    }

    public void AimMouse(InputAction.CallbackContext mm)
    {
        if (IsLocalPlayer)
        {
            Vector2 delta = mm.ReadValue<Vector2>();
            if ((mm.started || mm.performed) && hasBomb)
            {                
                Vector3 newArrowRot = arrowPivot.transform.eulerAngles;
                //minus since rotation is more negative clockwise
                float potentialNewZ = newArrowRot.z - delta.x * arrowSensitivity;                
                newArrowRot.z = potentialNewZ;                

                if(newArrowRot.z > 45 && newArrowRot.z < 315)
                    arrowPivot.transform.eulerAngles = newArrowRot;

                Vector2 aimDir = RotZToDir(arrowPivot.transform.eulerAngles.z);                
                SendCommand("AIM_MOUSE", aimDir.ToString());
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
        while (IsConnected)
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
                Vector2 newVel = new Vector2(lastMoveInput.x, 0) * speed + new Vector2(0, myRig.velocity.y);
                myRig.velocity = Vector2.Lerp(myRig.velocity, newVel, acceleration);
            }            
        }

        if (IsLocalPlayer)
        {
            Vector3 newCamPos = new Vector3(0, 0, cam.transform.position.z);
            newCamPos.x = GameManager.CENTER_PIECE_X;
            //Mathf.infinity is not bad on performance at all since it is stored as some sort of constant
            newCamPos.y = Mathf.Clamp(this.transform.position.y + 5, -Mathf.Infinity, highestCamY);

            //use Vector3 lerp because Vector2.lerp puts camera z at 0 and messes up the view
            cam.transform.position = Vector3.Lerp(cam.transform.position, newCamPos, camAccel);
        }
    }

    public override void TakeDamage(int damage)
    {
        //throw new System.NotImplementedException();
    }
}