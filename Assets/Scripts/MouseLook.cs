using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    public InputActionAsset actions;
    private InputAction lookAction;
    public float sensitivity = 10f;
    public Transform playerBody;
    float xRotation = 0f;
    public GameObject player;
    private Animator animator;
    private void Awake()
    {
        lookAction = actions.FindActionMap("Player").FindAction("Look");
    }
    void OnEnable() => lookAction.Enable();
    void OnDisable() => lookAction.Disable();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 隐藏鼠标指针并锁定在屏幕中心
        Cursor.lockState = CursorLockMode.Locked;
        animator = player.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {   //获取鼠标输入
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float mouseX = lookInput.x * sensitivity * Time.deltaTime;
        float mouseY = lookInput.y * sensitivity * Time.deltaTime;
        if (mouseX != 0)
        {
            if (mouseX > 0.1)
            {
                animator.SetBool("is_turn_right", true);
            }
            else if(mouseX < -0.1) 
            {
                animator.SetBool("is_turn_left", true);
            }
        }
        else 
        {
            animator.SetBool("is_turn_right", false);
            animator.SetBool("is_turn_left", false);
        }
        //反向旋转摄像机
        xRotation -= mouseY;
        //限制摄像机旋转范围
        xRotation = Mathf.Clamp(xRotation, -10f, 20f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
