using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    public LayerMask targetMask;
    private List<GameObject> hitTargets = new List<GameObject>();
    private bool startDetectCollision = false;
    private enum BoneState
    {
        Chasing,
        Attack,
        Dead
    }
    private BoneState currentState = BoneState.Chasing;
    private float lastAttackTime = 0f;
    private float attackCoolDown = 2f;
    // start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        StartCoroutine(Scream());
        target = GameObject.FindGameObjectWithTag("Player");
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
        StartCoroutine(BoneStateRuntine());
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
    void IDamageable.TakeDamage(float amount)
    {
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
                        animator.SetTrigger("attack_trigger");
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
                hitTargets.Add(target);
                ApplyDamage(target, hit.point);
            }
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
    public void StartDetectCollision() 
    {
        startDetectCollision = true;
    }
    public void StopDetectCollision() 
    {
        startDetectCollision = false;
    }
}
