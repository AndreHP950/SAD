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
    public float sensitivity = 0.15f;
    public CinemachineOrbitalFollow orbital = null;
    private int activeTouchId = -1;
    private Vector2 lastTouchPos;

    private bool jumpPressed;

    private void Awake()
    {
        joystick = GameObject.Find("UIManager/GameUI/MobileUI/Joystick").GetComponent<Joystick>();
        jumpButton = GameObject.Find("UIManager/GameUI/MobileUI/Jump").GetComponent<Button>();

        if (jumpButton) jumpButton.onClick.AddListener(() => jumpPressed = true);
        if (SceneManager.GetActiveScene().name == "Game") orbital = GameObject.Find("PlayerCamera").GetComponent<CinemachineOrbitalFollow>();
    }

    private void Update()
    {
        if (!GameManager.instance.isMobile) return;

        foreach (Touch touch in Input.touches)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) continue;

            if (touch.phase == TouchPhase.Began && activeTouchId == -1)
            {
                activeTouchId = touch.fingerId;
                lastTouchPos = touch.position;
            }
            else if (touch.fingerId == activeTouchId && touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition * sensitivity;
                orbital.HorizontalAxis.Value += delta.x;
                orbital.VerticalAxis.Value += delta.y;
                lastTouchPos = touch.position;
            }
            else if (touch.fingerId == activeTouchId && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
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
}
