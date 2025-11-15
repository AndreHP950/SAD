using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;

    [Header("Mobile Controls")]
    public Joystick joystick;
    public Button jumpButton;

    [Header("Camera Controls")]
    public float sensitivityMobile = 5f;
    public float sensitivityPC = 50f;
    public CinemachineOrbitalFollow orbital = null;
    private int activeTouchId = -1;
    private Vector2 lastTouchPos;

    [SerializeField] private GraphicRaycaster raycaster;
    private PointerEventData pointerEventData;

    private bool jumpPressed;

    private void Start()
    {
        joystick = UIManager.instance.joystick;
        jumpButton = UIManager.instance.jumpButton;

        if (jumpButton) jumpButton.onClick.AddListener(() => jumpPressed = true);
        if (SceneManager.GetActiveScene().name == "Game") orbital = GameObject.Find("PlayerCamera").GetComponent<CinemachineOrbitalFollow>();

        raycaster = UIManager.instance.GetComponent<GraphicRaycaster>();
    }

    private HashSet<int> blockedFingerIds = new HashSet<int>();

    private void Update()
    {
        if (GameManager.instance.isMobile)
        {
            HandleTouchCamera();
        }
        else
        {
            HandleMouseCamera();
        }
            
    }

    void HandleMouseCamera()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
        {
            orbital.HorizontalAxis.Value += mouseX * sensitivityPC;
            orbital.VerticalAxis.Value += mouseY * sensitivityPC * -1;
        }
    }

    void HandleTouchCamera()
    {
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began)
            {
                if (TouchIsOnUI(touch.position))
                {
                    blockedFingerIds.Add(touch.fingerId);
                    continue;
                }
            }

            if (blockedFingerIds.Contains(touch.fingerId))
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    blockedFingerIds.Remove(touch.fingerId);

                continue;
            }

            if (touch.phase == TouchPhase.Began && activeTouchId == -1)
            {
                activeTouchId = touch.fingerId;
                lastTouchPos = touch.position;
            }
            else if (touch.fingerId == activeTouchId && touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition * sensitivityMobile;
                orbital.HorizontalAxis.Value += delta.x;
                orbital.VerticalAxis.Value += delta.y * -1;
                lastTouchPos = touch.position;
            }
            else if (touch.fingerId == activeTouchId &&
                    (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                activeTouchId = -1;
            }
        }
    }

    public void GetPlayerCamera()
    {
        orbital = GameObject.Find("PlayerCamera").GetComponent<CinemachineOrbitalFollow>();
    }

    public float GetHorizontal()
    {
        return GameManager.instance.isMobile ? joystick.Horizontal : Input.GetAxis("Horizontal");
    }

    public float GetVertical()
    {
        return GameManager.instance.isMobile ? joystick.Vertical : Input.GetAxis("Vertical");
    }

    public bool GetJump()
    {
        if (GameManager.instance.isMobile)
        {
            bool pressed = jumpPressed;
            jumpPressed = false;
            return pressed;
        }
        else
        {
            return Input.GetKeyDown(KeyCode.Space);
        }
    }
    private bool TouchIsOnUI(Vector2 screenPos)
    {
        pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = screenPos;

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, results);

        return results.Count > 0; // Se há UI, está sobre UI
    }
}
