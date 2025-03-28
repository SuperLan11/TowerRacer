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
    private Color[] colors;
    private Vector2 lastMoveInput;          
    [SerializeField] private float jumpStrength = 10f;
    public Dictionary<string, string> NET_FLAGS = new Dictionary<string, string>();

    private string state = "NORMAL";
    private Rope grabbedRope;
    public string holdingDir = "";


    public override void HandleMessage(string flag, string value)
    {
        if (IsServer)
        {
            Debug.Log("server got flag " + flag + " in " + this.GetType().Name);
        }

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
                Debug.Log("server got move input: " + value);                
                SendUpdate("MOVE", value);
            }
        }
        else if(flag == "JUMP")
        {
            if (state == "SWINGING")
            {
                state = "NORMAL";                
            }

            if (IsServer)
            {
                Debug.Log("jumping!");
                myRig.velocity += new Vector2(0, jumpStrength);                
                SendUpdate("JUMP", "");
            }            
        }
        else if(flag == "SWING")
        {
            state = "SWINGING";
            grabbedRope = GameObject.Find(value).GetComponent<Rope>();            
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

    public void GrabRope(Rope rope)
    {
        if (IsServer)
        {
            state = "SWINGING";
            this.transform.position = rope.swingLoc.position;
            grabbedRope = rope;
            SendUpdate("SWING", rope.name);
        }
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
            /*if (state == "SWINGING")
            {
                this.transform.position = grabbedRope.swingLoc.position;
                myRig.velocity = Vector3.zero;
            }
            else
            {
                myRig.velocity = new Vector3(lastMoveInput.x, 0, 0) * speed + new Vector3(0, myRig.velocity.y, 0);
            }*/
            myRig.velocity = new Vector3(lastMoveInput.x, 0, 0) * speed + new Vector3(0, myRig.velocity.y, 0);

        }
        /*if(IsClient)
        {
            Debug.Log("testing SendUpdate from PlayerController...");
            SendUpdate("DEBUG", "");
        }*/
    }
}
