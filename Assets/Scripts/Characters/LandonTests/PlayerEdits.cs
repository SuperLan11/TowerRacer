//Character controller datafields and methods are courtesy of of Sasquatch B Studios.
//https://youtu.be/zHSWG05byEc?si=_eNhW3uz9ZkeFVMr

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;
using UnityEngine.UI;

using NETWORK_ENGINE;

using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


/*TODO
    4.start programming different abilities
    3. Camera code
    ...
    5. Test IsDirty stuff once we have movement states other than ground that aren't dependent on input (climbing, swinging, etc)*/


public class PlayerEdits : Character
{


    //!If you add a variable to this, you are responsible for making sure it goes in the IsDirty check in SlowUpdate()    

    // [System.NonSerialized] public Text PlayerName;
    // [System.NonSerialized] public string PName = "<Default>";

    [System.NonSerialized] public bool isFacingRight;

    [System.NonSerialized] public bool jumpPressed = false;
    [System.NonSerialized] public bool jumpReleased = false;

    [System.NonSerialized] public bool movementAbilityPressed = false;

    //I doubt we'll want run button in our game, but it's here just in case
    [System.NonSerialized] public bool holdingRun = false;

    [SerializeField] public movementState currentMovementState;
    [SerializeField] public characterClass selectedCharacterClass;

    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public BoxCollider2D feetCollider;
    [SerializeField] public BoxCollider2D bodyCollider;
    [System.NonSerialized] public RopeEdits currentRope = null;
    [System.NonSerialized] public LadderEdits currentLadder = null;
    //we may want to eventually use the rigidbody variable in Character.cs, although ain't no way we're keeping the name as "myRig"
    public Rigidbody2D rigidbody;

    public float MAX_WALK_SPEED = 12.5f;
    public float GROUND_ACCELERATION = 5f, GROUND_DECELERATION = 20f;
    public float AIR_ACCELERATION = 5f, AIR_DECELERATION = 5f;
    public float WALL_JUMP_ACCELERATION;     //totally fine if we want to make it independent

    public float MAX_RUN_SPEED = 20f;

    //these values will probably need to change based on the size of the Player

    public float COLLISION_RAYCAST_LENGTH = 0.02f;
    public float WALL_COLLISION_RAYCAST_LENGTH;

    public float JUMP_HEIGHT = 6.5f;
    public float JUMP_HEIGHT_COMPENSATION_FACTOR = 1.054f;
    //apex = max height of jump
    public float TIME_TILL_JUMP_APEX = 0.35f;
    public float GRAVITY_RELEASE_MULTIPLIER = 2f;
    public float MAX_FALL_SPEED = 26f;
    //no need to have canDoubleJump or anything like that since we have this int. Unfortunately can't be a  cause of double jumping
    public int MAX_JUMPS = 1;
    public int MAX_WALL_JUMPS = 1;
    public float WALL_JUMP_HORIZONTAL_BOOST = 15f;

    [SerializeField] public bool inMovementAbilityCooldown = false;
    public float dashSpeed;
    public float dashTimer;
    public float MAX_DASH_TIME = 0.5f;



    [System.NonSerialized] public uint numWallJumpsUsed = 0;
    [System.NonSerialized] public bool onWall = false;
    [System.NonSerialized] public bool inWallJump = false;
    [System.NonSerialized] public bool onWallWithoutJumpPressed = false;
    [System.NonSerialized] public bool wallJumpPressed = false;

    //!RENAME THIS LATER
    [System.NonSerialized] public bool canGrabRope = true;
    public int swingPosHeight = 0;
    [System.NonSerialized] public Transform swingPos;
    public float MAX_SWING_SPEED = 7.0f;
    public float MAX_LAUNCH_SPEED;

    public float TIME_FOR_UP_CANCEL = 0.027f;
    public float APEX_THRESHOLD = 0.97f, APEX_HANG_TIME = 0.075f;
    public float MAX_JUMP_BUFFER_TIME = 0.125f;
    public float MAX_JUMP_COYOTE_TIME = 0.1f;
    public float MAX_WALL_JUMP_TIME = 0.2f;
    public float MAX_WALL_STICK_TIME = 3f;

    [System.NonSerialized] public float gravity;
    [System.NonSerialized] public float initialJumpVelocity;
    [System.NonSerialized] public float adjustedJumpHeight;

    [System.NonSerialized] public float verticalVelocity;
    [System.NonSerialized] public Vector2 moveVelocity;
    [System.NonSerialized] public Vector2 moveInput;
    [System.NonSerialized] public Vector2 ropeLaunchVec = Vector2.zero;

    //how fast you can change directions midair
    [SerializeField] public float launchCorrectionSpeed = 32f;      //previously 8f
    [SerializeField] public float airSlowdownMult = 1f;

    public float fastFallTimer;
    public float fastFallReleaseSpeed;

    [System.NonSerialized] public int numJumpsUsed;
    public float apexPoint;
    public float timePastApexThreshold;
    public bool isPastApexThreshold;

    public float jumpBufferTimer;
    [System.NonSerialized] public bool jumpReleasedDuringBuffer;
    public float coyoteTimer;
    public float wallJumpTimer;
    public float wallsTimer;

    //can't do this cause references to items is funky
    //public Item currentlyEquippedItem = null;
    [System.NonSerialized] public bool hasBomb = false;
    public Vector2 lastAimDir;
    [SerializeField] public GameObject aimArrow;
    [SerializeField] public GameObject arrowPivot;
    public float ARROW_SENSITIVITY = 0.2f;

    [SerializeField] public float movementAbilityCooldownTimer;
    [SerializeField] public float MAX_MOVEMENT_ABILITY_COOLDOWN;


    [System.NonSerialized] public Vector2 upNormal = new Vector2(0, 1f);
    [System.NonSerialized] public Vector2 downNormal = new Vector2(0, -1f);
    [System.NonSerialized] public Vector2 leftNormal = new Vector2(-1f, 0);
    [System.NonSerialized] public Vector3 rightNormal = new Vector2(1f, 0);

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    public enum movementState
    {
        GROUND,
        JUMPING,    //!jumping means you are in the air with the jump button pressed
        FALLING,
        FAST_FALLING,
        SWINGING,   //trigger
        LAUNCHING,  //trigger
        CLIMBING,   //trigger
        DASHING,    //abiliy
    };

    public enum characterClass
    {
        ARCHER,
        MAGE,
        BANDIT,
        KNIGHT
    }




    //!For the else {} debug to work, you NEED to check IsServer or IsClient INSIDE of the flag if statement!
    public override void HandleMessage(string flag, string value)
    {
        if (flag == "MOVE")
        {
            if (IsServer)
            {
                //not a sync var, but still needs to be set on the server
                moveInput = Player.Vector2FromString(value);
            }
        }
        else if (flag == "JUMP_PRESSED")
        {
            jumpPressed = true;
            jumpReleased = false;

            if (IsServer)
            {
                SendUpdate("JUMP_PRESSED", "GoodMorning");
            }
        }
        else if (flag == "JUMP_RELEASED")
        {
            jumpPressed = false;
            jumpReleased = true;

            if (IsServer)
            {
                SendUpdate("JUMP_RELEASED", "GoodMorning");
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
        else if (flag == "MOVEMENT_ABILITY_PRESSED")
        {
            movementAbilityPressed = bool.Parse(value);

            if (IsServer)
            {
                SendUpdate("MOVEMENT_ABILITY_PRESSED", value);
            }
        }
        else if (flag == "IS_FACING_RIGHT")
        {
            isFacingRight = bool.Parse(value);
        }
        else if (flag == "HOLDING_RUN")
        {
            holdingRun = bool.Parse(value);
        }
        else if (flag == "HAS_BOMB")
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
                float yOffset = 2f;
                bombPos.y += ((bodyCollider.bounds.size.y / 2) + (feetCollider.bounds.size.y / 2) + yOffset);

                GameObject bombObj = MyCore.NetCreateObject(14, Owner, bombPos, Quaternion.identity);
                Bomb bomb = bombObj.GetComponent<Bomb>();
                bomb.launchVec = lastAimDir * bomb.launchSpeed;

                hasBomb = false;
            }
        }
        else if (flag == "CURRENT_MOVEMENT_STATE")
        {
            //doing this for performance reasons since Enum.Parse<>() is apparently performance-intensive
            switch (value)
            {
                case "GROUND":
                    currentMovementState = movementState.GROUND;
                    break;
                case "JUMPING":
                    currentMovementState = movementState.JUMPING;
                    break;
                case "FALLING":
                    currentMovementState = movementState.FALLING;
                    break;
                case "FAST_FALLING":
                    currentMovementState = movementState.FAST_FALLING;
                    break;
                case "SWINGING":
                    currentMovementState = movementState.SWINGING;
                    break;
                case "LAUNCHING":
                    currentMovementState = movementState.LAUNCHING;
                    break;
                case "CLIMBING":
                    currentMovementState = movementState.CLIMBING;
                    break;
                case "DASHING":
                    currentMovementState = movementState.DASHING;
                    break;
            }
        }
        else if (flag == "SELECTED_CHARACTER_CLASS")
        {
            //doing this for performance reasons since Enum.Parse<>() is apparently performance-intensive
            switch (value)
            {
                case "ARCHER":
                    selectedCharacterClass = characterClass.ARCHER;
                    break;
                case "MAGE":
                    selectedCharacterClass = characterClass.MAGE;
                    break;
                case "BANDIT":
                    selectedCharacterClass = characterClass.BANDIT;
                    break;
                case "KNIGHT":
                    selectedCharacterClass = characterClass.KNIGHT;
                    break;
            }
        }

        //anything with a cooldown is gonna look something like this
        /*else if (flag == "ATTACK")
        {
            if (IsServer)
            {
                SendUpdate("ATTACK", "GoodMorning");
            }
            else
            {
                animator.Play("Attack1h1", 0);
            }
        }*/

        else if (!OTHER_FLAGS.ContainsKey(flag))
        {
            Debug.LogWarning(flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            if (IsClient)
            {
                SendCommand("DEBUG", flag + " is not a valid flag in " + this.GetType().Name + ".cs");
            }
        }
    }

    #region HELPER

    public static Vector2 Vector2FromString(string v)
    {
        string raw = v.Trim('(').Trim(')');
        string[] args = raw.Split(',');
        return new Vector2(float.Parse(args[0].Trim()), float.Parse(args[1].Trim()));
    }

    //this is technically a bad way to do it, but it'll be better for performance
    public string MovementStateToString(movementState value)
    {
        switch (value)
        {
            case movementState.GROUND:
                return "GROUND";
            case movementState.JUMPING:
                return "JUMPING";
            case movementState.FALLING:
                return "FALLING";
            case movementState.FAST_FALLING:
                return "FAST_FALLING";
            case movementState.SWINGING:
                return "SWINGING";
            case movementState.LAUNCHING:
                return "LAUNCHING";
            case movementState.CLIMBING:
                return "CLIMBING";
            case movementState.DASHING:
                return "DASHING";
            default:
                return "GROUND";
        }
    }

    public string CharacterClassToString(characterClass value)
    {
        switch (value)
        {
            case characterClass.ARCHER:
                return "ARCHER";
            case characterClass.MAGE:
                return "MAGE";
            case characterClass.BANDIT:
                return "BANDIT";
            case characterClass.KNIGHT:
                return "KNIGHT";
            default:
                return "THOU HAS SELECTED THE WRONG CLASS";
        }
    }

    public float DirToDegrees(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //return -(angle + 360) % 360;
        return angle + 180;
    }

    //this assumes 0 degrees means the arrow is facing left
    public Vector2 RotZToDir(float rotZ)
    {
        float radianRot = rotZ * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(radianRot), -Mathf.Cos(radianRot));
        return direction;
    }

    public static PlayerEdits ClosestPlayerToPos(Vector2 pos)
    {
        PlayerEdits[] players = FindObjectsOfType<PlayerEdits>();
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

    #endregion

    public override void NetworkedStart()
    {
        CalculateInitialConditions();
        dashSpeed = initialJumpVelocity * 2f;

        isFacingRight = true;
        //!WE ARE NOT USING SPEED OR MYRIG ON THE PLAYER!!!!!!
        speed = -9000000;
        myRig = null;

        switch (selectedCharacterClass)
        {
            case characterClass.ARCHER:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 20f;
                break;
            case characterClass.MAGE:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 5f;
                break;
            //why 3 jumps for a double jump? Cause Unity can't do something as simple as make a functional input system. Set this to 2 at your own
            //peril
            case characterClass.BANDIT:
                MAX_JUMPS = 3;
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 0.0001f;
                //maybe increase movement speed as well?
                break;
            case characterClass.KNIGHT:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 2f;
                break;

        }

        if (!GameManager.debugMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        currentMovementState = movementState.GROUND;
    }

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        if (rigidbody == null)
        {
            Debug.LogError("Thine rigidbody is missing, good sir!");
        }

        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;

        // arrowPivot = transform.GetChild(0).GetChild(1).gameObject;
        // aimArrow = arrowPivot.transform.GetChild(0).gameObject;

        //add this back in when we start doing player spawn eggs

        GameObject temp = GameObject.Find("SpawnPoint");
        rigidbody.position = temp.transform.position;


        rigidbody.gravityScale = 0f;
    }

    #region PHYSICS
    //go watch the GDC talk if you want know why this math works
    public void CalculateInitialConditions()
    {
        //proper way to do this would probably be just to modify jump height, but I'm lazy
        float patrickBSAdjuster = 0.1f;

        adjustedJumpHeight = JUMP_HEIGHT * JUMP_HEIGHT_COMPENSATION_FACTOR;

        //g = (-2 * h) / t^2
        gravity = -1 * (2f * adjustedJumpHeight) / Mathf.Pow(TIME_TILL_JUMP_APEX, 2f) * patrickBSAdjuster;

        initialJumpVelocity = Mathf.Abs(gravity);
    }

    //wouldn't recommend changing this
    public void SetApexVariables()
    {
        if (isPastApexThreshold)
        {
            timePastApexThreshold += Time.deltaTime;
            if (timePastApexThreshold < APEX_HANG_TIME)
            {
                verticalVelocity = 0f;
            }
            else
            {
                verticalVelocity = -0.01f;
            }
        }
        else
        {
            isPastApexThreshold = true;
            timePastApexThreshold = 0f;
        }
    }

    //wouldn't recommend changing this
    public void GravityOnAscending()
    {
        verticalVelocity += gravity * Time.deltaTime;

        if (isPastApexThreshold)
        {
            isPastApexThreshold = false;
        }
    }

    #endregion

    //only gets called in server
    public void TurnCheck()
    {
        bool turnRight;

        if (isFacingRight && moveInput.x < 0)
        {
            turnRight = false;
            Turn(turnRight);
        }
        else if (!isFacingRight && moveInput.x > 0)
        {
            turnRight = true;
            Turn(turnRight);
        }
    }

    //only gets called in server
    public void Turn(bool turnRight)
    {
        isFacingRight = turnRight;

        if (isFacingRight)
        {
            transform.Rotate(0f, 180f, 0f);
        }
        else
        {
            transform.Rotate(0f, -180f, 0f);
        }
    }


    #region COLLISION

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsServer)
        {
            if (collision.gameObject.name.Contains("Floor"))
            {
                currentMovementState = movementState.GROUND;
                ropeLaunchVec = Vector2.zero;
            }
            else if (collision.gameObject.tag == "FLOOR")
            {
                currentMovementState = movementState.GROUND;
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
                currentMovementState = movementState.SWINGING;
            }
        }
    }

    public bool CheckForGround()
    {
        if (IsServer)
        {
            Vector2 tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == upNormal))
                {
                    return true;
                }
            }

            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = feetCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == upNormal))
                {
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == upNormal))
                {
                    return true;
                }
            }
        }

        return false;
    }


    public bool CheckForCeiling()
    {
        if (IsServer)
        {
            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == downNormal))
                {
                    return true;
                }
            }

            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = bodyCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == downNormal))
                {
                    return true;
                }
            }

            tempPos.x = bodyCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == downNormal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool CanLeftWallJump()
    {
        if (IsServer)
        {
            Vector2 leftTempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] leftHits = Physics2D.RaycastAll(leftTempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            bool leftCollision = false;
            foreach (RaycastHit2D hit in leftHits)
            {
                if (!hit.collider.isTrigger && (hit.normal == (Vector2)rightNormal))
                {
                    leftCollision = true;
                }
            }
            //bool movingLeft = (moveInput.x < 0f);
            //bool movingRight = (moveInput.x > 0f);

            return leftCollision;
        }

        return false;
    }

    public bool CanRightWallJump()
    {
        if (IsServer)
        {
            Vector2 rightTempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] rightHits = Physics2D.RaycastAll(rightTempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            bool rightCollision = false;
            foreach (RaycastHit2D hit in rightHits)
            {
                if (!hit.collider.isTrigger && (hit.normal == leftNormal))
                {
                    rightCollision = true;
                }
            }
            //bool movingRight = (moveInput.x > 0f);
            // bool movingLeft = (moveInput.x < 0f);

            return rightCollision;
        }

        return false;
    }

    public bool CanWallJump()
    {
        return ((CanLeftWallJump() || CanRightWallJump()) && (numWallJumpsUsed < MAX_WALL_JUMPS));
    }

    public bool CheckForWalls()
    {
        if (IsServer)
        {
            Vector2 tempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            bool leftCollision = false;
            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == (Vector2)rightNormal))
                {
                    leftCollision = true;
                }
            }
            //DrawDebugNormal(leftTempPos, Vector2.left, GROUND_DETECTION_RAY_LENGTH, true);

            Vector2 rightTempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] rightHits = Physics2D.RaycastAll(rightTempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            bool rightCollision = false;
            foreach (RaycastHit2D hit in rightHits)
            {
                if (!hit.collider.isTrigger && (hit.normal == leftNormal))
                {
                    rightCollision = true;
                }
            }
            //DrawDebugNormal(rightTempPos, Vector2.right, GROUND_DETECTION_RAY_LENGTH, true);

            return (leftCollision || rightCollision);
        }

        return false;
    }

    //At some point we may want to convert this to a general CheckForTriggers() function that takes in different parameters, and depending
    //on the parameter assigns currentRope, currentLadder, etc
    public bool CheckForRopes()
    {
        if (IsServer)
        {
            //UP
            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope up");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }

            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = bodyCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope down");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }

            tempPos.x = bodyCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope down");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }


            //DOWN
            tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope down");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope down");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope down");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }


            //LEFT
            tempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == (Vector2)rightNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope left");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }


            //RIGHT
            tempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == leftNormal) && hit.collider.gameObject.name.Contains("Rope"))
                {
                    Debug.Log("hit rope right");
                    currentRope = hit.collider.gameObject.GetComponentInParent<RopeEdits>();
                    return true;
                }
            }
        }

        return false;
    }

    public bool CheckForLadders()
    {
        if (IsServer)
        {
            //UP
            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }

            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = bodyCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }

            tempPos.x = bodyCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }


            //DOWN
            tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }


            //LEFT
            tempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == (Vector2)rightNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }


            //RIGHT
            tempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == leftNormal) && hit.collider.gameObject.name.Contains("Ladder"))
                {
                    currentLadder = hit.collider.gameObject.GetComponentInParent<LadderEdits>();
                    return true;
                }
            }
        }

        return false;
    }

    //We may need to do this differently in the future for performance reasons, but if we want to actually handle collisions in Update(), we need
    //different methods for checking side to side colliders vs triggers

    //!You must be in scene view for this to show up
    public void DrawDebugNormal(Vector2 pos, Vector2 unitVector, float length, bool makeLargeForVisibility = true)
    {
        if (makeLargeForVisibility)
        {
            length *= 20f;
        }

        Debug.DrawRay(pos, unitVector * length, Color.red);
    }

    #endregion

    #region MOVEMENT_STATE

    public bool IsGrounded()
    {
        return (currentMovementState == movementState.GROUND);
    }

    public bool IsJumping()
    {
        return (currentMovementState == movementState.JUMPING);
    }

    public bool IsFalling()
    {
        return (currentMovementState == movementState.FALLING);
    }

    public bool IsFastFalling()
    {
        return (currentMovementState == movementState.FAST_FALLING);
    }

    public bool IsFallingInTheAir()
    {
        return (IsFalling() || IsFastFalling());
    }

    public bool InTheAir()
    {
        return (IsJumping() || IsFallingInTheAir() || IsLaunching());
    }

    public bool IsClimbing()
    {
        return (currentMovementState == movementState.CLIMBING);
    }

    public bool IsSwinging()
    {
        return (currentMovementState == movementState.SWINGING);
    }

    public bool IsLaunching()
    {
        return (currentMovementState == movementState.LAUNCHING);
    }

    public bool IsDashing()
    {
        return (currentMovementState == movementState.DASHING);
    }

    //add to this if we add any trigger movement states
    public bool InSpecialMovementState()
    {
        return (IsClimbing() || IsSwinging() || IsLaunching());
    }


    public bool IsInMovementAbilityState()
    {
        return ((IsJumping() && (numJumpsUsed == MAX_JUMPS)) || IsDashing());
    }

    #endregion


    #region INPUT

    public void MoveAction(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            if (context.started || context.performed)
            {
                moveInput = context.ReadValue<Vector2>();
                SendCommand("MOVE", moveInput.ToString());
            }
            else if (context.canceled)
            {
                moveInput = Vector2.zero;
                SendCommand("MOVE", moveInput.ToString());
            }
        }
    }

    //"canceled" being called immediately after "started" or "performed" is why we need 3 jumps to do a double jump
    public void JumpAction(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            if (context.started || context.performed)
            {
                SendCommand("JUMP_PRESSED", "GoodMorning");
            }
            else if (context.canceled)
            {
                SendCommand("JUMP_RELEASED", "GoodMorning");
            }
        }
    }

    public void AimStick(InputAction.CallbackContext aim)
    {
        if (IsLocalPlayer)
        {
            if (!hasBomb)
            {
                return;
            }

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
                hasBomb = false;

                lastAimDir = Vector2.zero;
                aimArrow.GetComponent<SpriteRenderer>().enabled = false;
                SendCommand("AIM_STICK", lastAimDir.ToString());
            }
        }
    }

    //keyboard + mouse need its own callback functions because how they work is fundamentally different
    public void LmbClick(InputAction.CallbackContext mc)
    {
        if (IsLocalPlayer)
        {
            if (!hasBomb)
            {
                return;
            }

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
                float potentialNewZ = newArrowRot.z - delta.x * ARROW_SENSITIVITY;
                newArrowRot.z = potentialNewZ;

                if (newArrowRot.z > 45 && newArrowRot.z < 315)
                    arrowPivot.transform.eulerAngles = newArrowRot;

                Vector2 aimDir = RotZToDir(arrowPivot.transform.eulerAngles.z);
                SendCommand("AIM_MOUSE", aimDir.ToString());
            }
        }
    }

    public void MovementAbilityAction(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            if (context.started || context.performed)
            {
                SendCommand("MOVEMENT_ABILITY_PRESSED", true.ToString());
            }
            else if (context.canceled)
            {
                SendCommand("MOVEMENT_ABILITY_PRESSED", false.ToString());
            }
        }
    }

    #endregion


    #region CLEANUP

    //only gets called on server and when grounded
    public void JumpVariableCleanup()
    {
        fastFallTimer = 0f;
        isPastApexThreshold = false;
        verticalVelocity = 0f;
        numJumpsUsed = 0;
        numWallJumpsUsed = 0;
    }

    public void WallVariableCleanup()
    {
        onWall = false;
        onWallWithoutJumpPressed = false;
        wallJumpPressed = false;
    }

    public void RopeVariableCleanup()
    {
        ropeLaunchVec = Vector2.zero;
    }

    #endregion

    //only gets called on server
    public void InitiateJump(int jumps)
    {
        if (!IsJumping())
        {
            currentMovementState = movementState.JUMPING;
        }

        jumpBufferTimer = 0;
        numJumpsUsed += jumps;
        verticalVelocity = initialJumpVelocity;
    }

    public void InitiateWallJump()
    {
        if (!IsJumping())
        {
            currentMovementState = movementState.JUMPING;
        }

        WallVariableCleanup();

        //isFacingRight = (moveInput.x > 0f);
        isFacingRight = !isFacingRight;
        numWallJumpsUsed++;

        //we're gonna give the horizontal boost in Update() cause that's where we change horizontal velocity
        inWallJump = true;
        verticalVelocity = initialJumpVelocity;

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);
    }

    public IEnumerator GrabCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        canGrabRope = true;
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected)
        {
            if (IsServer)
            {

                if (IsDirty)
                {
                    SendUpdate("IS_FACING_RIGHT", isFacingRight.ToString());
                    SendUpdate("HOLDING_RUN", holdingRun.ToString());
                    SendUpdate("CURRENT_MOVEMENT_STATE", MovementStateToString(currentMovementState));
                    SendUpdate("SELECTED_CHARACTER_CLASS", CharacterClassToString(selectedCharacterClass));
                    SendUpdate("MOVEMENT_ABILITY_PRESSED", movementAbilityPressed.ToString());
                    //SendUpdate("NAME", Pname);

                    IsDirty = false;
                }
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }

    //order is going to matter a lot here
    //we're checking collision here rather than in any of the OnCollision() Unity methods
    void Update()
    {
        //Debug.Log("CurrentMovementState: " + currentMovementState);
        //Debug.Log("Move input" + moveInput);
        //Debug.Log("Num jumps used: " + numJumpsUsed);

        if (!MyId.IsInit)
        {
            return;
        }

        //this may be how we do walking animation code
        if (IsClient)
        {
            // float tempSpeed = this.rigidbody.velocity.magnitude;

            // if (tempSpeed <= 0.01f){
            //     animator.SetFloat("speedh", 0);
            // }else{
            //     animator.SetFloat("speedh", Mathf.Abs(tempSpeed));
            // }
        }


        if (IsServer)
        {
            //bool onGround = CheckForGround();

            if (inMovementAbilityCooldown)
            {
                if (movementAbilityCooldownTimer > 0f)
                {
                    movementAbilityCooldownTimer -= Time.deltaTime;
                }
                else
                {
                    movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN;
                    inMovementAbilityCooldown = false;
                }
            }

            if (movementAbilityPressed && !inMovementAbilityCooldown)
            {
                //at least for right now, the only way to double jump is going to be to hit jump twice
                if (selectedCharacterClass == characterClass.MAGE)
                {
                    currentMovementState = movementState.DASHING;
                    dashTimer = MAX_DASH_TIME;

                    Vector2 dashVelocity;
                    float xDirection = 0f, yDirection = 0f;

                    bool noInput = (moveInput.x > -0.01f && moveInput.x < 0.01f && moveInput.y > -0.01f && moveInput.y < 0.01f);
                    if (noInput)
                    {
                        xDirection = (isFacingRight ? 1f : -1f);
                        yDirection = 0f;

                        dashVelocity = new Vector2(xDirection * dashSpeed, yDirection);
                        verticalVelocity = dashVelocity.y;
                        rigidbody.velocity = dashVelocity;

                        inMovementAbilityCooldown = true;
                        return;
                    }

                    if (moveInput.x > 0.01f)
                    {
                        xDirection = 1f;
                    }
                    else if (moveInput.x < -0.01f)
                    {
                        xDirection = -1f;
                    }

                    if (moveInput.y > 0.01f)
                    {
                        yDirection = 1f;
                    }
                    else if (moveInput.y < -0.01f)
                    {
                        yDirection = -1f;
                    }

                    TurnCheck();

                    dashVelocity = new Vector2(xDirection * dashSpeed, yDirection * dashSpeed);
                    verticalVelocity = dashVelocity.y;
                    rigidbody.velocity = dashVelocity;

                    //bypassing both gravity and horizontal velocity code
                    inMovementAbilityCooldown = true;
                    return;
                }

                //inMovementAbilityCooldown = true;
            }

            if (currentMovementState != movementState.GROUND)
            {
                coyoteTimer -= Time.deltaTime;
            }
            else
            {
                coyoteTimer = MAX_JUMP_COYOTE_TIME;
            }


            #region SPECIAL_CASES
            //can't bypass jumping code cause we need gravity to work to be bypassed by special movement states

            //check this stuff in collision functions
            if (canGrabRope && CheckForRopes())
            {
                if (currentRope != null)
                {
                    canGrabRope = false;
                    currentRope.GrabRope(this);
                    Debug.Log("grab rope");
                    currentMovementState = movementState.SWINGING;
                }
            }

            if (CheckForLadders())
            {
                bool pressingUpOrDown = (moveInput.y > 0f || moveInput.y < 0f);
                if ((currentLadder != null) && pressingUpOrDown && !IsClimbing())
                {
                    currentLadder.InitializeLadderVariables(this);
                    currentMovementState = movementState.CLIMBING;
                }
            }

            if (IsSwinging())
            {
                if (jumpPressed)
                {
                    currentRope.BoostPlayer(this);
                    currentMovementState = movementState.LAUNCHING;
                    currentRope.playerPresent = false;
                    currentRope = null;
                    StartCoroutine(GrabCooldown(1f));
                }
                else
                {
                    //don't worry about horizontal movement cause it's already taken care of in the rope script
                    rigidbody.velocity = (swingPos.position - transform.position) * currentRope.swingSnapMult;
                }

                return;
            }

            if (IsDashing())
            {
                if (dashTimer > 0f)
                {
                    dashTimer -= Time.deltaTime;
                }
                else
                {
                    dashTimer = MAX_DASH_TIME;
                    currentMovementState = movementState.FALLING;
                }
            }

            if (IsLaunching() && (verticalVelocity < 0f))
            {
                currentMovementState = movementState.FAST_FALLING;
            }


            if (IsClimbing())
            {
                //ik this is copy paste from the jumping code, but idk a different way to do this that doesn't involve returning out of Update()
                //at specific points
                bool ladderJump = (jumpPressed && IsClimbing());
                if (ladderJump)
                {
                    InitiateJump(1);

                    if (jumpReleasedDuringBuffer)
                    {
                        currentMovementState = movementState.FAST_FALLING;
                        fastFallReleaseSpeed = verticalVelocity;
                    }
                }


                /*if (moveInput.y > 0f)
                {
                    rigidbody.velocity = new Vector2(0, currentLadder.ladderSpeed);
                }
                else if (moveInput.y < 0f)
                {
                    if (onGround)
                    {
                        currentMovementState = movementState.GROUND;
                    }
                    else
                    {
                        rigidbody.velocity = new Vector2(0, -currentLadder.ladderSpeed);
                    }
                }
                else
                {
                    rigidbody.velocity = Vector2.zero;
                }

                bool aboveLadder = (feetCollider.bounds.min.y > currentLadder.GetComponent<Collider2D>().bounds.max.y);
                if (aboveLadder)
                {
                    currentMovementState = movementState.GROUND;
                    rigidbody.velocity = Vector2.zero;
                    currentLadder.attachedPlayer = null;
                    currentLadder = null;
                    verticalVelocity = 0f;
                }

                return;
            }
            #endregion

            #region JUMPING
            jumpBufferTimer -= Time.deltaTime;

            if (!onGround)
            {
                coyoteTimer -= Time.deltaTime;
            }
            else
            {
                coyoteTimer = MAX_JUMP_COYOTE_TIME;
            }

            if (CheckForWalls())
            {
                if (!onWall)
                {
                    onWall = true;
                    wallsTimer = MAX_WALL_STICK_TIME;
                }
                else
                {
                    wallsTimer -= Time.deltaTime;
                }

                if (jumpReleased)
                {
                    onWallWithoutJumpPressed = true;
                }
                else if (jumpPressed && onWallWithoutJumpPressed)
                {
                    wallJumpPressed = true;
                }
            }
            else
            {
                WallVariableCleanup();
            }

            if (jumpPressed)
            {
                jumpBufferTimer = MAX_JUMP_BUFFER_TIME;
                jumpReleasedDuringBuffer = false;
            }
            else if (jumpReleased)
            {
                if (jumpBufferTimer > 0f)
                {
                    jumpReleasedDuringBuffer = true;
                }

                //letting go of jump while still moving upwards is what causes fast falling
                if (IsJumping() && verticalVelocity > 0f)
                {
                    currentMovementState = movementState.FAST_FALLING;

                    if (isPastApexThreshold)
                    {
                        isPastApexThreshold = false;
                        fastFallTimer = TIME_FOR_UP_CANCEL;
                        verticalVelocity = 0;
                    }
                    else
                    {
                        fastFallReleaseSpeed = verticalVelocity;
                    }
                }
            }

            //no need to check for isJumpPressed since that's what jumpBufferTimer is doing
            bool normalJump = (jumpBufferTimer > 0f && !IsJumping() && (onGround || coyoteTimer > 0f));
            bool wallJump = (wallsTimer > 0f && onWall && wallJumpPressed && CanWallJump());
            bool extraJump = (jumpBufferTimer > 0f && IsFastFalling() && (numJumpsUsed < MAX_JUMPS));
            bool airJump = (jumpBufferTimer > 0f && IsFallingInTheAir() && (numJumpsUsed < MAX_JUMPS - 1));

            if (normalJump)
            {
                InitiateJump(1);

                if (jumpReleasedDuringBuffer)
                {
                    currentMovementState = movementState.FAST_FALLING;
                    fastFallReleaseSpeed = verticalVelocity;
                }
            }
            else if (wallJump)
            {
                InitiateWallJump();
            }
            else if (extraJump)
            {
                InitiateJump(1);
            }
            else if (airJump)
            {
                //forces player to only get one jump when they're falling, so they can't fall off a ledge and then jump twice.
                InitiateJump(2);

                currentMovementState = movementState.FAST_FALLING;
            }

            bool justLanded = (InTheAir() && onGround && (verticalVelocity <= 0f));
            if (justLanded)
            {
                currentMovementState = movementState.GROUND;
                JumpVariableCleanup();
                RopeVariableCleanup();

                //force it to reset cause Unity's input system is a piece of dogcrap, and otherwise it'll infintely jump
                jumpPressed = false;
                jumpReleased = true;
                SendUpdate("JUMP_RELEASED", "GoodMorning");
            }
            #endregion

            #region VERTICAL_VELOCITY   
            if (InTheAir())
            {
                //If we want our air movement to feel more floaty, we'll probably want to comment this out. This is a question for Mr. Game Design
                if (CheckForCeiling())
                {
                    currentMovementState = movementState.FAST_FALLING;
                }

                if (verticalVelocity >= 0f)
                {
                    apexPoint = Mathf.InverseLerp(initialJumpVelocity, 0f, verticalVelocity);
                    //if we're 97% of the way there to apex, we set apex variables
                    if (apexPoint > APEX_THRESHOLD)
                    {
                        SetApexVariables();
                    }
                    else
                    {
                        GravityOnAscending();
                    }
                }
                else if (!IsFastFalling())
                {
                    verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.deltaTime;
                }
                else if (verticalVelocity < 0f)
                {
                    if (!IsFallingInTheAir())
                    {
                        currentMovementState = movementState.FALLING;
                    }
                }
            }

            if (IsFastFalling())
            {
                if (fastFallTimer >= TIME_FOR_UP_CANCEL)
                {
                    verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.deltaTime;
                }
                else if (fastFallTimer < TIME_FOR_UP_CANCEL)
                {
                    verticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTimer / TIME_FOR_UP_CANCEL));
                }

                fastFallTimer += Time.deltaTime;
            }

            if (!onGround && !IsJumping())
            {
                if (!IsFallingInTheAir() && !InSpecialMovementState())
                {
                    currentMovementState = movementState.FALLING;
                }

                verticalVelocity += gravity * Time.deltaTime;
            }

            verticalVelocity = Mathf.Clamp(verticalVelocity, -MAX_FALL_SPEED, 50f);
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);
            #endregion


            #region HORIZONTAL_VELOCITY
            //!You MUST change horizontal velocity here!
            if (inWallJump)
            {
                if (wallJumpTimer > 0f)
                {
                    wallJumpTimer -= Time.deltaTime;
                }
                else
                {
                    wallJumpTimer = MAX_WALL_JUMP_TIME;
                    inWallJump = false;
                }

                //can't do moveInput.x cause that would be using player input
                float xDirection = (isFacingRight ? 1f : -1f);

                Vector2 targetVelocity = new Vector2(xDirection * WALL_JUMP_HORIZONTAL_BOOST, 0f);

                moveVelocity = Vector2.Lerp(new Vector2(xDirection / 10f, 0f), targetVelocity, WALL_JUMP_ACCELERATION * Time.deltaTime);
                rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);

                //returning to temporarily take away horizontal control from the player
                return;
            }

            if (IsLaunching())
            {
                float newXVel = rigidbody.velocity.x;
                newXVel += moveInput.x * Time.deltaTime * launchCorrectionSpeed;
                if (moveInput.x == 0)
                    newXVel *= 1 - (Time.deltaTime * airSlowdownMult);

                if (Mathf.Abs(newXVel) < Mathf.Abs(ropeLaunchVec.x))
                    ropeLaunchVec.x = newXVel;

                //if this is too slow, change MAX_WALK_SPEED to a  with a higher value
                float allowedXSpeed = Mathf.Max(Mathf.Abs(ropeLaunchVec.x), MAX_LAUNCH_SPEED);
                newXVel = Mathf.Clamp(newXVel, -Mathf.Abs(allowedXSpeed), Mathf.Abs(allowedXSpeed));

                rigidbody.velocity = new Vector2(newXVel, rigidbody.velocity.y);

                return;
            }

            if (onGround && !IsJumping() && !InSpecialMovementState())
            {
                currentMovementState = movementState.GROUND;
                JumpVariableCleanup();
            }



            float currentAcceleration = (IsGrounded() ? GROUND_ACCELERATION : AIR_ACCELERATION);
            float currentDeceleration = (IsGrounded() ? GROUND_DECELERATION : AIR_DECELERATION);


            if (moveInput != Vector2.zero)
            {     //accelerate
                TurnCheck();

                Vector2 targetVelocity = new Vector2(moveInput.x, 0);
                targetVelocity *= (holdingRun ? MAX_RUN_SPEED : MAX_WALK_SPEED);

                moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
                rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
            }
            else
            {      //decelerate
                moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, currentDeceleration * Time.deltaTime);
                rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
            }*/
                #endregion
            }
        }
    }
}