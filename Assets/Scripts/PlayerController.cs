using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour,IDamageable
{
    private Rigidbody rb;
    private float movingSpeed = 4f;
    float horizontal;
    float vertical;
    private float turnSpeed = 100f;
    public Animator animator;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public bool isGrounded;
    //攻击相关
    public GameObject Sword;
    private float lastAttackTime;
    private bool attackPermission;
    private bool isAttacking;
    public Transform rayStart;
    public Transform rayEnd;
    public float radius = 0.2f;
    public LayerMask enemyMask;
    private List<GameObject> hitTargets = new List<GameObject>();
    public GameObject stepCheak;
    private float hp = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        isAttacking = false;   
    }
    //获取键盘wasd输入
    private void OnMove(InputValue value)
    {
        Vector2 movement = value.Get<Vector2>();
        horizontal = movement.x;
        vertical = movement.y;
    }    
    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Speed", vertical * movingSpeed);
        //判断是否着地
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        animator.SetBool("is_grounded", isGrounded);
        //攻击冷却
        if (Time.time - lastAttackTime > 0.5f)
        {
            attackPermission = true;
        }
        else 
        {
            attackPermission = false;
        }
        if (Input.GetButtonDown("Fire1") && attackPermission)
        {
            Attack();
        }
       
        if (Input.GetButtonDown("Jump")&&isGrounded) 
        {
            Jump();
        }
        if (vertical == 0 && horizontal != 0)
        {
            Turn();
        }
        else 
        {
            animator.SetBool("turn_right", false);
            animator.SetBool("turn_left", false);
        }
        //动画状态控制刚体
        switch (true) 
        {
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack1"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack2"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack3"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(0).IsName("PlayerTakeDamage"):
                rb.isKinematic = true;
                break;
            default:
                rb.isKinematic = false;
                break;
        }
        if (isAttacking) 
        {
            DetectCollision();
        }
        if (Input.GetButtonDown("Fire2"))
        {
            animator.SetBool("is_block", true);
        }
        else if (Input.GetButtonUp("Fire2"))
        {
            animator.SetBool("is_block", false);
        }
    }
    private void FixedUpdate()
    {
        //处于攻击动画时禁止移动
        if (rb.isKinematic) 
        {
            return;
        }
        //根据输入计算移动和旋转
        Vector3 velocity = transform.forward * movingSpeed * vertical;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
        float turn = horizontal * turnSpeed * Time.fixedDeltaTime;
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
        rb.MoveRotation(rb.rotation * turnRotation);
        //上台阶
        StepClimb();
    }
    private void Attack()
    {   
        animator.SetTrigger("attack_trigger");
        lastAttackTime = Time.time;
        hitTargets.Clear();
    }
    private void AttackStart() 
    {
        isAttacking = true;
    }
    private void AttackStop() 
    {
        isAttacking = false;
    }
    private void Jump() 
    {
        animator.SetTrigger("jump_trigger");
        rb.AddForce(Vector3.up * 7f, ForceMode.Impulse);
    }
    private void Turn() 
    {
        if (horizontal > 0) 
        {
            animator.SetBool("turn_right", true);
        }
        else if (horizontal < 0) 
        {
            animator.SetBool("turn_left", true);
        }
    }
    private void OnAnimatorMove()
    {
        //在攻击动画时，使用动画的位移和旋转
        if (rb.isKinematic)
        {
            Vector3 deltaPos = animator.deltaPosition;
            Vector3 rayStart = transform.position + Vector3.up * 0.8f;
            int layerMask = ~LayerMask.GetMask("Player");
            if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, deltaPos.magnitude + 0.2f, layerMask))
            {
                deltaPos = Vector3.zero;
            }
            rb.MovePosition(rb.position + deltaPos);
            rb.MoveRotation(rb.rotation * animator.deltaRotation);
        }
    }
    private void DetectCollision() 
    {
        //射线检测敌人
        Vector3 direction = rayEnd.position - rayStart.position;
        float distance = direction.magnitude;
        RaycastHit[] hits = Physics.SphereCastAll(rayStart.position, radius, direction.normalized, distance, enemyMask);
        foreach (RaycastHit hit in hits) 
        {
            GameObject target = hit.collider.gameObject;
            if (!hitTargets.Contains(target)) 
            {
                hitTargets.Add(target);
                ApplyDamage(target, hit.point);
            }
        }
    }
    void IDamageable.TakeDamage(float amount)
    {
        animator.SetTrigger("damage_trigger");
        hp -= amount;
        if (hp <= 0) 
        {

        }
    }
    private void ApplyDamage(GameObject target, Vector3 hitPoint) 
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null) 
        {
            damageable.TakeDamage(10f);
        }
    }
    //爬台阶
    void StepClimb()
    {
        Vector3 stepCheakPoint = stepCheak.transform.position;
        float stepHeight = 0.5f;
        float climbSmooth = 5f;
        //低射线检测
        RaycastHit hitLower;
        if (Physics.Raycast(stepCheakPoint, transform.forward, out hitLower, 0.1f))
        {
            //高射线检测
            RaycastHit hitupper;
            if (!Physics.Raycast(stepCheakPoint + new Vector3(0, stepHeight, 0), transform.forward, out hitupper, 0.5f))
            {
                rb.position += new Vector3(0, climbSmooth * Time.deltaTime, 0);
            }
        }
    }
}
