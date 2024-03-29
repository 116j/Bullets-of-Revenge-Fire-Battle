using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public class FightingPlayerController : MonoBehaviour
{
    [SerializeField]
    AnimatorController m_fightingController;
    [SerializeField]
    Transform m_startPosition;
    [SerializeField]
    Slider m_healthBar;
    [SerializeField]
    Text m_startText;

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
    readonly int m_HashRandom = Animator.StringToHash("Random");
    readonly int m_HashDie = Animator.StringToHash("Die");

    float m_health = 10;
    FightingStatus m_status;
    bool m_dead = false;
    bool m_bounds = false;
    Color m_fadeColor;

    readonly int m_upperHeadAttackCount = 4;
    readonly int m_attackCount = 2;
    readonly float m_speed = 3f;
    readonly float m_fadeTime = 3f;

    public FightingStatus PlayerStatus => m_status;
    float HitPoint => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.4f : 8f;

    // Start is called before the first frame update
    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();
        m_col = GetComponent<CapsuleCollider>();
        m_col.center = new Vector3(0f, m_col.center.y + 0.03f, 0.05f);
        m_col.radius += 0.06f;
        m_col.height += 0.1f;
        m_col.isTrigger = true;
        m_rb.isKinematic = true;
        m_rb.detectCollisions = true;
        m_anim.runtimeAnimatorController = m_fightingController;
        m_input.ChangeGanre();

        m_healthBar.maxValue = m_health;
        m_healthBar.value = m_health;
        m_startText.gameObject.SetActive(true);
        m_fadeColor = m_startText.color;
        m_fadeColor.a = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        m_startText.color = Color.Lerp(m_startText.color, m_fadeColor, Time.deltaTime * m_fadeTime);
        if (m_startText.isActiveAndEnabled&&m_startText.color.a<0.01f)
        {
            m_startText.gameObject.SetActive(false);
            m_status = FightingStatus.Idle;
            m_input.LockInput();
        }
        m_anim.SetBool(m_HashDie, m_dead);
        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x);
        m_anim.SetFloat(m_HashVertical, m_input.Move.y);

        if (m_input.UpperAttack)
        {
            m_anim.SetTrigger(m_HashUpperAttack);
            if (m_input.Move.y > 0.1)
            {
                m_status = FightingStatus.MiddleAttack;
                m_anim.SetInteger(m_HashRandom, Random.Range(1, m_upperHeadAttackCount + 1));
            }
            else
            {
                m_status = FightingStatus.LowerAttack;
                m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount + 1));
            }
        }
        else if (m_input.LowerAttack)
        {
            m_anim.SetTrigger(m_HashLowerAttack);
            m_status = m_input.Move.y > 0.1 ? FightingStatus.MiddleAttack : FightingStatus.LowerAttack;
            m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount + 1));
        }

        if (m_input.MiddleBlock)
        {
            m_status = FightingStatus.MiddleBlock;
        }
        else if (m_input.UpperBlock)
        {
            m_status = FightingStatus.UpperBlock;
        }
        m_anim.SetBool(m_HashUpperBlock, m_input.UpperBlock);
        m_anim.SetBool(m_HashMiddleBlock, m_input.MiddleBlock);

    }

    private void OnAnimatorMove()
    {
        m_rb.MovePosition(m_rb.position + m_anim.deltaPosition.magnitude * transform.forward * m_speed * m_input.Move.x * (m_bounds ? 0 : 1));
    }

    void Hit(int hitPart)
    {
        m_health -= HitPoint;
        m_healthBar.value = m_health; ;
        if (m_health <= 0)
        {
            m_dead = true;
            m_healthBar.value = 0;
            m_input.Die();
            m_status = FightingStatus.Die;
        }
        else
        {
            m_rb.MovePosition(m_rb.position - transform.forward * 0.1f);
            m_anim.SetInteger(m_HashHitTarget, hitPart);
            m_anim.SetTrigger(m_HashHit);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_dead && other.gameObject.CompareTag("attack"))
        {
            Vector3 point = other.ClosestPoint(transform.position);
            float part = m_col.bounds.size.y / 3f;
            if (m_col.bounds.max.y - part <= point.y && !m_input.UpperBlock)
            {
                Debug.Log("Player Upper hit");
                Hit(1);
            }
            if (m_col.bounds.max.y - part > point.y && !m_input.MiddleBlock)
            {
                Debug.Log("Player Middle hit");
                Hit(2);
            }
        }
        else if (!m_dead && other.gameObject.CompareTag("bound"))
        {
            Debug.Log(Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized));
            m_bounds = Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) < 0.7f && m_input.Move.x < 0
       || Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) > 0.7f && m_input.Move.x > 0;
        }
    }

    public void Restart()
    {
        m_dead = false;
        transform.SetPositionAndRotation(m_startPosition.position, m_startPosition.rotation);
        m_health = m_healthBar.maxValue;
        m_healthBar.value = m_health;
        m_anim.Rebind();
        m_anim.Update(0f);
        m_startText.gameObject.SetActive(true);
        Color textColor = m_startText.color;
        textColor.a = 1;
        m_startText.color = textColor;
    }
}
