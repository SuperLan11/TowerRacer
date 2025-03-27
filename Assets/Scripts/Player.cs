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

/*
TODO
    3. Figure out wtf is going on with double jumps
    5. Use surface normals for collision detection
*/

public class Player : MonoBehaviour {
    #region SyncVars

    private Vector2 moveVelocity;
    private Vector2 moveInput;
    
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

    //I doubt we'll want this, but it's here just in case
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
    
    private const float GROUND_DETECTION_RAY_LENGTH = 0.02f, HEAD_DETECTION_RAY_LENGTH = 0.02f;

    private const float JUMP_HEIGHT = 6.5f;
    private const float JUMP_HEIGHT_COMPENSATION_FACTOR = 1.054f;
    //apex = max height of jump
    private const float TIME_TILL_JUMP_APEX = 0.35f;
    private const float GRAVITY_RELEASE_MULTIPLIER = 2f;
    private const float MAX_FALL_SPEED = 26f;
    private const int MAX_JUMPS = 2;

    private const float TIME_FOR_UP_CANCEL = 0.027f;
    private const float APEX_THRESHOLD = 0.97f, APEX_HANG_TIME = 0.075f;
    private const float MAX_JUMP_BUFFER_TIME = 0.125f;
    private const float MAX_JUMP_COYOTE_TIME = 0.1f;

    private float gravity;
    private float initialJumpVelocity;
    private float adjustedJumpHeight;

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
    


    //add networking bullcrap here

    private void Awake()
    {
        isFacingRight = true;
        rigidbody = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        CalculateInitialConditions();
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

    private void SetApexVariables(){
        if (!isPastApexThreshold){
            isPastApexThreshold = true;
            timePastApexThreshold = 0f;
        }else{
            timePastApexThreshold += Time.fixedDeltaTime;
            if (timePastApexThreshold < APEX_HANG_TIME){
                verticalVelocity = 0f;
            }else{
                verticalVelocity = -0.01f;
            }
        }
    }

    private void GravityOnAscending(){
        verticalVelocity += gravity * Time.fixedDeltaTime;

        if (isPastApexThreshold){
            isPastApexThreshold = false;
        }
    }

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

    private void Turn(bool turnRight){
        isFacingRight = turnRight;

        if (isFacingRight){
            transform.Rotate(0f, 180f, 0f);
        }else{
            transform.Rotate(0f, -180f, 0f);
        }
    }

    private bool CheckForGround(){
        Vector2 boxOrigin = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.min.y);
        Vector2 boxSize = new Vector2(feetCollider.bounds.size.x, GROUND_DETECTION_RAY_LENGTH);

        RaycastHit2D groundHit = Physics2D.BoxCast(boxOrigin, boxSize, 0, Vector2.down, GROUND_DETECTION_RAY_LENGTH, groundLayer);

        return (!(groundHit.collider == null));
    }

    private bool CheckForCeiling(){
        //doing a little bit of math cause we don't have the head collider directly
        Vector2 boxOrigin = new Vector2(feetCollider.bounds.center.x, feetCollider.bounds.max.y);
        Vector2 boxSize = new Vector2(feetCollider.bounds.size.x, HEAD_DETECTION_RAY_LENGTH);

        RaycastHit2D headHit = Physics2D.BoxCast(boxOrigin, boxSize, 0, Vector2.up, HEAD_DETECTION_RAY_LENGTH, groundLayer);

        return (!(headHit.collider == null));
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

    private void InitiateJump(int jumps){
        if (!IsJumping()){
            currentMovementState = movementState.JUMPING;
        }

        jumpBufferTimer = 0;
        numJumpsUsed += jumps;
        verticalVelocity = initialJumpVelocity;

        rigidbody.velocity = new Vector2(rigidbody.velocity.x, verticalVelocity);
    }

    private void JumpVariableCleanup(){
        fastFallTime = 0f;
        isPastApexThreshold = false;
        verticalVelocity = 0f;
        numJumpsUsed = 0;
    }

    public void MoveAction(InputAction.CallbackContext context){
        if (context.started || context.performed){
            moveInput = context.ReadValue<Vector2>();
        }else if (context.canceled){
            moveInput = Vector2.zero;
        }
    }

    public void JumpAction(InputAction.CallbackContext context){
        if (context.started || context.performed){
            jumpPressed = true;
            jumpReleased = false;
        }else if (context.canceled){
            jumpPressed = false;
            jumpReleased = true;
        }
    }


    void Update()
    {
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

        bool landed = (IsJumping() || IsFallingInTheAir()) && CheckForGround() && (verticalVelocity <= 0f);
        if (landed){
            currentMovementState = movementState.GROUND;
            JumpVariableCleanup();
        }
    }

    //order is going to matter a lot here
    //we're checking collision here rather than in any of the OnCollision() Unity methods
    void FixedUpdate()
    {
        Debug.Log("Num Jumps Used: " + numJumpsUsed);
        bool onGround = CheckForGround();

        //Vertical Velocity
        if (IsJumping()){
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
                verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.fixedDeltaTime;
            }else if (verticalVelocity < 0f){
                if (!IsFallingInTheAir()){
                    currentMovementState = movementState.FALLING;
                }
            }
        }

        if (IsFastFalling()){
            if (fastFallTime >= TIME_FOR_UP_CANCEL){
                verticalVelocity += gravity * GRAVITY_RELEASE_MULTIPLIER * Time.fixedDeltaTime;
            }else if (fastFallTime < TIME_FOR_UP_CANCEL){
                verticalVelocity = Mathf.Lerp(fastFallReleaseSpeed, 0f, (fastFallTime / TIME_FOR_UP_CANCEL));
            }

            fastFallTime += Time.fixedDeltaTime;
        }

        if (!CheckForGround() && !IsJumping()){
            if (!IsFallingInTheAir()){
                currentMovementState = movementState.FALLING;
            }

            verticalVelocity += gravity * Time.fixedDeltaTime;
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

            moveVelocity = Vector2.Lerp(moveVelocity, targetVelocity, currentAcceleration * Time.fixedDeltaTime);
            rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
        }else{      //decelerate
            moveVelocity = Vector2.Lerp(moveVelocity, Vector2.zero, currentDeceleration * Time.fixedDeltaTime);
            rigidbody.velocity = new Vector2(moveVelocity.x, rigidbody.velocity.y);
        }
    }
}