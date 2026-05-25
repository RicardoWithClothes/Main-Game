using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerStatsSO stats; 
    public Transform orientation;

    // Components
    [HideInInspector] public PlayerInputHandler input;
    [HideInInspector] public Rigidbody rb;

    // State Machine
    private PlayerBaseState currentState;

    // State Instances
    public PlayerState_Walk WalkState;
    public PlayerState_Sprint SprintState;
    public PlayerState_Crouch CrouchState;
    public PlayerState_Air AirState;

    // Variables shared across states
    [HideInInspector] public bool isGrounded;
    [HideInInspector] public bool readyToJump = true;
    [HideInInspector] public bool exitingSlope = false;

    private RaycastHit slopeHit;

    // Helper Properties
    public bool IsCrouching => currentState is PlayerState_Crouch;
    public float CurrentSpeed => rb.linearVelocity.magnitude;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInputHandler>();
        rb.freezeRotation = true;

        // Initialize States
        WalkState = new PlayerState_Walk(this);
        SprintState = new PlayerState_Sprint(this);
        CrouchState = new PlayerState_Crouch(this);
        AirState = new PlayerState_Air(this);
    }

    private void Start()
    {
        // Start in Walking state
        SwitchState(WalkState);
    }

    private void Update()
    {
        GroundCheck();
        currentState.Update();
        SpeedControl();
    }

    private void FixedUpdate()
    {
        currentState.FixedUpdate();
    }

    public void SwitchState(PlayerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    // --- PHYSICS HELPERS (Called by States) ---

    public void MovePlayer(float speed)
    {
        Vector3 moveDir = orientation.forward * input.Vertical + orientation.right * input.Horizontal;

        // Slope movement
        if (OnSlope() && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection(moveDir) * speed * 20f, ForceMode.Force);

            if (rb.linearVelocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        // Ground movement
        else if (isGrounded)
        {
            rb.AddForce(moveDir.normalized * speed * 10f, ForceMode.Force);
        }
        // Air movement
        else
        {
            rb.AddForce(moveDir.normalized * speed * 10f * stats.airMultiplier, ForceMode.Force);
        }

        rb.useGravity = !OnSlope();
    }

    public void DoJump()
    {
        exitingSlope = true;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * stats.jumpForce, ForceMode.Impulse);

        readyToJump = false;
        Invoke(nameof(ResetJump), stats.jumpCooldown);
    }

    public void ChangeScale(float yScale)
    {
        transform.localScale = new Vector3(transform.localScale.x, yScale, transform.localScale.z);
    }

    public void AddDownForce(float force)
    {
        rb.AddForce(Vector3.down * force, ForceMode.Impulse);
    }

    // --- INTERNAL CHECKS ---

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }

    private void GroundCheck()
    {
        // CHECK THIS, might have to change so that it is not jumping of everything. just ground. whatIsGround. removed the layer restriction so that i dont have to change other code for mesh children thing.
        isGrounded = Physics.Raycast(transform.position, Vector3.down, stats.playerHeight * 0.5f + 0.3f, Physics.DefaultRaycastLayers);

        // Handle Drag
        if (isGrounded) rb.linearDamping = stats.groundDrag;
        else rb.linearDamping = 0;
    }

    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, stats.playerHeight * 0.5f + 0.3f, stats.whatIsGround))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            return angle < stats.maxSlopeAngle && angle != 0;
        }
        return false;
    }

    public Vector3 GetSlopeMoveDirection(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void SpeedControl()
    {
        // Get current State's target speed (a bit of a hack, but keeps it simple)
        // Ideally, states should enforce their own limits, but this global limiter is fine for now.
        float currentTargetSpeed = stats.walkSpeed;
        if (currentState == SprintState) currentTargetSpeed = stats.sprintSpeed;
        if (currentState == CrouchState) currentTargetSpeed = stats.crouchSpeed;

        if (OnSlope() && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > currentTargetSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * currentTargetSpeed;
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (flatVel.magnitude > currentTargetSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * currentTargetSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }

    }

}