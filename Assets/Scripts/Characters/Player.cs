/*
@Authors - Patrick and Landon
@Description - Player Script
*/
//Character controller datafields and methods are courtesy of of Sasquatch B Studios.
//https://youtu.be/zHSWG05byEc?si=_eNhW3uz9ZkeFVMr

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
//using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

using NETWORK_ENGINE;

using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using System.Numerics;


/*
TODO
    1. Level tiles (although not that many)
    2. End trigger for level where it waits for all 4 players and round resetting
    3. Speed powerup and chicken powerup (invincibility) fully programmed
    4. Placeholder art for enemies (and MAKE SURE it's actually placeholder so we don't leave them in the final game. Make them Ronald McDonald 
    or something)
*/

public class Player : Character
{
    //!If you add a variable to this, you are responsible for making sure it goes in the IsDirty check in SlowUpdate()
    #region SyncVars

    // [System.NonSerialized] public Text PlayerName;
    // [System.NonSerialized] public string PName = "<Default>";

    private bool isFacingRight;

    private bool jumpPressed = false;
    private bool jumpReleased = false;

    private bool movementAbilityPressed = false;
    private bool attackPressed = false;
    //int represents the index of the layer
    private int noJumpThruLayer;
    private int normalLayer;

    //I doubt we'll want run button in our game, but it's here just in case
    private bool holdingRun = false;
    [System.NonSerialized] public bool hasChicken = false;

    [System.NonSerialized] public bool hasSpeedBoost = false;

    [System.NonSerialized] public bool hasBomb = false;

    [SerializeField] private characterClass selectedCharacterClass;

    #endregion



    #region NonSync Vars

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D feetCollider;
    [SerializeField] private BoxCollider2D bodyCollider;
    [System.NonSerialized] public Rope currentRope = null;
    [System.NonSerialized] public LadderObj currentLadder = null;
    public bool inDismountTrigger = false;

    //we may want to eventually use the rigidbody variable in Character.cs, although ain't no way we're keeping the name as "myRig"
    [System.NonSerialized] public Rigidbody2D rigidbody;
    private LayerMask floorLayer;
    //don't need to worry about this in the inspector
    Tilemap tilemap;
    [System.NonSerialized] public Gamepad gamepad;

    private Camera cam;
    public static float highestCamY;
    private float camAccel = 0.12f;
    public bool camFrozen = false;
    public bool playerFrozen = false;
    private Text placeLbl;
    private Color32[] placeColors;
    [System.NonSerialized] public Vector2 startPos;
    public bool isRoundWinner = false;
    private Player winningPlayer;

    private GameObject itemUI;
    private GameObject scorePanel;
    //public GameObject[] itemPrefabs;
    public Sprite[] itemSprites;
    [System.NonSerialized] public int wins = 0;
    public Sprite[] heroSprites;
    public string playerName;

    [SerializeField] private Material dashMaterial;
    [SerializeField] private Material stunMaterial;
    [SerializeField] private Material movementAbilityMaterial;

    [SerializeField] private Color dashColor;
    [SerializeField] private Color knightDashColor;
    protected Color stunColor = Color.yellow;
    //used for lerping between normal material and gold material
    private const float MOVEMENT_ABILITY_FLASH_DURATION = 0.75f;


    [SerializeField] private AudioSource winRoundSfx;
    [SerializeField] private AudioSource useItemSfx;
    [SerializeField] private AudioSource dashSfx;
    [SerializeField] private AudioSource jumpSfx;
    [SerializeField] private AudioSource doubleJumpSfx;
    [SerializeField] private AudioSource stunSfx;
    [SerializeField] private AudioSource lungeSfx;
    [SerializeField] private AudioSource lungeHitSfx;
    [SerializeField] private AudioSource attackSfx;
    [SerializeField] private AudioSource movementRechargeSfx;

    //not const anymore cause we want to change it for speed boost powerup
    public float MAX_WALK_SPEED = 11f;
    private const float GROUND_ACCELERATION = 15f, GROUND_DECELERATION = 12f;

    private const float AIR_ACCELERATION = 15f, AIR_DECELERATION = 2f;
    private float WALL_JUMP_ACCELERATION;

    private float MAX_RUN_SPEED = 20f;

    //these values will probably need to change based on the size of the Player

    private const float COLLISION_RAYCAST_LENGTH = 0.02f;
    private const float WALL_COLLISION_RAYCAST_LENGTH = (COLLISION_RAYCAST_LENGTH + 0.01f) * 8f;       //formerly COLLISION_RAYCAST_LENGTH + 0.1f
    private const float ATTACK_RAYCAST_LENGTH = 2f;


    private float JUMP_HEIGHT = 6.5f;
    private float JUMP_HEIGHT_COMPENSATION_FACTOR = 1.054f;
    //apex = max height of jump
    private float TIME_TILL_JUMP_APEX = 0.35f;
    private float GRAVITY_RELEASE_MULTIPLIER = 4f;
    private float MAX_FALL_SPEED = 20f;
    //no need to have canDoubleJump or anything like that since we have this int. Unfortunately can't be a const cause of double jumping
    public int MAX_JUMPS = 1;
    private const int MAX_WALL_JUMPS = 1;
    private float WALL_JUMP_HORIZONTAL_BOOST = 15f;

    private bool inMovementAbilityCooldown = false;
    private float dashSpeed;
    private float knightDashSpeed;
    private float archerGrappleSpeed;
    private float dashTimer;

    private float DASH_EFFECT_DURATION = 0.5f;

    private float MAX_DASH_TIME = 0.02f;

    private Coroutine dashCoroutine;
    private Coroutine stunCoroutine;
    private Coroutine rumbleCoroutine;


    private uint numWallJumpsUsed = 0;
    private bool onWall = false;
    private bool inWallJump = false;
    private bool justStartedWallJump = false;
    private bool onWallWithoutJumpPressed = false;
    private bool wallJumpPressed = false;
    //just a heads up, these three variables are the epitome of jank. Touch them at your own
    private bool beenInAirLongEnough = false;
    private bool jumpReleasedDuringJump = false;
    private bool held = false;

    private bool canGrabRope = true;
    private bool canRopeJump = false;
    private bool onRopeWithoutJumpPressed = false;
    private bool ropeJumpPressed = false;

    [System.NonSerialized] public int swingPosHeight = 0;
    [System.NonSerialized] public Transform swingPos;
    [SerializeField] public const float MAX_SWING_SPEED = 7.0f;
    private float MAX_LAUNCH_SPEED;

    [SerializeField] private float TIME_FOR_UP_CANCEL = 0.027f;
    [SerializeField] private float APEX_THRESHOLD = 0.97f, APEX_HANG_TIME = 0.075f;
    private const float MAX_JUMP_BUFFER_TIME = 0.125f;
    //[SerializeField] private float MAX_JUMP_BUFFER_TIME = 0.0f;
    [SerializeField] private float MAX_JUMP_COYOTE_TIME = 0.1f;
    [SerializeField] private float MAX_WALL_JUMP_TIME = 0.05f;
    [SerializeField] private float MAX_WALL_STICK_TIME = 3f;

    private const float TILEMAP_PLATFORM_OFFSET = 2f;


    private float gravity; // = -1 * (2f * adjustedJumpHeight) / Mathf.Pow(TIME_TILL_JUMP_APEX, 2f) * patrickBSAdjuster;    
    private float initialJumpVelocity; // = Mathf.Abs(gravity);
    private float adjustedJumpHeight; //jump height * compensation

    [System.NonSerialized] public float verticalVelocity;
    private Vector2 moveVelocity;
    [System.NonSerialized] public Vector2 moveInput;
    [System.NonSerialized] public Vector2 ropeLaunchVec = Vector2.zero;

    //how fast you can change directions midair
    private float launchCorrectionSpeed = 32f;      //previously 8f
    private float airSlowdownMult = 1f;

    private float fastFallTimer;
    private float fastFallReleaseSpeed;

    private int numJumpsUsed;
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;

    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;
    private float coyoteTimer;
    private float wallJumpTimer;
    private float wallsTimer;

    //can't do this cause references to items is funky
    //private Item currentlyEquippedItem = null;

    [System.NonSerialized] public bool isInvincible = false;
    private float CHICKEN_INVINCIBILITY_TIME = 5f, TAKE_DAMAGE_INVINCIBILITY_TIME = 0.5f;
    private float SPEED_BOOST_TIME = 2.5f;
    public bool isStunned = false;
    private float STUN_TIME = 1f;
    private bool clientCollidersEnabled = true;

    //this is probably terrible, but we have 2 days left
    private bool jankGroundGravity = false;

    private Vector2 lastAimDir;
    private GameObject aimArrow;
    private GameObject arrowPivot;
    private const float ARROW_SENSITIVITY = 0.2f;

    [System.NonSerialized] public bool archerArrowHitObj = false;
    [System.NonSerialized] public Transform archerArrowHitPosition = null;

    private float movementAbilityCooldownTimer;
    private float MAX_MOVEMENT_ABILITY_COOLDOWN;


    private Vector2 upNormal = new Vector2(0, 1f);
    private Vector2 downNormal = new Vector2(0, -1f);
    private Vector2 leftNormal = new Vector2(-1f, 0);
    private Vector3 rightNormal = new Vector2(1f, 0);

    private const int BOMB_SPAWN_PREFAB_INDEX = Idx.BOMB;
    private const int LEFT_SWORD_SPAWN_PREFAB_INDEX = Idx.LEFT_SWORD;
    private const int RIGHT_SWORD_SPAWN_PREFAB_INDEX = Idx.RIGHT_SWORD;
    private const int ROPE_ARROW_SPAWN_PREFAB_INDEX = Idx.ROPE_ARROW;

    public Dictionary<string, string> OTHER_FLAGS = new Dictionary<string, string>();

    private enum movementState
    {
        GROUND,
        JUMPING,    //!jumping means you are in the air with the jump button pressed
        FALLING,
        FAST_FALLING,
        SWINGING,       //trigger
        LAUNCHING,      //trigger
        CLIMBING,       //trigger
        DASHING,        //abiliy
        KNIGHT_DASHING, //ability
        GRAPPLING,      //ability
    };

    [SerializeField] private movementState currentMovementState;
    private movementState lastMovementState;

    private enum characterClass
    {
        ARCHER,
        MAGE,
        BANDIT,
        KNIGHT
    }

    #endregion




    //!For the else {} debug to work, you NEED to check IsServer or IsClient INSIDE of the flag if statement!
    public override void HandleMessage(string flag, string value)
    {
        if (flag == "INIT_NAME")
        {
            if (IsClient)
            {
                playerName = value;
                //set name above opposing players' heads
                if (!IsLocalPlayer)
                    transform.GetChild(2).GetComponentInChildren<Text>().text = playerName;
                //put name in results later
            }
        }
        else if (flag == "PLACE")
        {
            if (IsLocalPlayer)
            {
                if (value == "1")
                {
                    placeLbl.text = "1st";
                }
                else if (value == "2")
                {
                    placeLbl.text = "2nd";
                }
                else if (value == "3")
                {
                    placeLbl.text = "3rd";
                }
                else if (value == "4")
                {
                    placeLbl.text = "4th";
                }
                int place = (int)char.GetNumericValue(value[0]);
                placeLbl.color = placeColors[place - 1];
            }
        }
        else if (flag == "FINALIZE_COLOR")
        {
            if (IsClient)
            {
                int place = int.Parse(value);
                Image playerScorePanel = scorePanel.transform.GetChild(Owner).GetComponent<Image>();
                Color32 placeColor = placeColors[place - 1];
                Debug.Log("set " + name + " score color to " + placeColor);

                placeColor.a = 0;
                playerScorePanel.color = placeColor;
            }
        }
        else if (flag == "ITEM")
        {
            if (IsLocalPlayer)
            {
                int itemIdx = int.Parse(value);

                ShowItem();
                itemUI.transform.GetChild(0).GetComponent<Image>().sprite = itemSprites[itemIdx];

                //GameObject itemImage = Instantiate(itemPrefabs[itemIdx], itemUI.transform.position, Quaternion.identity);                

                if (itemIdx == 0)
                {
                    hasChicken = true;
                    SendCommand("HAS_CHICKEN", hasChicken.ToString());
                }
                else if (itemIdx == 1)
                {
                    hasSpeedBoost = true;
                    SendCommand("HAS_SPEED_BOOST", hasSpeedBoost.ToString());
                }
                else if (itemIdx == 2)
                {
                    hasBomb = true;
                    SendCommand("HAS_BOMB", hasBomb.ToString());
                }
            }
        }
        else if (flag == "HAS_CHICKEN")
        {

            hasChicken = bool.Parse(value);

        }
        else if (flag == "HAS_SPEED_BOOST")
        {

            hasSpeedBoost = bool.Parse(value);

        }
        else if (flag == "HAS_BOMB")
        {

            hasBomb = bool.Parse(value);

        }
        else if (flag == "CAM_FREEZE")
        {
            if (IsClient)
            {
                camFrozen = true;
            }
        }
        else if (flag == "CAM_UNFREEZE")
        {
            if (IsClient)
            {
                camFrozen = false;
            }
        }
        else if (flag == "CAM_END")
        {
            if (IsClient)
            {
                //Debug.Log("set highest cam y to " + float.Parse(value));
                Player.highestCamY = float.Parse(value);
            }
        }
        else if (flag == "SHOOT_ANIM")
        {
            if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("ArcherShoot"))
                    anim.Play("ArcherShoot", -1, 0f);
            }
        }
        else if (flag == "HIT_DOOR")
        {
            if (IsClient)
            {
                GetComponent<TrailRenderer>().enabled = false;

                this.transform.position = startPos;
                SendCommand("IDLE_ANIM", "");

                int place = int.Parse(value.Split(";")[0]);
                int owner = int.Parse(value.Split(";")[1]);

                Color32 placeColor = placeColors[place - 1];
                placeColor.a = 0;
                //make score panel color correct for player's place
                //scorePanel.GetComponent<Image>().color = placeColor;
                scorePanel.transform.GetChild(owner).GetComponent<Image>().color = placeColor;
            }
        }
        else if (flag == "TIMED_OUT")
        {
            if (IsClient)
            {
                this.transform.position = startPos;
            }
        }
        else if (flag == "MOVE")
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

            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
            {
                anim.Play("Jump", -1, 0f);
                //doesn't accout for double jumps
                if (IsClient)
                {
                    //jumpSfx.Play();
                }
                //anim.Play("Takeoff", -1, 0f);
            }

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
        else if (flag == "JUMP_SFX")
        {
            if (IsClient)
                jumpSfx.Play();
        }
        else if (flag == "DOUBLE_JUMP_SFX")
        {
            if (IsClient)
            {
                doubleJumpSfx.Play();
            }
        }
        else if (flag == "LUNGE_SFX")
        {
            if (IsClient)
            {
                lungeSfx.Play();
            }
        }
        else if (flag == "LUNGE_HIT_SFX")
        {
            if (IsClient)
            {
                lungeSfx.Stop();
                lungeHitSfx.Play();
            }
        }
        else if (flag == "ATTACK_SFX")
        {
            if (IsClient)
            {
                attackSfx.Play();
            }
        }
        else if (flag == "ATTACK_ANIM")
        {
            if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Stab"))
                    anim.Play("Stab", -1, 0f);
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
        else if (flag == "ATTACK_PRESSED")
        {
            attackPressed = bool.Parse(value);

            if (IsServer)
            {
                SendUpdate("ATTACK_PRESSED", value);
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
        else if (flag == "SHOOT_BOMB")
        {
            if (IsServer)
            {
                Vector2 bombPos = transform.position;
                float yOffset = 2f;
                bombPos.y += ((bodyCollider.bounds.size.y / 2) + (feetCollider.bounds.size.y / 2) + yOffset);

                GameObject bombObj = MyCore.NetCreateObject(BOMB_SPAWN_PREFAB_INDEX, Owner, bombPos, Quaternion.identity);
                Bomb bomb = bombObj.GetComponent<Bomb>();
                bomb.currentPlayer = this;
                bomb.launchVec = lastAimDir * bomb.launchSpeed;
            }
        }
        else if (flag == "USE_ITEM")
        {
            if (IsServer)
            {
                useItemSfx.Play();
                if (hasChicken)
                {
                    UseChickenItem();
                }
                else if (hasSpeedBoost)
                {
                    UseSpeedBoostItem();
                }
                else if (hasBomb)
                {
                    Use8DirectionalBomb();
                }
            }
        }
        else if (flag == "PARENT_TO_ROPE")
        {
            if (IsClient)
            {
                Vector2 pos = Vector2FromString(value);
                Rope closestRope = ClosestRopeToPos(pos);
                Debug.Log("parenting to " + closestRope.transform.parent);
                transform.SetParent(closestRope.pivotRig.transform);

                rigidbody.freezeRotation = false;
            }
        }
        else if (flag == "PLAYER_FROZEN"){
            if (IsClient){
                playerFrozen = bool.Parse(value);
            }
        }
        else if (flag == "UNPARENT")
        {
            if (IsClient)
            {
                transform.SetParent(null);
                transform.eulerAngles = Vector3.zero;
                rigidbody.freezeRotation = true;
            }
        }
        else if (flag == "DISMOUNT")
        {
            if (IsClient)
            {
                Vector2 dismountPos = Vector2FromString(value);
                transform.position = dismountPos;
            }
        }
        else if (flag == "TELEPORT")
        {    //not necessary, but here to smooth out client teleportation just in case
            if (IsClient)
            {
                transform.position = Vector2FromString(value);
            }
        }
        else if (flag == "LOCAL_POS")
        {
            if (IsClient)
            {
                transform.localPosition = Vector2FromString(value);
            }
        }
        else if (flag == "JUMP_ANIM")
        {
            if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                    anim.Play("Jump", -1, 0f);
            }
        }
        else if (flag == "IDLE_ANIM")
        {
            if (IsServer)
            {
                SendUpdate("IDLE_ANIM", "");
            }
            else if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Idle"))
                    anim.Play("Idle", -1, 0f);
            }
        }
        else if (flag == "LADDER_ANIM")
        {
            if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Climb"))
                    anim.Play("Climb", -1, 0f);
            }
        }
        else if (flag == "LUNGE_ANIM")
        {
            if (IsClient)
            {
                if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Lunge"))
                    anim.Play("Lunge", -1, 0f);
            }
        }
        else if (flag == "TURN")
        {
            if (isFacingRight)
            {
                spriteRender.flipX = false;
                //transform.Rotate(0f, 180f, 0f);
            }
            else
            {
                spriteRender.flipX = true;
                //transform.Rotate(0f, -180f, 0f);
            }
        }
        else if (flag == "START_DASH_EFFECT")
        {
            if (IsClient)
            {
                StartDashEffect(dashColor);
            }
        }
        else if (flag == "LERP_GOLD")
        {
            if (IsLocalPlayer)
            {
                StartCoroutine(LerpToGold());
            }
        }
        else if (flag == "START_HIT_EFFECT")
        {
            if (IsClient)
            {
                StartHitEffect(hitColor);
            }
        }
        else if (flag == "START_STUN_EFFECT")
        {
            if (IsClient)
            {
                isStunned = true;
                StartStunEffect(stunColor);
            }
        }
        else if (flag == "GAMEPAD"){
            bool hasGamepad = bool.Parse(value);
            if (hasGamepad){
                GetCurrentGamepad();
            }else{
                gamepad = null;
            }
        }
        else if (flag == "CURSOR_VISIBLE")
        {
            if (IsLocalPlayer)
            {
                Cursor.visible = bool.Parse(value);
            }
        }
        else if (flag == "RUMBLE")
        {
            if (IsLocalPlayer)
            {
                //these will need to be variables if rumble will be used for multiple actions
                Rumble(0.25f, 1f, DASH_EFFECT_DURATION);
            }
        }
        else if (flag == "ENABLE_COLLIDERS")
        {
            if (IsClient)
            {
                feetCollider.enabled = true;
                bodyCollider.enabled = true;
            }
        }
        else if (flag == "DISABLE_COLLIDERS")
        {
            if (IsClient)
            {
                feetCollider.enabled = false;
                bodyCollider.enabled = false;
            }
        }
        else if (flag == "ENABLE_TRAIL")
        {
            if (IsClient)
            {
                transform.GetChild(3).GetComponent<TrailRenderer>().enabled = true;
            }
        }
        else if (flag == "DISABLE_TRAIL")
        {
            if (IsClient)
            {
                transform.GetChild(3).GetComponent<TrailRenderer>().enabled = true;
            }
        }
        else if (flag == "INIT_SCORE_PANEL")
        {
            if (IsClient)
            {
                //need to assign ui here because Start doesn't always run before sending handle msg on a newly instantiated object
                GameObject scorePanel = GameObject.FindGameObjectWithTag("SCORE");
                NPM[] playerNPMs = FindObjectsOfType<NPM>();
                //iterate over npms instead of players so you have the owner and character chosen
                foreach (NPM npm in playerNPMs)
                {
                    int owner = npm.Owner;
                    int charChosen = npm.CharSelected;

                    Image charImage = scorePanel.transform.GetChild(owner).GetChild(0).GetComponent<Image>();
                    Text nameLbl = scorePanel.transform.GetChild(owner).GetComponentInChildren<Text>();

                    Debug.Log("setting " + nameLbl.name + " to " + npm.PName);
                    nameLbl.text = npm.PName;
                    charImage.sprite = heroSprites[charChosen];
                }
            }
        }
        else if (flag == "ENABLE_JUMP_THRU_COLLISION")
        {
            if (IsClient)
            {
                this.gameObject.layer = normalLayer;
            }
        }
        else if (flag == "DISABLE_JUMP_THRU_COLLISION")
        {
            if (IsClient)
            {
                this.gameObject.layer = noJumpThruLayer;
            }
        }
        else if (flag == "WIN_ROUND_SFX")
        {
            if (IsLocalPlayer)
            {
                winRoundSfx.Play();
            }
        }
        else if (flag == "DASH_SFX")
        {
            if (IsClient)
            {
                dashSfx.Play();
            }
        }
        else if (flag == "WINNING_PLAYER")
        {
            if (IsClient)
            {
                int winningOwner = int.Parse(value);
                foreach (Player player in FindObjectsOfType<Player>())
                {
                    if (player.Owner == winningOwner)
                        winningPlayer = player;
                }
            }
        }
        else if (flag == "STUN_SFX")
        {
            if (IsClient)
            {
                stunSfx.Play();
            }
        }
        else if (flag == "STOP_STUN_SFX")
        {
            if (IsClient)
            {
                stunSfx.Stop();
            }
        }
        else if (flag == "MOVEMENT_RECHARGE_SFX")
        {
            if (IsLocalPlayer)
            {
                movementRechargeSfx.Play();
            }
        }
        else if (flag == "SET_GROUND_POS")
        {
            if (IsClient)
            {
                this.transform.position = Vector2FromString(value);
            }
        }
        else if (flag == "ENABLE_CLIENT_GRAVITY")
        {
            if (IsClient)
            {
                rigidbody.gravityScale = 15f;
            }
        }
        else if (flag == "DISABLE_CLIENT_GRAVITY")
        {
            if (IsClient)
            {
                rigidbody.gravityScale = 0f;
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
                case "KNIGHT_DASHING":
                    currentMovementState = movementState.KNIGHT_DASHING;
                    break;
                case "GRAPPLING":
                    currentMovementState = movementState.GRAPPLING;
                    break;
            }
        }

        //anything with a cooldown is gonna look something like this
        /*else if (flag == "ATTACK"){
            if (IsServer){
                SendUpdate("ATTACK", "GoodMorning");
            }else{
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
    private string MovementStateToString(movementState value)
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
            case movementState.KNIGHT_DASHING:
                return "KNIGHT_DASHING";
            case movementState.GRAPPLING:
                return "GRAPPLING";
            default:
                return "GROUND";
        }
    }

    private string CharacterClassToString(characterClass value)
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

    private float DirToDegrees(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //return -(angle + 360) % 360;
        //return angle + 180;
        return angle + 90;
    }

    //this assumes 0 degrees means the arrow is facing left
    private Vector2 RotZToDir(float rotZ)
    {
        float radianRot = rotZ * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Sin(radianRot), -Mathf.Cos(radianRot));
        return direction;
    }

    public static Player ClosestPlayerToPos(Vector2 pos)
    {
        Player[] players = FindObjectsOfType<Player>();
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

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        if (rigidbody == null)
        {
            Debug.LogError("Thine rigidbody is missing, good sir!");
        }

        floorLayer = LayerMask.GetMask("Floor", "JumpThru");
        bool invalidFloor = (floorLayer == 0);
        if (invalidFloor)
        {
            Debug.LogError("Thine floor layer is missing, good sir!");
        }

        spriteRender = GetComponent<SpriteRenderer>();
        sprite = spriteRender.sprite;
        regularMaterial = spriteRender.material;
        anim = GetComponent<Animator>();
        cam = Camera.main;

        //Cursor.visible = false;

        if (GameObject.FindGameObjectWithTag("QUIT") != null)
            GameObject.FindGameObjectWithTag("QUIT").SetActive(false);

        if (GetComponent<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponent<NetworkRB2D>().FLAGS;
        else if (GetComponentInChildren<NetworkRB2D>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkRB2D>().FLAGS;
        else if (GetComponent<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponent<NetworkTransform>().FLAGS;
        else if (GetComponentInChildren<NetworkTransform>() != null)
            OTHER_FLAGS = GetComponentInChildren<NetworkTransform>().FLAGS;

        placeColors = new Color32[4];
        placeColors[0] = new Color32(255, 220, 0, 255); //gold for first
        placeColors[1] = new Color32(148, 148, 148, 255); //silver for second
        placeColors[2] = new Color32(196, 132, 0, 255); //bronze for third
        placeColors[3] = new Color32(129, 135, 255, 255); //blue for fourth

        arrowPivot = transform.GetChild(0).gameObject;
        aimArrow = arrowPivot.transform.GetChild(0).gameObject;

        //need to do name to layer to get index. GetMask has runtime error
        noJumpThruLayer = LayerMask.NameToLayer("NoJumpThru");
        normalLayer = this.gameObject.layer;

        placeLbl = GameObject.FindGameObjectWithTag("PLACE").GetComponent<Text>();
        itemUI = GameObject.FindGameObjectWithTag("ITEM_UI");
        scorePanel = GameObject.FindGameObjectWithTag("SCORE");

        WALL_JUMP_ACCELERATION = AIR_ACCELERATION * 4f;

        //add this back in when we start doing player spawn eggs
        /*
        GameObject temp = GameObject.Find("SpawnPoint");
        rigidbody.position = temp.transform.position;\
        */

        rigidbody.gravityScale = 0f;
    }

    //!remember that if you get an error ANYWHERE in this script, the rest of it won't run, so you actually do need to worry about errors here
    public override void NetworkedStart()
    {
        CalculateInitialConditions();
        dashSpeed = 19f;
        knightDashSpeed = 20f;
        archerGrappleSpeed = 22f;
        MAX_LAUNCH_SPEED = MAX_WALK_SPEED * 20f;

        startPos = this.transform.position;

        isFacingRight = true;
        //!WE ARE NOT USING SPEED OR MYRIG ON THE PLAYER!!!!!!
        speed = -9000000;
        myRig = null;

        if (IsLocalPlayer)
        {
            GetCurrentGamepad();

            if (gamepad == null)
            {
                Debug.LogWarning("gamepad not detected");
                SendCommand("GAMEPAD", false.ToString());
            }else{
                SendCommand("GAMEPAD", true.ToString());
            }
        }

        health = MAX_HEALTH = 3;
        inAttackCooldown = false;
        ATTACK_COOLDOWN_DURATION = 1f;

        //unity youtube man says this is necessary for preventing side effects
        dashMaterial = new Material(dashMaterial);
        hitMaterial = new Material(hitMaterial);
        stunMaterial = new Material(stunMaterial);
        movementAbilityMaterial = new Material(movementAbilityMaterial);





        //TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        TrailRenderer trailRenderer = transform.GetChild(3).GetComponent<TrailRenderer>();
        switch (selectedCharacterClass)
        {
            case characterClass.ARCHER:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 1.15f;
                trailRenderer.startColor = Color.green;
                trailRenderer.endColor = new Color(0f, 255f, 0f, 0f);
                break;
            case characterClass.MAGE:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 3.3f;
                trailRenderer.startColor = Color.yellow;
                trailRenderer.endColor = new Color(255f, 255f, 0f, 0f);
                break;
            case characterClass.BANDIT:
                MAX_JUMPS = 2;
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 0.0001f;
                Vector2 adjustedTrailPos = new Vector2(trailRenderer.transform.localPosition.x, trailRenderer.transform.localPosition.y - 0.5f);
                trailRenderer.transform.localPosition = adjustedTrailPos;
                trailRenderer.startColor = Color.blue;
                trailRenderer.endColor = new Color(0f, 0f, 255f, 0f);
                //maybe increase movement speed as well?
                break;
            case characterClass.KNIGHT:
                movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN = 1.5f;
                trailRenderer.startColor = Color.red;
                trailRenderer.endColor = new Color(255f, 0f, 0f, 0f);
                break;
        }

        if (IsServer)
        {
            trailRenderer.enabled = false;
            StartCoroutine(ChangeMovementState());
        }

        if (!GameManager.debugMode)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        currentMovementState = movementState.FALLING;

    }

    #region PHYSICS
    //go watch the GDC talk if you want know why this math works
    private void CalculateInitialConditions()
    {
        //proper way to do this would probably be just to modify jump height, but I'm lazy
        float patrickBSAdjuster = 0.1f;

        adjustedJumpHeight = JUMP_HEIGHT * JUMP_HEIGHT_COMPENSATION_FACTOR;

        //g = (-2 * h) / t^2
        gravity = -1 * (2f * adjustedJumpHeight) / Mathf.Pow(TIME_TILL_JUMP_APEX, 2f) * patrickBSAdjuster;

        //initialJumpVelocity = Mathf.Abs(gravity);
        initialJumpVelocity = Mathf.Abs(gravity) + 1.8f;
    }

    //wouldn't recommend changing this
    private void SetApexVariables()
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
    private void GravityOnAscending()
    {
        verticalVelocity += gravity * Time.deltaTime;

        if (isPastApexThreshold)
        {
            isPastApexThreshold = false;
        }
    }

    #endregion

    //only gets called in server
    private void TurnCheck()
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
    private void Turn(bool turnRight)
    {
        isFacingRight = turnRight;
        SendUpdate("IS_FACING_RIGHT", isFacingRight.ToString());

        if (isFacingRight)
        {
            transform.Rotate(0f, 180f, 0f);
        }
        else
        {
            transform.Rotate(0f, -180f, 0f);
        }
        SendUpdate("TURN", "GoodMorning");
    }


    #region COLLISION
    //previously using COLLISION_RAYCAST_LENGTH * 2
    private bool CheckForGround()
    {
        if (IsServer)
        {
            bool jumpingThroughTilemap = false;

            Vector2 tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);

            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.GetComponent<TilemapCollider2D>() != null)
                {
                    tilemap = hit.collider.GetComponent<Tilemap>();
                    float tileUpperY = GetTileUpperY(hit);

                    jumpingThroughTilemap = ((verticalVelocity > 0f) && (feetCollider.bounds.min.y < tileUpperY + TILEMAP_PLATFORM_OFFSET));
                }

                /*if (jumpingThroughTilemap){
                    clientCollidersEnabled = false;
                    SendUpdate("DISABLE_COLLIDERS", "GoodMorning");
                }*/

                if (!hit.collider.isTrigger && (hit.normal == upNormal)/* && !jumpingThroughTilemap*/)
                {
                    return true;
                }
            }

            DrawDebugNormal(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, false);


            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = feetCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.GetComponent<TilemapCollider2D>() != null)
                {
                    tilemap = hit.collider.GetComponent<Tilemap>();
                    float tileUpperY = GetTileUpperY(hit);

                    jumpingThroughTilemap = ((verticalVelocity > 0f) && (feetCollider.bounds.min.y < tileUpperY + TILEMAP_PLATFORM_OFFSET));
                }

                /* if (jumpingThroughTilemap){
                     clientCollidersEnabled = false;
                     SendUpdate("DISABLE_COLLIDERS", "GoodMorning");
                 }*/

                if (!hit.collider.isTrigger && (hit.normal == upNormal)/* && !jumpingThroughTilemap*/)
                {
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.GetComponent<TilemapCollider2D>() != null)
                {
                    tilemap = hit.collider.GetComponent<Tilemap>();
                    float tileUpperY = GetTileUpperY(hit);

                    jumpingThroughTilemap = ((verticalVelocity > 0f) && (feetCollider.bounds.min.y < tileUpperY + TILEMAP_PLATFORM_OFFSET));
                }

                /*if (jumpingThroughTilemap){
                    clientCollidersEnabled = false;
                    SendUpdate("DISABLE_COLLIDERS", "GoodMorning");
                }*/

                if (!hit.collider.isTrigger && (hit.normal == upNormal)/* && !jumpingThroughTilemap*/)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private IEnumerator GroundCollisionGravity()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        SendUpdate("DISABLE_CLIENT_GRAVITY", "GoodMorning");
    }

    private bool TryToTeleportToGround()
    {
        if (IsServer)
        {
            Vector2 tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);

            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.GetComponent<TilemapCollider2D>() != null)
                {
                    tilemap = hit.collider.GetComponent<Tilemap>();
                    float tileUpperY = GetTileUpperY(hit);
                    float tempY = 0.01f + tileUpperY + (PlayerHeight() / 2f);

                    Vector2 tempGroundVec = new Vector2(this.transform.position.x, tempY);
                    SendUpdate("SET_GROUND_POS", tempGroundVec.ToString());

                    return true;
                }
            }
        }

        return false;
    }

    float GetTileUpperY(RaycastHit2D hit)
    {
        if (tilemap != null)
        {
            Vector3 hitWorldPos = hit.point;
            Vector3Int cellPosition = tilemap.WorldToCell(hitWorldPos);

            Vector3 tileWorldPos = tilemap.CellToWorld(cellPosition);
            float tileHeight = tilemap.cellSize.y;

            return tileWorldPos.y + tileHeight;
        }
        else
        {
            return -5000000f;
            Debug.LogError("Null tile map!");
        }
    }


    private bool CheckForCeiling()
    {
        if (IsServer)
        {
            int floorLayer = 6;
            int defaultLayer = 0;
            //bit shifting mumbo jumbo that lets me do this without having a for loop for each layer mask
            int combinedLayerMask = (1 << floorLayer) | (1 << defaultLayer);

            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH * 3f, combinedLayerMask);

            foreach (RaycastHit2D hit in hits)
            {
                DrawDebugNormal(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH * 3f, false);
                if (!hit.collider.isTrigger && (hit.normal == downNormal))
                {
                    return true;
                }
            }


            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = bodyCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH * 3f, combinedLayerMask);

            foreach (RaycastHit2D hit in hits)
            {
                if (!hit.collider.isTrigger && (hit.normal == downNormal))
                {
                    return true;
                }
            }

            tempPos.x = bodyCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH * 3f, combinedLayerMask);

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

    private bool CanLeftWallJump()
    {
        if (IsServer)
        {
            Vector2 leftTempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] leftHits = Physics2D.RaycastAll(leftTempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH * 2f, ~0);

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

    private bool CanRightWallJump()
    {
        if (IsServer)
        {
            Vector2 rightTempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D[] rightHits = Physics2D.RaycastAll(rightTempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH * 2f, ~0);

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

    private bool CanWallJump()
    {
        return ((CanLeftWallJump() || CanRightWallJump()) && (numWallJumpsUsed < MAX_WALL_JUMPS));
    }

    private bool CheckForWalls()
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
    bool CheckForTriggers(string triggerType)
    {
        if (triggerType != "Rope" && triggerType != "Ladder")
        {
            return false;
        }

        if (IsServer)
        {
            //UP
            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D[] hits = Physics2D.RaycastAll(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }

            //shoot left and right raycast only if middle raycast didn't detect anything
            tempPos.x = bodyCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }

            tempPos.x = bodyCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == downNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }


            //DOWN
            tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.min.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }

            tempPos.x = feetCollider.bounds.max.x;
            hits = Physics2D.RaycastAll(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == upNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }


            //LEFT
            tempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.left, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == (Vector2)rightNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }


            //RIGHT
            tempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            hits = Physics2D.RaycastAll(tempPos, Vector2.right, WALL_COLLISION_RAYCAST_LENGTH, ~0);

            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger && (hit.normal == leftNormal) && hit.collider.gameObject.name.Contains(triggerType))
                {
                    SetCurrentTriggerObj(triggerType, hit.collider.gameObject);
                    return true;
                }
            }
        }

        return false;
    }

    private void SetCurrentTriggerObj(string triggerType, GameObject collidingObj)
    {
        if (triggerType == "Rope")
        {
            currentRope = collidingObj.GetComponentInParent<Rope>();
        }
        else if (triggerType == "Ladder")
        {
            currentLadder = collidingObj.GetComponentInParent<LadderObj>();
        }
    }

    //We may need to do this differently in the future for performance reasons, but if we want to actually handle collisions in Update(), we need
    //different methods for checking side to side colliders vs triggers

    //!You must be in scene view for this to show up
    private void DrawDebugNormal(Vector2 pos, Vector2 unitVector, float length, bool makeLargeForVisibility = true)
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

    public bool JumpPressedAgainWhileFalling()
    {
        return (jumpPressed && jumpReleasedDuringJump);
    }

    public bool InTheAir()
    {
        return (IsJumping() || IsFallingInTheAir() || IsLaunching() || ((IsDashing() || IsKnightDashing()) && verticalVelocity > 0f));
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

    public bool IsKnightDashing()
    {
        return (currentMovementState == movementState.KNIGHT_DASHING);
    }

    public bool IsGrappling()
    {
        return (currentMovementState == movementState.GRAPPLING);
    }

    //add to this if we add any trigger movement states
    public bool InSpecialMovementState()
    {
        return (IsClimbing() || IsSwinging() || IsLaunching());
    }

    /*
    public bool IsInMovementAbilityState(){
        return ((IsJumping() && (numJumpsUsed == MAX_JUMPS)) || IsDashing());
    }*/

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
            if (!hasBomb || playerFrozen)
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
                if (newRot.z < 0)
                    newRot.z += 360f;

                if (newRot.z > 45 && newRot.z < 315)
                    arrowPivot.transform.eulerAngles = newRot;
            }
            else if (aim.canceled)
            {
                SendCommand("SHOOT_BOMB", "");
                hasBomb = false;
                SendCommand("HAS_BOMB", hasBomb.ToString());
                HideItem();

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
            if (!hasBomb || playerFrozen)
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
                SendCommand("HAS_BOMB", hasBomb.ToString());
                //Destroy(itemUI.transform.GetChild(0).gameObject);
                HideItem();
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

    //not used for bombs, those are different!
    public void UseItemAction(InputAction.CallbackContext context)
    {
        bool hasExactlyOneItem = ((hasChicken && !hasSpeedBoost && !hasBomb)
                                 || (!hasChicken && hasSpeedBoost && !hasBomb)
                                 || ((!hasChicken && !hasSpeedBoost && hasBomb)));
        if (!hasExactlyOneItem)
        {
            return;
        }

        if (IsLocalPlayer)
        {
            if (context.started)
            {
                SendCommand("USE_ITEM", "GoodMorning");
                //Destroy(itemUI.transform.GetChild(0).gameObject);
                HideItem();
            }
            else if (context.canceled)
            {
                if (hasChicken)
                {
                    hasChicken = false;
                    SendCommand("HAS_CHICKEN", hasChicken.ToString());
                }
                else if (hasSpeedBoost)
                {
                    hasSpeedBoost = false;
                    SendCommand("HAS_SPEED_BOOST", hasSpeedBoost.ToString());
                }
                else if (hasBomb)
                {
                    hasBomb = false;
                    SendCommand("HAS_BOMB", hasBomb.ToString());
                }
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

    public void AttackAction(InputAction.CallbackContext context)
    {
        if (IsLocalPlayer)
        {
            if (context.started || context.performed)
            {
                SendCommand("ATTACK_PRESSED", true.ToString());
            }
            else if (context.canceled)
            {
                SendCommand("ATTACK_PRESSED", false.ToString());
            }
        }
    }

    #endregion


    #region CLEANUP

    //only gets called on server and when grounded
    private void JumpVariableCleanup()
    {
        fastFallTimer = 0f;
        isPastApexThreshold = false;
        //beenInAirLongEnough = false;
        verticalVelocity = 0f;
        numJumpsUsed = 0;
        numWallJumpsUsed = 0;
        held = false;
    }

    private void WallVariableCleanup()
    {
        onWall = false;
        onWallWithoutJumpPressed = false;
        wallJumpPressed = false;
    }

    private void RopeVariableCleanup()
    {
        ropeLaunchVec = Vector2.zero;
        onRopeWithoutJumpPressed = false;
        ropeJumpPressed = false;
    }

    #endregion

    #region ITEM

    private void HideItem()
    {
        Image itemSprite = itemUI.transform.GetChild(0).GetComponent<Image>();
        Debug.Log("hiding " + itemSprite.name + "...");
        Color hiddenColor = itemSprite.color;
        hiddenColor.a = 0;
        itemSprite.color = hiddenColor;
    }

    private void ShowItem()
    {
        Image itemSprite = itemUI.transform.GetChild(0).GetComponent<Image>();
        Debug.Log("showing " + itemSprite.name + "...");
        Color visibleColor = itemSprite.color;
        visibleColor.a = 1;
        itemSprite.color = visibleColor;
    }

    private void UseChickenItem()
    {
        health = MAX_HEALTH;
        isInvincible = true;
        StartCoroutine(InvincibilityCooldown(CHICKEN_INVINCIBILITY_TIME));
    }

    private void UseSpeedBoostItem()
    {
        //MAX_WALK_SPEED *= 2;
        initialJumpVelocity *= 1.5f;
        StartCoroutine(SpeedBoostCooldown(SPEED_BOOST_TIME));
    }

    private void Use8DirectionalBomb()
    {
        float xDirection = 0f, yDirection = 0f;
        float xOffset = 2f;
        float yOffset = 2f;
        GameObject bombObj;
        Bomb bomb;
        Vector2 bombPos;

        bool noInput = (moveInput.x > -0.01f && moveInput.x < 0.01f && moveInput.y > -0.01f && moveInput.y < 0.01f);
        if (noInput)
        {
            xDirection = (isFacingRight ? 1f : -1f);
            yDirection = 0f;

            bombPos = transform.position;
            bombPos.x += (isFacingRight ? xOffset : -xOffset);
            bombPos.y += yOffset / 2f;

            bombObj = MyCore.NetCreateObject(BOMB_SPAWN_PREFAB_INDEX, Owner, bombPos, Quaternion.identity);
            bomb = bombObj.GetComponent<Bomb>();
            bomb.currentPlayer = this;
            bomb.launchVec = new Vector2(xDirection * bomb.launchSpeed, yDirection);

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

        bombPos = transform.position;
        bombPos.y += ((bodyCollider.bounds.size.y / 2) + (feetCollider.bounds.size.y / 2) + yOffset);

        bombObj = MyCore.NetCreateObject(BOMB_SPAWN_PREFAB_INDEX, Owner, bombPos, Quaternion.identity);
        bomb = bombObj.GetComponent<Bomb>();
        bomb.currentPlayer = this;
        Vector2 bombVelocity = new Vector2(xDirection, yDirection).normalized * bomb.launchSpeed;
        bomb.launchVec = bombVelocity;
    }

    public bool HasItem()
    {
        return (hasChicken || hasSpeedBoost || hasBomb);
    }

    #endregion

    //only gets called on server
    private void InitiateJump(int jumps)
    {
        if (!IsJumping())
        {
            currentMovementState = movementState.JUMPING;
        }

        //jumpBufferTimer = 0;
        numJumpsUsed += jumps;
        verticalVelocity = initialJumpVelocity;

        beenInAirLongEnough = false;
        StartCoroutine(AirRoutine());
    }

    private void InitiateWallJump()
    {
        if (!IsJumping())
        {
            currentMovementState = movementState.JUMPING;
        }

        WallVariableCleanup();

        //isFacingRight = (moveInput.x > 0f);
        isFacingRight = !isFacingRight;
        SendUpdate("IS_FACING_RIGHT", isFacingRight.ToString());
        Turn(isFacingRight);
        numWallJumpsUsed++;

        //we're gonna give the horizontal boost in Update() cause that's where we change horizontal velocity
        inWallJump = true;
        justStartedWallJump = true;
        verticalVelocity = initialJumpVelocity;

        beenInAirLongEnough = false;
        StartCoroutine(AirRoutine());

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);
    }

    public void ResetTimers()
    {
        movementAbilityCooldownTimer = MAX_MOVEMENT_ABILITY_COOLDOWN;
        inMovementAbilityCooldown = false;
        inAttackCooldown = false;
    }

    //should only be called in localplayer
    private void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        GetCurrentGamepad();
        if (gamepad == null)
        {
            Debug.LogWarning("no gamepad detected");
            return;
        }

        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        rumbleCoroutine = StartCoroutine(RumbleRoutine(duration));
    }

    private IEnumerator RumbleRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        gamepad.SetMotorSpeeds(0f, 0f);
    }

    private IEnumerator GrabCooldown(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        canGrabRope = true;
    }

    private IEnumerator InvincibilityCooldown(float cooldown)
    {
        yield return new WaitForSecondsRealtime(cooldown);

        isInvincible = false;
    }

    private IEnumerator SpeedBoostCooldown(float cooldown)
    {
        yield return new WaitForSecondsRealtime(cooldown);

        //MAX_WALK_SPEED /= 2;
        initialJumpVelocity /= 1.5f;
    }

    private IEnumerator StunCooldown()
    {
        yield return new WaitForSecondsRealtime(STUN_TIME);

        isStunned = false;
        health = 3;
        SendUpdate("STOP_STUN_SFX", "GoodMorning");
    }

    private IEnumerator RopeJumpCooldown(float cooldown)
    {
        yield return new WaitForSecondsRealtime(cooldown);

        canRopeJump = true;
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSecondsRealtime(ATTACK_COOLDOWN_DURATION);
        inAttackCooldown = false;
    }

    private void StartDashEffect(Color color)
    {
        //prevents multiple of the same coroutine from running
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
        }

        dashCoroutine = StartCoroutine(DashRoutine(color));
    }

    private IEnumerator DashRoutine(Color color)
    {
        spriteRender.material = dashMaterial;
        dashMaterial.color = color;

        yield return new WaitForSeconds(DASH_EFFECT_DURATION);

        spriteRender.material = regularMaterial;
        dashCoroutine = null;
    }

    //this is gippity slop, but we've got 8 hours till expo
    private IEnumerator LerpToGold()
    {
        Material matInstance = new Material(regularMaterial);
        spriteRender.material = matInstance;

        Vector2 origTiling = matInstance.GetTextureScale("_MainTex");
        Vector2 origOffset = matInstance.GetTextureOffset("_MainTex");

        float halfDur = MOVEMENT_ABILITY_FLASH_DURATION * 0.5f;
        float timer = 0f;

        //first half
        while (timer < halfDur)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDur);
            matInstance.Lerp(regularMaterial, movementAbilityMaterial, t);
            yield return null;
        }

        matInstance.Lerp(regularMaterial, movementAbilityMaterial, 1f);
        timer = 0f;

        //second half
        while (timer < halfDur)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / halfDur);
            matInstance.Lerp(movementAbilityMaterial, regularMaterial, t);
            yield return null;
        }

        matInstance.Lerp(movementAbilityMaterial, regularMaterial, 1f);

        matInstance.SetTextureScale("_MainTex", origTiling);
        matInstance.SetTextureOffset("_MainTex", origOffset);

        Destroy(matInstance);
        spriteRender.material = regularMaterial;
    }

    private IEnumerator ChangeMovementState()
    {
        while (IsConnected)
        {
            yield return new WaitUntil(() => currentMovementState != lastMovementState);

            lastMovementState = currentMovementState;
            SendUpdate("CURRENT_MOVEMENT_STATE", MovementStateToString(currentMovementState));
        }
    }

    private IEnumerator AirRoutine()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        beenInAirLongEnough = true;
    }

    private IEnumerator TestForHeld()
    {
        yield return new WaitForSeconds(0.1f);

        held = true;
    }

    protected void StartStunEffect(Color color)
    {
        //prevents multiple of the same coroutine from running
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunRoutine(color));
    }

    protected IEnumerator StunRoutine(Color color)
    {
        spriteRender.material = stunMaterial;
        stunMaterial.color = color;

        yield return new WaitForSeconds(STUN_TIME);

        spriteRender.material = regularMaterial;
        stunCoroutine = null;

        isStunned = false;
    }

    private IEnumerator EnsureEnabledColliders(){
        yield return new WaitForSeconds(0.3f);

        if (!IsSwinging()){
            SendUpdate("ENABLE_COLLIDERS", "GoodMorning");
        }
    }

    private void GetCurrentGamepad()
    {
        gamepad = Gamepad.current;
    }

    private float PlayerHeight()
    {
        float height = bodyCollider.bounds.max.y;
        height -= feetCollider.bounds.min.y;
        return height;
    }

    private Rope ClosestRopeToPos(Vector2 pos)
    {
        Rope[] ropes = FindObjectsOfType<Rope>();
        float minDist = Mathf.Infinity;
        Rope closestRope = null;

        foreach (Rope rope in ropes)
        {
            float distToRope = Vector2.Distance(transform.position, rope.transform.position);
            if (distToRope < minDist)
            {
                minDist = distToRope;
                closestRope = rope;
            }
        }
        if (closestRope == null)
            Debug.LogWarning("closest rope is null!");
        else
            Debug.Log("closest rope is " + closestRope.name);

        return closestRope;
    }

    protected override void Attack()
    {
        if (inAttackCooldown || isStunned)
        {
            return;
        }

        //this may be screwing me over
        //TurnCheck();

        //!don't forget to actually put this in handle message!
        //SendUpdate("ATTACK_ANIM", "GoodMorning");
        SendUpdate("ATTACK_SFX", "");
        SendUpdate("ATTACK_ANIM", "");

        Vector2 direction = (isFacingRight ? Vector2.right : Vector2.left);
        float xPos = (isFacingRight ? (bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH) : (bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH));
        Vector2 position = new Vector2(xPos, bodyCollider.bounds.center.y);

        RaycastHit2D[] hits = Physics2D.RaycastAll(position, direction, ATTACK_RAYCAST_LENGTH, ~0);

        foreach (RaycastHit2D hit in hits)
        {
            DrawDebugNormal(position, direction, ATTACK_RAYCAST_LENGTH, false);
            if (hit.collider.GetComponentInParent<Enemy>() != null)
            {
                hit.collider.GetComponentInParent<Enemy>().TakeDamage(1);
            }
            else if (hit.collider.GetComponent<Enemy>() != null)
            {
                hit.collider.GetComponent<Enemy>().TakeDamage(1);
            }
        }

        inAttackCooldown = true;
        StartCoroutine(AttackCooldown());
    }

    public override void TakeDamage(int damage)
    {
        if (!isInvincible && !isStunned && !playerFrozen)
        {
            //Debug.Log("taking damage");
            health -= damage;

            if (health <= 0)
            {
                isStunned = true;
                StartCoroutine(StunCooldown());
                SendUpdate("START_STUN_EFFECT", "GoodMorning");
                SendUpdate("STUN_SFX", "GoodMorning");
            }
            else
            {      //give player a moment of brief invincibility after taking a hit of damage
                isInvincible = true;
                StartCoroutine(InvincibilityCooldown(TAKE_DAMAGE_INVINCIBILITY_TIME));
                SendUpdate("START_HIT_EFFECT", "GoodMorning");
                //StartHitEffect(hitColor);
            }
        }
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
                    SendUpdate("SELECTED_CHARACTER_CLASS", CharacterClassToString(selectedCharacterClass));
                    SendUpdate("MOVEMENT_ABILITY_PRESSED", movementAbilityPressed.ToString());
                    SendUpdate("ATTACK_PRESSED", attackPressed.ToString());
                    SendUpdate("HAS_CHICKEN", hasChicken.ToString());
                    SendUpdate("HAS_SPEED_BOOST", hasSpeedBoost.ToString());
                    SendUpdate("HAS_BOMB", hasBomb.ToString());
                    //SendUpdate("CURRENT_MOVEMENT_STATE", MovementStateToString(currentMovementState));
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
        if (!MyId.IsInit)
        {
            return;
        }


        //smooth movement
        if (IsLocalPlayer)
        {
            //really should be in game manager, but I don't know how to do that and game expo starts in 3 hours
            if (playerFrozen){
                GetCurrentGamepad();

                
                if (gamepad == null){
                    Cursor.visible = true;
                }else{
                    Cursor.visible = false;
                }
            }
            
            //Debug.Log("currentLadder == null: " + (currentLadder == null));
            bool regularMovementState = (IsGrounded() || IsFallingInTheAir());
            if (regularMovementState)
            {
                float currentAcceleration = (IsGrounded() ? GROUND_ACCELERATION : AIR_ACCELERATION);
                float currentDeceleration = (IsGrounded() ? GROUND_DECELERATION : AIR_DECELERATION);

                if (moveInput != Vector2.zero)
                {     //accelerate
                    if (isStunned)
                    {
                        return;
                    }

                    //TurnCheck();

                    Vector2 targetVelocity = new Vector2(moveInput.x, 0);
                    targetVelocity *= (holdingRun ? MAX_RUN_SPEED : MAX_WALK_SPEED);

                    moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
                    rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
                }
                else
                {      //decelerate
                    moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, currentDeceleration * Time.deltaTime);
                    rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
                }
            }



            //synchronized variable
            if (winningPlayer != null)
            {
                //win game sfx played in game manager
                //preserve z position of camera
                Vector3 newCamPos = Camera.main.transform.position;
                newCamPos.x = winningPlayer.transform.position.x;
                newCamPos.y = winningPlayer.transform.position.y;

                Camera.main.transform.position = newCamPos;
                Camera.main.orthographicSize = 3f;
            }
            else if (!camFrozen)
            {
                Vector3 newCamPos = new Vector3(0, 0, cam.transform.position.z);
                newCamPos.x = GameManager.CENTER_PIECE_X;
                //Mathf.infinity is not bad on performance at all since it is stored as some sort of constant
                newCamPos.y = Mathf.Clamp(this.transform.position.y + 5, -Mathf.Infinity, highestCamY);

                //use Vector3 lerp because Vector2.lerp puts camera z at 0 and messes up the view
                cam.transform.position = Vector3.Lerp(cam.transform.position, newCamPos, camAccel);
            }
        }

        if (IsServer)
        {
            //for countdown
            if (playerFrozen)
            {
                rigidbody.velocity = Vector2.zero;

                //this is some fucking terrible code, but game expo starts soon
                SendUpdate("PLAYER_FROZEN", playerFrozen.ToString());
                return;
            }

            //only sendupdate when switching layers
            //phase through jump thrus when jumping up
            if (verticalVelocity > 0.000001f && this.gameObject.layer == normalLayer && !IsClimbing() && !IsSwinging())
            {
                this.gameObject.layer = noJumpThruLayer;
                SendUpdate("DISABLE_JUMP_THRU_COLLISION", "");
            }
            //enable jump thru collision when falling
            else if (verticalVelocity < 0.000001f && this.gameObject.layer == noJumpThruLayer && !IsClimbing() && !IsSwinging())
            {
                this.gameObject.layer = normalLayer;
                SendUpdate("ENABLE_JUMP_THRU_COLLISION", "");
            }


            if (jankGroundGravity)
            {
                StartCoroutine(GroundCollisionGravity());
                SendUpdate("ENABLE_CLIENT_GRAVITY", "GoodMorning");
                jankGroundGravity = false;
            }

            //Debug.Log("CurrentMovementState: " + currentMovementState);
            //Debug.Log("Move input" + moveInput);
            //Debug.Log("Num jumps used: " + numJumpsUsed);
            bool onGround = CheckForGround();


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

                    if (selectedCharacterClass != characterClass.BANDIT)
                    {
                        SendUpdate("MOVEMENT_RECHARGE_SFX", "");
                    }

                    SendUpdate("LERP_GOLD", "");
                    //StartCoroutine(LerpToGold());
                }
            }

            if (movementAbilityPressed && !inMovementAbilityCooldown)
            {
                if (selectedCharacterClass == characterClass.ARCHER)
                {
                    float yOffset = 2f;
                    Vector2 ropeArrowPos = new Vector2(this.transform.position.x, this.transform.position.y + yOffset);
                    float ropeArrowSpeed = 22f;
                    Vector2 direction = new Vector2(0f, 1f);
                    Quaternion arrowDirection;

                    SendUpdate("SHOOT_ANIM", "");

                    //only 3 directions: top left, top right, and top
                    if (moveInput.x > 0.01f)
                    {
                        direction.x = 1f;
                        arrowDirection = Quaternion.Euler(0f, 0f, 45f);
                    }
                    else if (moveInput.x < -0.01f)
                    {
                        direction.x = -1f;
                        arrowDirection = Quaternion.Euler(0f, 0f, 135f);
                    }
                    else
                    {
                        arrowDirection = Quaternion.Euler(0f, 0f, 90f);
                    }

                    GameObject ropeArrow = MyCore.NetCreateObject(ROPE_ARROW_SPAWN_PREFAB_INDEX, Owner, ropeArrowPos, arrowDirection);
                    ropeArrow.GetComponent<RopeArrow>().currentPlayer = this;
                    ropeArrow.GetComponent<Rigidbody2D>().velocity = direction.normalized * ropeArrowSpeed;

                    inMovementAbilityCooldown = true;
                }
                else if (selectedCharacterClass == characterClass.MAGE)
                {
                    SendUpdate("DASH_SFX", "");
                    currentMovementState = movementState.DASHING;
                    dashTimer = MAX_DASH_TIME;   
                    SendUpdate("START_DASH_EFFECT", "GoodMorning");
                    SendUpdate("RUMBLE", "GoodMorning");

                    //may want to normalize the vector if we don't want diagonals to be op
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
                else if (selectedCharacterClass == characterClass.KNIGHT)
                {
                    currentMovementState = movementState.KNIGHT_DASHING;
                    SendUpdate("LUNGE_SFX", "");
                    //make these different variables if we want it to be different for the knight
                    //!This is intentional! The knight has a special dash timer!
                    dashTimer = 0.3f; 
                    //SendUpdate("START_KNIGHT_DASH_EFFECT", "GoodMorning");
                    SendUpdate("RUMBLE", "GoodMorning");
                    SendUpdate("LUNGE_ANIM", "");

                    Vector2 dashVelocity;

                    float xDirection = (isFacingRight ? 1f : -1f);
                    float yDirection = 0f;

                    dashVelocity = new Vector2(xDirection * knightDashSpeed, yDirection);
                    verticalVelocity = dashVelocity.y;
                    rigidbody.velocity = dashVelocity;

                    inMovementAbilityCooldown = true;
                    return;
                }
            }

            if (attackPressed)
            {
                Attack();
            }

            if (!onGround)
            {
                coyoteTimer -= Time.deltaTime;
            }
            else
            {
                coyoteTimer = MAX_JUMP_COYOTE_TIME;
            }


            #region SPECIAL_CASES
            //can't bypass jumping code cause we need gravity to work to be bypassed by special movement states
            if (canGrabRope && CheckForTriggers("Rope")/*CheckForRopes()*/)
            {
                if (currentRope != null)
                {
                    /*SendUpdate("PARENT_TO_ROPE", currentRope.transform.position.ToString());
                    transform.SetParent(currentRope.pivotRig.transform);
                    rigidbody.velocity = Vector2.zero;*/

                    canGrabRope = false;
                    currentRope.GrabRope(this);

                    this.gameObject.layer = noJumpThruLayer;
                    SendUpdate("DISABLE_JUMP_THRU_COLLISION", "");

                    currentMovementState = movementState.SWINGING;
                    canRopeJump = false;
                    //prevents player from immediately jumping if they're holding jump while collding with a rope
                    StartCoroutine(RopeJumpCooldown(0.1f));
                    SendUpdate("JUMP_ANIM", "");

                    feetCollider.enabled = false;
                    bodyCollider.enabled = false;
                    SendUpdate("DISABLE_COLLIDERS", "GoodMorning");
                }
            }

            if (CheckForTriggers("Ladder")/*CheckForLadders()*/)
            {
                bool pressingUpOrDown = (moveInput.y > 0f || moveInput.y < 0f);
                if ((currentLadder != null) && pressingUpOrDown && !IsClimbing() && !inDismountTrigger)
                {
                    Debug.Log("set to ladder state");
                    currentLadder.InitializeLadderVariables(this);
                    currentMovementState = movementState.CLIMBING;
                    SendUpdate("LADDER_ANIM", "");
                }
            }

            if (IsSwinging())
            {
                if (jumpReleased)
                {
                    onRopeWithoutJumpPressed = true;
                }
                else if (jumpPressed && onRopeWithoutJumpPressed)
                {
                    ropeJumpPressed = true;
                }

                if (ropeJumpPressed && canRopeJump)
                {
                    currentRope.BoostPlayer(this);
                    //transform.SetParent(null);
                    //SendUpdate("UNPARENT", "");

                    this.gameObject.layer = normalLayer;
                    SendUpdate("ENABLE_JUMP_THRU_COLLISION", "");

                    currentMovementState = movementState.LAUNCHING;
                    currentRope.playerPresent = false;
                    currentRope.SendUpdate("ROPE_JUMP_SFX", "");
                    currentRope = null;
                    StartCoroutine(GrabCooldown(1f));

                    feetCollider.enabled = true;
                    bodyCollider.enabled = true;
                    SendUpdate("ENABLE_COLLIDERS", "GoodMorning");
                    //hopefully fixes weird issue with colliders being permanently disabled
                    StartCoroutine(EnsureEnabledColliders());
                }
                else
                {
                    //don't worry about horizontal movement cause it's already taken care of in the rope script
                    rigidbody.velocity = (swingPos.position - transform.position) * currentRope.swingSnapMult;
                    //rigidbody.velocity = Vector2.zero;
                }

                return;
            }

            if (archerArrowHitObj && (archerArrowHitPosition != null))
            {
                //SendUpdate("DASH_SFX", "");
                //currentMovementState = movementState.DASHING;
                dashTimer = MAX_DASH_TIME;
                //SendUpdate("START_DASH_EFFECT", "GoodMorning");
                SendUpdate("RUMBLE", "GoodMorning");

                Vector2 arrowPos = new Vector2(archerArrowHitPosition.position.x, archerArrowHitPosition.position.y);
                Vector2 direction = (arrowPos - rigidbody.position).normalized;

                Vector2 tempVelocity = direction * archerGrappleSpeed;
                verticalVelocity = tempVelocity.y;
                rigidbody.velocity = tempVelocity;

                currentMovementState = movementState.GRAPPLING;

                archerArrowHitObj = false;
                archerArrowHitPosition = null;

                return;
            }

            //3 movement abilities
            if (IsGrappling())
            {
                if ((dashTimer > 0f) && !CheckForCeiling())
                {
                    dashTimer -= Time.deltaTime;

                    //returning instead of giving the player midair control makes it feel more like a Celeste-style dash
                    return;
                }
                else
                {
                    dashTimer = MAX_DASH_TIME;
                    currentMovementState = movementState.FALLING;
                }
            }

            if (IsDashing())
            {
                if ((dashTimer > 0f) && !CheckForCeiling())
                {
                    dashTimer -= Time.deltaTime;

                    //returning instead of giving the player midair control makes it feel more like a Celeste-style dash
                    return;
                }
                else
                {
                    dashTimer = MAX_DASH_TIME;
                    currentMovementState = movementState.FALLING;
                }
            }

            if (IsKnightDashing())
            {
                if (dashTimer > 0f)
                {
                    //hit wall while dashing horizontally
                    if ((verticalVelocity <= 0f) && CheckForWalls())
                    {
                        Vector2 dashVelocity;

                        //flip velocities but retain x velocity a little
                        float xVelocity = (isFacingRight ? (knightDashSpeed / 8f) : (-knightDashSpeed / 8f));
                        float yVelocity = knightDashSpeed - 6;

                        dashVelocity = new Vector2(xVelocity, yVelocity);
                        verticalVelocity = dashVelocity.y;
                        rigidbody.velocity = dashVelocity;
                        SendUpdate("JUMP_ANIM", "");
                        SendUpdate("LUNGE_HIT_SFX", "");
                        //dashTimer = MAX_DASH_TIME;
                        //inMovementAbilityCooldown = true;
                    }

                    dashTimer -= Time.deltaTime;

                    //returning instead of giving the player midair control makes it feel more like a Celeste-style dash
                    return;
                }
                else
                {
                    dashTimer = MAX_DASH_TIME;
                    currentMovementState = movementState.FALLING;
                    SendUpdate("IDLE_ANIM", "");
                }
            }

            if (IsLaunching() && (verticalVelocity < 0f))
            {
                currentMovementState = movementState.FAST_FALLING;
            }


            if (IsClimbing())
            {
                if (currentLadder == null)
                {
                    Debug.LogError("Thou art trying to climbeth a ladder that existeth not?");
                    return;
                }

                //ik this is copy paste from the jumping code, but idk a different way to do this that doesn't involve returning out of Update()
                //at specific points
                bool ladderJump = (jumpPressed && IsClimbing());
                if (ladderJump)
                {
                    InitiateJump(1);
                    SendUpdate("JUMP_ANIM", "");
                    //currentLadder.SendUpdate("STOP_LADDER_SFX", "GoodMorning");
                    //currentLadder.SendUpdate("DETACH_PLAYER", "");
                    currentLadder.attachedPlayer = null;
                    currentLadder = null;

                    if (jumpReleasedDuringBuffer)
                    {
                        currentMovementState = movementState.FAST_FALLING;
                        fastFallReleaseSpeed = verticalVelocity;
                    }

                    return;
                }

                //don't allow player to climb on ladder if they have dismounted above the ladder
                if (currentMovementState != movementState.CLIMBING && inDismountTrigger)
                    return;

                if (moveInput.y > 0f)
                {
                    rigidbody.velocity = new Vector2(0, currentLadder.ladderSpeed);
                }
                else if (moveInput.y < 0f)
                {
                    if (onGround)
                    {
                        //need this cause we only want to SendUpdate() when the game state has changed
                        if (!IsGrounded())
                        {
                            currentMovementState = movementState.GROUND;
                            SendUpdate("IDLE_ANIM", "GoodMorning");
                            //currentLadder.SendUpdate("STOP_LADDER_SFX", "GoodMorning");
                            currentLadder.attachedPlayer = null;
                            //currentLadder.SendUpdate("DETACH_PLAYER", "");
                            currentLadder = null;

                            return;
                        }
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
                    if (!IsGrounded())
                    {
                        currentMovementState = movementState.GROUND;
                        SendUpdate("IDLE_ANIM", "GoodMorning");
                    }

                    //currentLadder.SendUpdate("DETACH_PLAYER", "");
                    rigidbody.velocity = Vector2.zero;
                    //currentLadder.SendUpdate("STOP_LADDER_SFX", "GoodMorning");
                    currentLadder.attachedPlayer = null;
                    currentLadder = null;
                    verticalVelocity = 0f;


                    float yOffset = 0.05f;
                    //
                    /*
                    float height = bodyCollider.bounds.size.y + feetCollider.bounds.size.y + yOffset;
					//raycast to floor instead
					Vector3 playerTop = transform.position + new Vector3(0, height / 2, 0);
					//if we have issues with tilemap being floor layer, we can change it back and just use ~0 for the last parameter here
                    RaycastHit2D floor = Physics2D.Raycast(playerTop, Vector2.down, height, floorLayer);
                    Vector2 dismountPos = new Vector2(this.transform.position.x, GetTileUpperY(floor));
					dismountPos.y += (height / 2) + yOffset;
                    */
                    Vector2 dismountPos = new Vector2(this.transform.position.x, this.transform.position.y + yOffset);
                    transform.position = dismountPos;

                    SendUpdate("DISMOUNT", dismountPos.ToString());
                }

                return;
            }
            #endregion

            #region JUMPING
            jumpBufferTimer -= Time.deltaTime;

            if (IsJumping())
            {
                if (beenInAirLongEnough && jumpReleased)
                {
                    jumpReleasedDuringJump = true;
                    held = false;
                }
            }

            if (InTheAir())
            {
                if (jumpReleasedDuringJump && jumpPressed)
                {
                    //Debug.Log("held is happening");
                    StartCoroutine(TestForHeld());
                }


                /*
                //maybe commment this out
                if (held && jumpReleasedDuringJump){
                    held = false;
                }
                */
            }


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
            bool jumpFromGround = (jumpPressed && IsGrounded() && !held);
            //bool normalJump = (jumpBufferTimer > 0f && !IsJumping() && (onGround || ((coyoteTimer > 0f) && JumpPressedAgainWhileFalling())));
            bool normalJump = (!held && jumpBufferTimer > 0f && IsFallingInTheAir() && (onGround || ((coyoteTimer > 0f) && JumpPressedAgainWhileFalling())));

            //make sure x input isn't 0
            //remove !onGround if knight dashing still screws up
            bool wallJump = (wallsTimer > 0f && onWall && wallJumpPressed && CanWallJump() && (moveInput.x != 0f) && !onGround);
            //change these two as well to jumpBufferTimer > 0 if doesn't work
            bool extraJump = (jumpPressed && IsFastFalling() && (numJumpsUsed < MAX_JUMPS) && JumpPressedAgainWhileFalling());
            bool airJump = (jumpPressed && IsFallingInTheAir() && (numJumpsUsed < MAX_JUMPS - 1));

            if (jumpFromGround)
            {
                Debug.Log("jumping from ground");
            }


            if (jumpFromGround || normalJump)
            {
                Debug.Log("normal jump");
                InitiateJump(1);
                SendUpdate("JUMP_ANIM", "");
                SendUpdate("JUMP_SFX", "");

                if (jumpReleasedDuringBuffer)
                {
                    currentMovementState = movementState.FAST_FALLING;
                    fastFallReleaseSpeed = verticalVelocity;
                }
            }
            else if (wallJump)
            {
                InitiateWallJump();
                SendUpdate("JUMP_ANIM", "");
                SendUpdate("DOUBLE_JUMP_SFX", "");
            }
            else if (extraJump)
            {
                Debug.Log("Extra jump");
                SendUpdate("DOUBLE_JUMP_SFX", "");
                InitiateJump(1);
                SendUpdate("JUMP_ANIM", "");
            }
            else if (airJump)
            {
                Debug.Log("air jump");
                SendUpdate("DOUBLE_JUMP_SFX", "");
                //forces player to only get one jump when they're falling, so they can't fall off a ledge and then jump twice.
                InitiateJump(2);
                SendUpdate("JUMP_ANIM", "");
            }

            bool justLanded = (InTheAir() && onGround && (verticalVelocity <= 0f));
            if (justLanded)
            {
                currentMovementState = movementState.GROUND;
                SendUpdate("IDLE_ANIM", "GoodMorning");
                clientCollidersEnabled = true;
                SendUpdate("ENABLE_COLLIDERS", "GoodMorning");
                jumpReleasedDuringJump = false;
                //held = false;
                JumpVariableCleanup();
                RopeVariableCleanup();

                //force it to reset cause Unity's input system is a piece of dogcrap, and otherwise it'll infintely jump
                jumpPressed = false;
                jumpReleased = true;
                SendUpdate("JUMP_RELEASED", "GoodMorning");

                jankGroundGravity = true;
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

                //only calculate velocity on the first frame of the wall jump so we only have to check collision once, and so that the wall jump
                //doesn't get cut off prematurely
                if (justStartedWallJump)
                {
                    //float xDirection = (isFacingRight ? 1f : -1f);

                    float xDirection;
                    //can't do moveInput.x cause that would be using player input
                    if (CanLeftWallJump())
                    {
                        xDirection = 1f;
                    }
                    else if (CanRightWallJump())
                    {
                        xDirection = -1f;
                    }
                    else
                    {
                        xDirection = 0f;
                        Debug.LogError("How can thou expect to jumpeth on a wall that doth not exist?");
                    }

                    Vector2 targetVelocity = new Vector2(xDirection * WALL_JUMP_HORIZONTAL_BOOST, 0f);

                    moveVelocity = Vector2.Lerp(new Vector2(xDirection / 10f, 0f), targetVelocity, WALL_JUMP_ACCELERATION * Time.deltaTime);
                    rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);

                    justStartedWallJump = false;
                }

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

                //if this is too slow, change MAX_WALK_SPEED to a const with a higher value
                float allowedXSpeed = Mathf.Max(Mathf.Abs(ropeLaunchVec.x), MAX_LAUNCH_SPEED);
                newXVel = Mathf.Clamp(newXVel, -Mathf.Abs(allowedXSpeed), Mathf.Abs(allowedXSpeed));

                rigidbody.velocity = new Vector2(newXVel, rigidbody.velocity.y);

                return;
            }

            if (onGround && !IsJumping() && !InSpecialMovementState())
            {
                if (!IsGrounded())
                {
                    currentMovementState = movementState.GROUND;

                    SendUpdate("IDLE_ANIM", "GoodMorning");

                    jankGroundGravity = true;
                }

                //beenInAirLongEnough = false;
                JumpVariableCleanup();
            }



            float currentAcceleration = (IsGrounded() ? GROUND_ACCELERATION : AIR_ACCELERATION);
            float currentDeceleration = (IsGrounded() ? GROUND_DECELERATION : AIR_DECELERATION);


            if (moveInput != Vector2.zero)
            {     //accelerate
                if (isStunned)
                {
                    return;
                }

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
            }
            #endregion
        }
    }
}