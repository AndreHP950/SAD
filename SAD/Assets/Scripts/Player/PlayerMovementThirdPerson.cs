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

    [Header("References")]
    public Transform cameraTransform;
    public TextMeshProUGUI speedText;

    CharacterController characterController;
    private Vector3 velocity;
    private Vector3 moveVelocity;
    private Vector3 lastPos;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();

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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

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
        if (Input.GetKeyDown(KeyCode.Space) && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //Gravity
        if (characterController.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = (moveVelocity + velocity) * Time.deltaTime;
        characterController.Move(finalMove);

        //Character Rotation
        Vector3 flatVel = new Vector3(moveVelocity.x, 0f, moveVelocity.z);
        if(flatVel.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatVel);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation * Time.deltaTime);
        }
    }

    private void UpdateVelocity()
    {
        float speedValue = (transform.position - lastPos).magnitude / Time.deltaTime * 3.6f;

        speedText.text = ("Speed: " + (int)speedValue + " Km/h");

        lastPos = transform.position;
    }
}
