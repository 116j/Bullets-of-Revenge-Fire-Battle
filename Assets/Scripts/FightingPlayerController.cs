using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class FightingPlayerController : MonoBehaviour
{
    [SerializeField]
    AnimatorController fightingController;
    [SerializeField]
    Slider healthBar;

    PlayerInput m_input;
    Animator m_anim;
    Rigidbody m_rb;
    CapsuleCollider m_col;

    //hashes for animator parameters
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");
    readonly int m_HashUpperAttack = Animator.StringToHash("UpperAttack");
    readonly int m_HashLowerAttack = Animator.StringToHash("LowerAttack");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashHitTarget = Animator.StringToHash("HitTarget");
    readonly int m_HashUpperBlock = Animator.StringToHash("UpperBlock");
    readonly int m_HashMiddleBlock = Animator.StringToHash("MiddleBlock");
    readonly int m_HashLowerBlock = Animator.StringToHash("LowerBlock");
    readonly int m_HashRandom = Animator.StringToHash("Random");
    readonly int m_HashDie = Animator.StringToHash("Die");

    float m_health;

    readonly int m_upperHeadAttackCount = 4;
    readonly int m_attackCount = 2;
    readonly float m_maxHealth;
    readonly float m_hit;

    // Start is called before the first frame update
    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();
        m_col = GetComponent<CapsuleCollider>();

        m_anim.runtimeAnimatorController = fightingController;
        m_input.ChangeGanre();
    }

    // Update is called once per frame
    void Update()
    {
        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x);
        m_anim.SetFloat(m_HashVertical, m_input.Move.y);

        if (m_input.UpperAttack)
        {
            m_anim.SetTrigger(m_HashUpperAttack);
            if (m_input.Move.y > 0.1)
            {
                m_anim.SetInteger(m_HashRandom, Random.Range(1, m_upperHeadAttackCount + 1));
            }
            else
            {
                m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount + 1));
            }
        }
        else if (m_input.LowerAttack)
        {
            m_anim.SetTrigger(m_HashLowerAttack);
            m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount));

        }
        m_anim.SetBool(m_HashLowerBlock, m_input.LowerBlock);
        m_anim.SetBool(m_HashMiddleBlock, m_input.MiddleBlock);
        m_anim.SetBool(m_HashUpperBlock, m_input.UpperBlock);
    }

    private void OnAnimatorMove()
    {
        m_rb.MovePosition(m_rb.position + m_anim.deltaPosition.magnitude * m_input.Move.x * transform.forward);
    }

    void Hit(int hitPart)
    {
        m_anim.SetInteger(m_HashHitTarget, hitPart);
        m_anim.SetTrigger(m_HashHit);
        m_health -= m_hit;
        if (m_health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        m_anim.SetTrigger(m_HashDie);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("attack"))
        {
            Vector3 point = collision.GetContact(0).point;
            float part = m_col.bounds.size.y / 3f;
            if (m_col.bounds.min.y + part >= point.y && !m_input.LowerBlock)
            {
                Hit(3);
            }
            else if (m_col.bounds.max.y - part <= point.y && !m_input.UpperBlock)
            {
                Hit(1);
            }
            else if (!m_input.MiddleBlock)
            {
                Hit(2);
            }
        }
    }
}
