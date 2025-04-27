using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAdvanced : MonoBehaviour
{

    public bool IsCrouching { get; private set; }
    public bool IsFalling { get; private set; }


    [Header("Movement")]
    public float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float groundDrag;

    public float jumpForce;
    public float jumpButtonGracePeriod;
    
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl; 

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    public bool grounded;


    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;
    private bool forceCrouch;


    //For jump buffer + coyote time
    private float? lastGroundTime;
    private float? jumpButtonPressedTime;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        startYScale = transform.localScale.y;
        readyToJump = true;
    }

    private void Update()
    {
        IsCrouching = state == MovementState.crouching;
        IsFalling = state == MovementState.air;
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();

        // handle drag
        if (grounded){
            rb.drag = groundDrag;
            lastGroundTime = Time.time;
        }
        else
            rb.drag = 0;

        if (Input.GetButtonDown("Jump"))
        {
            jumpButtonPressedTime = Time.time;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if (Input.GetKey(jumpKey) && readyToJump &&  grounded)
        {
            readyToJump = false;

            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

        if (Input.GetKeyDown(crouchKey))
        {
            Crouch();
        }

        // try to stop crouching
        if (Input.GetKeyUp(crouchKey))
        {
            // Cast a ray upwards to check for ceiling
            float ceilingCheckDistance = (startYScale - crouchYScale) + 0.2f; // small buffer
            Vector3 raycastOrigin = transform.position + Vector3.up * (crouchYScale * 0.5f);
            bool ceilingBlocked = Physics.Raycast(raycastOrigin, Vector3.up, ceilingCheckDistance, whatIsGround);

            if (!ceilingBlocked)
            {
                Uncrouch();
            }
            else
            {
                forceCrouch = true; // Still stuck under ceiling
            }
        }
    }
    private void CheckCeiling()
    {
        if (!forceCrouch)
            return;

        float ceilingCheckDistance = (startYScale - crouchYScale) + 0.2f;
        Vector3 raycastOrigin = transform.position + Vector3.up * (crouchYScale * 0.5f);
        bool ceilingBlocked = Physics.Raycast(raycastOrigin, Vector3.up, ceilingCheckDistance, whatIsGround);

        if (!ceilingBlocked)
        {
            // No longer blocked, we can stand up
            Uncrouch();
        }
    }
    private void Crouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
    }

    private void Uncrouch()
    {
        transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        forceCrouch = false;
    }
    private void StateHandler()
    {
        CheckCeiling();
        // MODE - crouching
        if ((Input.GetKey(crouchKey) || forceCrouch) && grounded)
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        
        // MODE- sprinting
        else if(grounded && Input.GetKey(sprintKey)){
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        // MODE - walking
        else if(grounded){
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        // MODE - air
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        //on slope
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            if (rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        // on ground
        if (grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        // in air
        else if (!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);

        //turn off gravity on slope
        rb.useGravity = !OnSlope();

    }

    private void SpeedControl()
    {
        //limiting speed on slope
        if (OnSlope() && !exitingSlope)
        {
            if (rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f, whatIsGround))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            return angle < maxSlopeAngle && angle !=0;

        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }
}   