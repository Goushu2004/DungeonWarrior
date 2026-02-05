using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
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
    private MeshCollider meshCollider;
    private float lastAttackTime;
    private bool attackPermission;
    private bool isAttacking;
    public Transform rayStart;
    public Transform rayEnd;
    public float radius = 0.2f;
    public LayerMask enemyMask;
    private List<GameObject> hitTargets = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        meshCollider = Sword.GetComponent<MeshCollider>();
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
        //检测是否处于攻击动画
        switch (true) 
        {
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack1"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack2"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(1).IsName("Attack3"):
                meshCollider.enabled = true;
                rb.isKinematic = true;
                break;
            default:
                meshCollider.enabled = false;
                rb.isKinematic = false;
                break;
        }
        if (isAttacking) 
        {
            DetectCollision();
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
        if (rb.isKinematic )
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
    public interface IDamageable 
    {
        void TakeDamage(float amout);
    }
    private void ApplyDamage(GameObject target, Vector3 hitPoint) 
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null) 
        {
            damageable.TakeDamage(10f);
            Debug.Log($"Hit {target.name} at {hitPoint}");
        }
    }
}
