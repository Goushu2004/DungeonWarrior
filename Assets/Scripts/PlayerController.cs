using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour, IDamageable
{
    private Rigidbody rb;
    private float movingSpeed = 4f;
    float horizontal;
    float vertical;
    public Animator animator;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    public bool isGrounded;
    //攻击相关
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
    private AudioSource audioSource;
    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public AudioClip attackClip;
    public AudioClip hitClip;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        isAttacking = false;
        audioSource = GetComponent<AudioSource>();
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

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }
        //左右移动动画控制
        if (vertical == 0 && horizontal != 0) 
        {
            if (horizontal > 0)
            {
                animator.SetBool("is_run_right", true);
            }
            else if (horizontal < 0)
            {
                animator.SetBool("is_run_left", true);
            }
        }
        else
        {
            animator.SetBool("is_run_right", false);
            animator.SetBool("is_run_left", false);
        }

        //动画状态控制刚体
        switch (true)
        {
            case bool _ when animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(0).IsTag("TakeDamage"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump"):
            case bool _ when animator.GetCurrentAnimatorStateInfo(0).IsTag("Block"):
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
        //根据输入计算移动
        Vector3 velocity = transform.forward * vertical * movingSpeed + transform.right * horizontal * movingSpeed;
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
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
        PlayClip();
    }
    private void AttackStop()
    {
        isAttacking = false;
    }
    private void Jump()
    {
        animator.SetTrigger("jump_trigger");
    }
    private void OnAnimatorMove()
    {
        //在部分动画时，使用动画的位移和旋转
        if (rb.isKinematic)
        {
            Vector3 deltaPos = animator.deltaPosition * 5;
            Vector3 rayStart = transform.position + Vector3.up * 0.8f;
            int layerMask = ~LayerMask.GetMask("Player");
            if (Physics.Raycast(rayStart, transform.forward, out RaycastHit hit, deltaPos.magnitude + 0.2f, layerMask))
            {
                deltaPos = Vector3.zero;
            }
            else if (Physics.Raycast(rayStart, -transform.forward, out hit, deltaPos.magnitude + 0.2f, layerMask))
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
    void IDamageable.TakeDamage(float amount, Transform transform)
    {
        //开启平滑转向攻击者的携程
        StartCoroutine(SmoothLookAt(transform.position));
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
            damageable.TakeDamage(10f, transform);
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
        if (Physics.Raycast(stepCheakPoint, transform.forward, out hitLower, 0.5f))
        {
            //高射线检测
            RaycastHit hitupper;
            if (!Physics.Raycast(stepCheakPoint + new Vector3(0, stepHeight, 0), transform.forward, out hitupper, 1f))
            {
                rb.position += new Vector3(0, climbSmooth * Time.deltaTime, 0);
            }
        }
    }
    //平滑转向攻击者携程
    IEnumerator SmoothLookAt(Vector3 targetPosition)
    {
        float elapsed = 0f;
        float duration = 0.15f;
        Quaternion startRotation = transform.rotation;
        //计算水平方向
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            while (elapsed < duration)
            {
                //平滑过渡，Quaternion.Slerp函数在两个旋转之间进行球面线性插值，返回一个新的旋转，第三个参数控制插值的程度，0返回startRotation，1返回targetRotation
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
                //增量时间，逐渐增加elapsed的值，直到达到duration
                elapsed += Time.deltaTime;
                yield return null;
            }
            //确保最终旋转是目标旋转
            transform.rotation = targetRotation;
        }
    }
    public void footSoundsPlay() 
    {
        
    }
    public void PlayClip()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Move"))
        {
            audioSource.PlayOneShot(footstepClip, 0.7f);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Jump"))
        {
            audioSource.PlayOneShot(jumpClip,1);
        }
        
        else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            audioSource.PlayOneShot(attackClip);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("TakeDamage"))
        {
            audioSource.PlayOneShot(hitClip);
        }
    }
}
