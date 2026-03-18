using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class BoneController : MonoBehaviour,  IDamageable
{
    private int health = 30;
    public Animator animator;
    private Rigidbody rb;
    private float speed = 2f;
    public GameObject groundCheckPoint;
    private float groundCheckDistance = 0.1f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool startAINaVi = false;
    private GameObject target;
    public NavMeshAgent agent;
    public GameObject stepCheak;
    public Transform rayStart;
    public Transform rayEnd;
    public float radius = 0.2f;
   private LayerMask targetMask;
    private List<GameObject> hitTargets = new List<GameObject>();
    private bool startDetectCollision = false;
    public int attackSymbol = 1;
    private string attackTriggerName = "";
    private enum BoneState
    {
        Chasing,
        Attack,
        Dead
    }
    private BoneState currentState = BoneState.Chasing;
    private float lastAttackTime = 0f;
    private float attackCoolDown = 2f;
    private GameObject player;
    private Animator playerAnimator;
    private AudioSource playerAudioSource;
    private AudioSource enemytAudioSource;
    public AudioClip blockClip;
    public AudioClip slashClip;
    public AudioClip deathClip;
    public AudioClip screamClip;
    // startis called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(Scream());
        target = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
        StartCoroutine(BoneStateRuntine());
        targetMask = LayerMask.GetMask("Player","Shield");
        player = GameObject.FindGameObjectWithTag("Player");
        playerAnimator = player.GetComponent<Animator>();
        playerAudioSource = player.GetComponent<AudioSource>();
        enemytAudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.CheckSphere(groundCheckPoint.transform.position, groundCheckDistance, groundMask))
        {
            isGrounded = true; 
        }
               
        if (startDetectCollision) 
        {
            DetectCollision();
        }
        StepClimb();
        //攻击符号重置
        if (attackSymbol > 2||Time.time - lastAttackTime > 4) 
        {
            attackSymbol = 1;
        }
        //更新攻击触发器名称
        attackTriggerName = "attack_trigger" + attackSymbol;
    }
    void FixedUpdate() 
    {
        //默认进场
        if (isGrounded&&startAINaVi == false) 
        {
            Vector3 rbVelocity = transform.forward * speed;
            rb.linearVelocity = new Vector3(rbVelocity.x, rb.linearVelocity.y, rbVelocity.z);
            animator.SetFloat("speed", rbVelocity.z);
        }
    }
    void Dead()
    {
        animator.SetTrigger("death_trigger");
        StartCoroutine(DestroyAfterDeath());
    }
    IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
    void IDamageable.TakeDamage(float amount,Transform transform)
    {
        StartCoroutine(SmoothLookAt(transform.position));
        animator.SetTrigger("damage_trigger");
        health -= (int)amount;
        if (health <= 0)
        {
            Dead();
            currentState = BoneState.Dead;
        }
    }

    IEnumerator Scream()
    {
        yield return new WaitForSeconds(1.5f);
        animator.SetTrigger("scream_trigger");
        animator.SetFloat("speed", 0);
        startAINaVi = true;
        StartCoroutine(BoneNavi());
    }
    IEnumerator BoneNavi() 
    {
        yield return new WaitForSeconds(1f);
        agent.enabled = true;
    }
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
    IEnumerator BoneStateRuntine() 
    {
        while (true) 
        {
            float distanceToPlayer = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToPlayer <= 1.5f)
            {
                currentState = BoneState.Attack;
            }
            else
            {
                currentState = BoneState.Chasing;
            }
            BoneStateSwitch();
            yield return new WaitForSeconds(0.2f);
        }
    }
    //状态机
    void BoneStateSwitch() 
    {
        if (agent.enabled)
        {
            switch (currentState) 
            {
                case BoneState.Chasing:
                    agent.SetDestination(target.transform.position);
                    animator.SetFloat("speed", agent.velocity.magnitude);
                    break;
                case BoneState.Attack:
                    if (Time.time > lastAttackTime + attackCoolDown) 
                    {
                        lastAttackTime = Time.time;
                        animator.SetFloat("speed", 0);
                        hitTargets.Clear(); 
                        animator.SetTrigger(attackTriggerName);
                        
                    }
                    break;
                case BoneState.Dead:
                    agent.enabled = false;
                    animator.SetFloat("speed", 0);
                    break;
            }
            
        }
    }
    private void DetectCollision()
    {
        //射线检测敌人
        Vector3 direction = rayEnd.position - rayStart.position;
        float distance = direction.magnitude;
        RaycastHit[] hits = Physics.SphereCastAll(rayStart.position, radius, direction.normalized, distance, targetMask);
        foreach (RaycastHit hit in hits)
        {
            GameObject target = hit.collider.gameObject;
            if (!hitTargets.Contains(target))
            {
                Vector3 attackDirection = (transform.position - player.transform.position).normalized;
                float dot = Vector3.Dot(player.transform.forward, attackDirection);
                if (playerAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Block") && target.gameObject.layer == LayerMask.NameToLayer("Shield") && dot > 0.5f)
                {
                    playerAnimator.SetTrigger("block_hit_trigger");
                    playerAudioSource.PlayOneShot(blockClip);
                    hitTargets.Add(target);
                    startDetectCollision = false;
                    break;
                }
                else if (target.gameObject.layer == LayerMask.NameToLayer("Player"))
                {               
                    ApplyDamage(target, hit.point);
                    hitTargets.Add(target);
                    startDetectCollision = false;
                    break;
                }
            }
        }
    }
    private void ApplyDamage(GameObject target, Vector3 hitPoint)
    {
        var damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(10f,transform);
        }
    }
    public void StartDetectCollision() 
    {
        startDetectCollision = true;
    }
    public void StopDetectCollision() 
    {
        startDetectCollision = false;
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
    public void PlayDeathSound() 
    {
        enemytAudioSource.PlayOneShot(deathClip);
    }
    public void PlayClip() 
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("BoneScream")) 
        {
            enemytAudioSource.PlayOneShot(screamClip,0.5f);
        }
        else if (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) 
        {
            enemytAudioSource.PlayOneShot(slashClip,0.3f);
        }
    }
    public void StopClip() 
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("BoneScream"))
        {
            enemytAudioSource.Stop();
        }
    }
}
//