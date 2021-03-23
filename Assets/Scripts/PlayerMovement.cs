using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 7f;
    [SerializeField] float airSpeed = 4f;
    [SerializeField] float gravity = -9.8f;
    [SerializeField] float gravityScale = 1.0f;
    [SerializeField] float jumpHeight = 3f;
    [SerializeField] float dashSpeed = 100f;
    [SerializeField] float dashDuration = 0.01f;


    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] LayerMask groundMask;

    private Rigidbody rb;

    private Vector3 moveVector;
    private bool isGrounded;
    private bool isDashing;
    private bool hasDoubleJump = true;

    [SerializeField] float climbSpeed = 3f;
    private Vector3 obstaclePos;
    private float obstacleHeight;
    private Vector3 distanceFromObstacle;
    private Vector3 climbStartPos;
    private Vector3 climbEndPos;
    private bool isClimbing = false;
    private float climbProgress = 0f;
    private Vector3 climbDirection;
    private Vector3 jumpPos;

    public bool hasHitLedge {get; set;}

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if(!isDashing && isGrounded)
            SetMovementVector();
        Jump();
        if(!isGrounded)
        {
            DoubleJump();
            AirMovement();
        }
        else
        {
            hasDoubleJump = true;
        }
        Dash();
        CheckLedge();
        
        if(isDashing)
            CheckBounds();

    }


    void FixedUpdate()
    {
        Move();
        Climb();
        UpdateGravity();
    }

    private void SetMovementVector()
    {
        Vector3 inputs = Vector3.zero;
        inputs.x = Input.GetAxis("Horizontal");
        inputs.z = Input.GetAxis("Vertical");

        moveVector = transform.right * inputs.x + transform.forward * inputs.z;
        moveVector = Vector3.ClampMagnitude(moveVector, 1f);
    }

    private void Move()
    {
        rb.MovePosition(rb.position + moveVector * speed * Time.fixedDeltaTime);
    }

    private void Jump()
    {
         if(Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * gravity), ForceMode.VelocityChange);
            //rb.velocity = New Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
    }

    private void DoubleJump()
    {
        if(hasDoubleJump && Input.GetButtonDown("Jump"))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            Vector3 inputs = Vector3.zero;
            inputs.x = Input.GetAxisRaw("Horizontal");
            inputs.z = Input.GetAxisRaw("Vertical");

            if(inputs != Vector3.zero)
                SetMovementVector();

            rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * gravity), ForceMode.VelocityChange);
            hasDoubleJump = false;
        }
    }

    private void AirMovement()
    {
        Vector3 inputs = Vector3.zero;
        inputs.x = Input.GetAxis("Horizontal");
        inputs.z = Input.GetAxis("Vertical");

        Vector3 airMove = transform.right * inputs.x + transform.forward * inputs.z;
        moveVector += airMove * airSpeed * Time.deltaTime;
        moveVector = Vector3.ClampMagnitude(moveVector, 1f);
    }

    private void Dash()
    {
        if (Input.GetButtonDown("Dash"))
        {
            isDashing = true;
            rb.drag = 8f;
            rb.AddForce(moveVector.normalized * dashSpeed, ForceMode.VelocityChange);
            StartCoroutine(DashTimer());
        }
    }

    IEnumerator DashTimer()
    {
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        rb.drag = 1f;
        rb.velocity = Vector3.zero;
    }

    private void CheckLedge()
    {
        if(hasHitLedge)
        {
            if(!Physics.Raycast(transform.position + Vector3.up * 1f , climbDirection, 1f))
            {
                isClimbing = true;
            }
            hasHitLedge = false;
        }
        
    }

    private void CheckBounds()
    {
        if(Physics.Raycast(transform.position, moveVector, 8f, groundMask))
        {
            moveVector = Vector3.zero;
        }
    }

    private void Climb()
    {
        if(isClimbing)
        {
            rb.MovePosition(Vector3.Lerp(climbStartPos, jumpPos + climbDirection *1f, climbProgress));
            climbProgress += Time.fixedDeltaTime * climbSpeed;
            if(Vector3.Distance(transform.position, jumpPos + climbDirection *1f) < 0.3f)
            {
                isClimbing = false;
                climbProgress = 0f;
            }
        }
    }

    private void UpdateGravity()
    {
        rb.AddForce(Vector3.up * gravity * gravityScale, ForceMode.Acceleration);
    }

    public void SetObstacleProperties(Vector3 obstaclePos, float obstacleHeight)
    {
        this.obstaclePos = obstaclePos;
        this.obstacleHeight = obstacleHeight;

        jumpPos = new Vector3(transform.position.x, obstaclePos.y + obstacleHeight + 1f, transform.position.z);
        climbDirection = moveVector.normalized;

        Vector3 obstacleDirection = obstaclePos - transform.position;
        Vector3 obstacleHorizontalDirection = new Vector3(obstacleDirection.x, 0f, obstacleDirection.z);

        climbStartPos = transform.position;
        climbEndPos = new Vector3(obstaclePos.y, obstaclePos.y + obstacleHeight + 1f, obstaclePos.z);
        climbEndPos -= obstacleHorizontalDirection.normalized * 0.1f;
        
    }

    private void OnCollisionEnter(Collision other) {
        if(LayerMask.LayerToName(other.gameObject.layer) == "Ground")
        {
            hasHitLedge = true;
            SetObstacleProperties(other.transform.position, other.collider.bounds.extents.y);
        }
    }
}
