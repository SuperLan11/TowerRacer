/*
@Authors - Patrick
@Description - Player Script
*/
//Character controller datafields and methods are courtesy of of Sasquatch B Studios.
//https://youtu.be/zHSWG05byEc?si=_eNhW3uz9ZkeFVMr

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

using NETWORK_ENGINE;
using System;

/*
TODO
    2. Send Updates for sync vars if dirty
    3. Look through skeleton code for things like animations
    4. start programming different abilities
*/

public class Player : NetworkComponent {
    //!If you add a variable to this, you are responsible for making sure it goes in the IsDirty check in SlowUpdate()
    #region SyncVars

    private Vector2 moveVelocity;
    
    private float verticalVelocity;
    
    private bool isFacingRight;

    private float fastFallTime;
    private float fastFallReleaseSpeed;

    private int numJumpsUsed;
    private float apexPoint;
    private float timePastApexThreshold;
    private bool isPastApexThreshold;
    private float jumpBufferTimer;
    private bool jumpReleasedDuringBuffer;
    private float coyoteTimer;

    private bool jumpPressed = false;
    private bool jumpReleased = false;

    //I doubt we'll want run button in our game, but it's here just in case
    private bool holdingRun = false;

    [SerializeField] private movementState currentMovementState;

    #endregion






    #region NonSync Vars

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private BoxCollider2D feetCollider;
    [SerializeField] private BoxCollider2D bodyCollider;
    private Rigidbody2D rigidbody;
    
    private const float MAX_WALK_SPEED = 12.5f;
    private const float GROUND_ACCELERATION = 5f, GROUND_DECELERATION = 20f;
    private const float AIR_ACCELERATION = 5f, AIR_DECELERATION = 5f;

    private const float MAX_RUN_SPEED = 20f;
    
    //these values will probably need to change based on the size of the Player
    
    private const float COLLISION_RAYCAST_LENGTH = 0.02f;

    private const float JUMP_HEIGHT = 6.5f;
    private const float JUMP_HEIGHT_COMPENSATION_FACTOR = 1.054f;
    //apex = max height of jump
    private const float TIME_TILL_JUMP_APEX = 0.35f;
    private const float GRAVITY_RELEASE_MULTIPLIER = 2f;
    private const float MAX_FALL_SPEED = 26f;
    //no need to have canDoubleJump or anything like that since we have this int
    private const int MAX_JUMPS = 1;

    private const float TIME_FOR_UP_CANCEL = 0.027f;
    private const float APEX_THRESHOLD = 0.97f, APEX_HANG_TIME = 0.075f;
    private const float MAX_JUMP_BUFFER_TIME = 0.125f;
    private const float MAX_JUMP_COYOTE_TIME = 0.1f;

    private float gravity;
    private float initialJumpVelocity;
    private float adjustedJumpHeight;

    private Vector2 moveInput;

    private Vector2 upNormal = new Vector2(0, 1f);    
    private Vector2 downNormal = new Vector2(0, -1f); 
    private Vector2 leftNormal = new Vector2(-1f, 0);
    private Vector3 rightNormal = new Vector2(1f, 0);

    private enum movementState
    {
            GROUND,
            JUMPING,    //!jumping means you are in the air with the jump button pressed or held down
            FALLING,
            FAST_FALLING,  
            SWINGING,
            CLIMBING
    };

    #endregion
    



    //!For the else {} debug to work, you NEED to check IsServer or IsClient INSIDE of the flag if statement!
    public override void HandleMessage(string flag, string value)
    {
        if (flag == "MOVE"){
            if (IsServer){
                //not a sync var, but still needs to be set on the server
                moveInput = Player.Vector2FromString(value);
            }
        }else if (flag == "JUMP_PRESSED"){
            jumpPressed = true;
            jumpReleased = false;
            
            if (IsServer){
                SendUpdate("JUMP_PRESSED", "GoodMorning");
            }
        }else if (flag == "JUMP_RELEASED"){
            jumpPressed = false;
            jumpReleased = true;
            
            if (IsServer){
                SendUpdate("JUMP_RELEASED", "GoodMorning");
            }
        }
        else{      //!Ask Landon later to do this part!
            //something like...
            Debug.Log("Thine flag name is INCORRECT!");
        }
    }

    public static Vector2 Vector2FromString(string v)
    {
        string raw = v.Trim('(').Trim(')');
        string [] args = raw.Split(',');
        return new Vector2(float.Parse(args[0].Trim()), float.Parse(args[1].Trim()));
    }

    public override void NetworkedStart()
    {
        CalculateInitialConditions();
        
        isFacingRight = true;

        currentMovementState = movementState.GROUND;
    }

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
    }

    //go watch the GDC talk if you want know why this math works
    private void CalculateInitialConditions(){
        //proper way to do this would probably be just to modify jump height, but I'm lazy
        float patrickBSAdjuster = 0.1f;
        
        adjustedJumpHeight = JUMP_HEIGHT * JUMP_HEIGHT_COMPENSATION_FACTOR;
        
        //g = (-2 * h) / t^2
        gravity = -1 * (2f * adjustedJumpHeight) / Mathf.Pow(TIME_TILL_JUMP_APEX, 2f) * patrickBSAdjuster;
        
        initialJumpVelocity = Mathf.Abs(gravity);
    }

    //only gets called in server
    private void SetApexVariables(){
        if (!isPastApexThreshold){
            isPastApexThreshold = true;
            timePastApexThreshold = 0f;
        }else{
            timePastApexThreshold += Time.deltaTime;
            if (timePastApexThreshold < APEX_HANG_TIME){
                verticalVelocity = 0f;
            }else{
                verticalVelocity = -0.01f;
            }
        }
    }

    //only gets called in server
    private void GravityOnAscending(){
        verticalVelocity += gravity * Time.deltaTime;

        if (isPastApexThreshold){
            isPastApexThreshold = false;
        }
    }

    //only gets called in server
    private void TurnCheck(){
        bool turnRight;

        if (isFacingRight && moveInput.x < 0){
            turnRight = false;
            Turn(turnRight);
        }else if (!isFacingRight && moveInput.x > 0){
            turnRight = true;
            Turn(turnRight);
        }
    }

    //only gets called in server
    private void Turn(bool turnRight){
        isFacingRight = turnRight;

        if (isFacingRight){
            transform.Rotate(0f, 180f, 0f);
        }else{
            transform.Rotate(0f, -180f, 0f);
        }
    }

    private bool CheckForGround(){
        if (IsServer){
            Vector2 tempPos = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y - COLLISION_RAYCAST_LENGTH);
            RaycastHit2D hit = Physics2D.Raycast(tempPos, Vector2.down, COLLISION_RAYCAST_LENGTH, ~0);
            //DrawDebugNormal(tempPos, Vector2.down, GROUND_DETECTION_RAY_LENGTH, false);

            return (hit.collider != null && (hit.normal == upNormal));
        }
        
        return false;
    }

    private bool CheckForCeiling(){
        if (IsServer){
            Vector2 tempPos = new Vector2(bodyCollider.bounds.center.x, bodyCollider.bounds.max.y + COLLISION_RAYCAST_LENGTH);
            RaycastHit2D hit = Physics2D.Raycast(tempPos, Vector2.up, COLLISION_RAYCAST_LENGTH, ~0);
            //DrawDebugNormal(tempPos, Vector2.up, GROUND_DETECTION_RAY_LENGTH, false);

            return (hit.collider != null && (hit.normal == downNormal));
        }

        return false;
    }

    //if we want a function for just the left or right wall, we'll want to modify this function and then create two new helper functions
    private bool CheckForWalls(){
        if (IsServer){
            Vector2 leftTempPos = new Vector2(bodyCollider.bounds.min.x - COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D leftHit = Physics2D.Raycast(leftTempPos, Vector2.left, COLLISION_RAYCAST_LENGTH, ~0);
            //DrawDebugNormal(leftTempPos, Vector2.left, GROUND_DETECTION_RAY_LENGTH, true);
            
            Vector2 rightTempPos = new Vector2(bodyCollider.bounds.max.x + COLLISION_RAYCAST_LENGTH, bodyCollider.bounds.center.y);
            RaycastHit2D rightHit = Physics2D.Raycast(rightTempPos, Vector2.right, COLLISION_RAYCAST_LENGTH, ~0);
            //DrawDebugNormal(rightTempPos, Vector2.right, GROUND_DETECTION_RAY_LENGTH, true);

            bool leftCollision = (leftHit.collider != null && (leftHit.normal == (Vector2)rightNormal));
            bool rightCollision = (rightHit.collider != null && (rightHit.normal == leftNormal));

            return (leftCollision || rightCollision);
        }

        return false;
    }

    //!You must be in scene view for this to show up
    private void DrawDebugNormal(Vector2 pos, Vector2 unitVector, float length, bool makeLargeForVisibility = true){
        if (makeLargeForVisibility){
            length *= 20f;
        }

        Debug.DrawRay(pos, unitVector * length, Color.red);
    }

    private bool IsGrounded(){
        return (currentMovementState == movementState.GROUND);
    }

    private bool IsJumping(){
        return (currentMovementState == movementState.JUMPING);
    }

    private bool IsFalling(){
        return (currentMovementState == movementState.FALLING);
    }

    private bool IsFastFalling(){
        return (currentMovementState == movementState.FAST_FALLING);
    }

    private bool IsFallingInTheAir(){
        return (IsFalling() || IsFastFalling());
    }

    private bool InTheAir(){
        return (IsJumping() || IsFallingInTheAir());
    }
    public void MoveAction(InputAction.CallbackContext context){
        if (IsLocalPlayer){
            if (context.started || context.performed){
                moveInput = context.ReadValue<Vector2>();
                SendCommand("MOVE", moveInput.ToString());
            }else if (context.canceled){
                moveInput = Vector2.zero;
                SendCommand("MOVE", moveInput.ToString());
            }
        }
    }

    public void JumpAction(InputAction.CallbackContext context){
        if (IsLocalPlayer){
            if (context.started || context.performed){
                SendCommand("JUMP_PRESSED", "GoodMorning");
            }else if (context.canceled){
                SendCommand("JUMP_RELEASED", "GoodMorning");
            }
        }
    }

    //only gets called on server
    private void JumpVariableCleanup(){
        fastFallTime = 0f;
        isPastApexThreshold = false;
        verticalVelocity = 0f;
        numJumpsUsed = 0;
    }

    //only gets called on server
    private void InitiateJump(int jumps){
        if (!IsJumping()){
            currentMovementState = movementState.JUMPING;
        }

        jumpBufferTimer = 0;
        numJumpsUsed += jumps;
        verticalVelocity = initialJumpVelocity;

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);
    }

    //order is going to matter a lot here
    //we're checking collision here rather than in any of the OnCollision() Unity methods
    void Update()
    {    
        //Debug.Log("CurrentMovementState: " + currentMovementState);
        //Debug.Log("Move input" + moveInput);
        
        if (!MyId.IsInit){
            return;
        }


        if (IsServer){
            //"Update"
            jumpBufferTimer -= Time.deltaTime;

            if (!CheckForGround()){
                coyoteTimer -= Time.deltaTime;
            }else{
                coyoteTimer = MAX_JUMP_COYOTE_TIME;
            }


            if (jumpPressed){
                jumpBufferTimer = MAX_JUMP_BUFFER_TIME;
                jumpReleasedDuringBuffer = false;
            }
            
            if (jumpReleased){
                if (jumpBufferTimer > 0f){
                    jumpReleasedDuringBuffer = true;
                }

                //letting go of jump while still moving upwards is what causes fast falling
                if (IsJumping() && verticalVelocity > 0f){
                    currentMovementState = movementState.FAST_FALLING;
                    
                    if (isPastApexThreshold){
                        isPastApexThreshold = false;
                        fastFallTime = TIME_FOR_UP_CANCEL;
                        verticalVelocity = 0;
                    }else{
                        fastFallReleaseSpeed = verticalVelocity;
                    }
                }
            }

            //no need to check for isJumpPressed since that's what jumpBufferTimer is doing
            bool normalJump = (jumpBufferTimer > 0f && !IsJumping() && (CheckForGround() || coyoteTimer > 0f));
            //double jumps use jumpPressed cause they cause a bug with jumpBufferTimer. Shouldn't be needed cause there's no need to buffer jumps in
            //the air
            //bool doubleJump = (jumpPressed && IsFastFalling() && (numJumpsUsed < MAX_JUMPS));
            bool extraJump = (jumpBufferTimer > 0f && IsFastFalling() && (numJumpsUsed < MAX_JUMPS));
            bool airJump = (jumpBufferTimer > 0f && IsFallingInTheAir() && (numJumpsUsed < MAX_JUMPS - 1));
        
            if (normalJump){
                InitiateJump(1);
                
                if (jumpReleasedDuringBuffer){
                    currentMovementState = movementState.FAST_FALLING;
                    fastFallReleaseSpeed = verticalVelocity;
                }
            }else if (extraJump){
                InitiateJump(1);
            }else if (airJump){
                //forces player to only get one jump when they're falling, so they can't fall off a ledge and then jump twice.
                InitiateJump(2);
                
                currentMovementState = movementState.FAST_FALLING;
            }

            bool justLanded = (IsJumping() || IsFallingInTheAir()) && CheckForGround() && (verticalVelocity <= 0f);
            if (justLanded){
                currentMovementState = movementState.GROUND;
                JumpVariableCleanup();
            }


            //"FixedUpdate"
            bool onGround = CheckForGround();

            //Vertical Velocity
            if (IsJumping()){
                //! If we want our air movement to feel more float, we'll probably want to comment this out. This is a question for Mr. Game Design
                if (CheckForCeiling()){
                    currentMovementState = movementState.FAST_FALLING;
                }

                if (verticalVelocity >= 0f){
                    apexPoint = Mathf.InverseLerp(initialJumpVelocity, 0f, verticalVelocity);
                    if (apexPoint > APEX_THRESHOLD){
                        SetApexVariables();
                    }else{
                        GravityOnAscending();;
                    }
                }else if (!IsFastFalling()){
                    verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.deltaTime;
                }else if (verticalVelocity < 0f){
                    if (!IsFallingInTheAir()){
                        currentMovementState = movementState.FALLING;
                    }
                }
            }

            if (IsFastFalling()){
                if (fastFallTime >= TIME_FOR_UP_CANCEL){
                    verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.deltaTime;
                }else if (fastFallTime < TIME_FOR_UP_CANCEL){
                    verticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / TIME_FOR_UP_CANCEL));
                }

                fastFallTime += Time.deltaTime;
            }

            if (!CheckForGround() && !IsJumping()){
                if (!IsFallingInTheAir()){
                    currentMovementState = movementState.FALLING;
                }

                verticalVelocity += gravity * Time.deltaTime;
            }

            verticalVelocity = Mathf.Clamp(verticalVelocity, -MAX_FALL_SPEED, 50f);
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);


            //Horizontal Velocity
            if (onGround){
                currentMovementState = movementState.GROUND;
                JumpVariableCleanup();
            }

            float currentAcceleration = (IsGrounded() ? GROUND_ACCELERATION : AIR_ACCELERATION);
            float currentDeceleration = (IsGrounded() ? GROUND_DECELERATION : AIR_DECELERATION);
            
            if (moveInput != Vector2.zero){     //accelerate
                TurnCheck();
                
                Vector2 targetVelocity = new Vector2(moveInput.x, 0);
                targetVelocity *= (holdingRun ? MAX_RUN_SPEED : MAX_WALK_SPEED);

                moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
                rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
            }else{      //decelerate
                moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, currentDeceleration * Time.deltaTime);
                rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
            }
        }
    }

    public override IEnumerator SlowUpdate()
    {
        while (IsConnected){
            if (IsServer){

                if (IsDirty){
                    //SendUpdate for all of sync vars

                    IsDirty = false;
                }
            }

            yield return new WaitForSeconds(MyCore.MasterTimer);
        }
    }
}