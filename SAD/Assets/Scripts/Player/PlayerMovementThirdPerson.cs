using TMPro;
using UnityEngine;

public class PlayerMovementThirdPerson : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float acceleration = 10f;
    public float deceleration = 8f;

    [Header("Rotation")]
    public float rotation = 10f;

    [Header("Y Movement")]
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Slope Sliding")]
    public float slideSpeed = 2f;
    public float slopeRayLength = 1.5f;
    private bool isSliding;

    [Header("References")]
    public Transform cameraTransform;
    public TextMeshProUGUI speedText;
     
    CharacterController characterController;
    PlayerInputManager playerInputManager;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Vector3 lastPos;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInputManager = GetComponent<PlayerInputManager>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        lastPos = transform.position;
    }

    private void Update()
    {
        Movement();
        UpdateVelocity();
    }

    private void Movement()
    {
        float horizontal = playerInputManager.GetHorizontal();
        float vertical = playerInputManager.GetVertical();

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = (camForward * vertical + camRight * horizontal).normalized;
        Vector3 targetVelocity = inputDir * speed;

        //Movement based on camera position
        float lerpSpeed = (inputDir.magnitude > 0.1f) ? acceleration : deceleration;
        moveVelocity = Vector3.Lerp(moveVelocity, targetVelocity, lerpSpeed * Time.deltaTime);

        //Jump
        if (playerInputManager.GetJump() && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //Gravity
        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        //Character Rotation
        Vector3 flatVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        if(flatVel.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatVel);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation * Time.deltaTime);
        }

        Vector3 moveDir = transform.forward * flatVel.magnitude;

        Vector3 slideDir;
        isSliding = CheckSlope(out slideDir);

        if (isSliding)
        {
            moveDir += slideDir * slideSpeed;
        }

        Vector3 finalMove = (moveDir + velocity) * Time.deltaTime;
        characterController.Move(finalMove);
    }

    private void UpdateVelocity()
    {
        float speedValue = (transform.position - lastPos).magnitude / Time.deltaTime * 3.6f;

        speedText.text = ("Speed: " + (int)speedValue + " Km/h");

        lastPos = transform.position;
    }

    private bool CheckSlope(out Vector3 slideDirection)
    {
        slideDirection = Vector3.zero;
        RaycastHit hit;

        if(Physics.Raycast(transform.position, Vector3.down, out hit, slopeRayLength))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

            if (slopeAngle > characterController.slopeLimit)
            {
                slideDirection = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
                return true;
            }
        }

        return false;
    }
}
