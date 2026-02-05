using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private int health = 30;
    public Animator animator;
    private Rigidbody rb;
    public LayerMask LayerMask;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            Dead();
        }
    }
    void Dead()
    {
        Destroy(gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PlayerWeapon"))
        {
            GetDamage();
        }
    }
    private void GetDamage() 
    {
        animator.SetTrigger("damage_trigger");
        health -= 10;
        if (health <= 0) 
        {
            Dead();
        }
    }
    public interface IDamageable
    {
        void TakeDamage(float amout);
    }
}
