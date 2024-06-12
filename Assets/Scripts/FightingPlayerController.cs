using Cinemachine;
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
    AudioSource m_voice;
    [SerializeField]
    AudioClip[] m_hitSounds;

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
    readonly int m_HashWin = Animator.StringToHash("Win");

    float m_health = 20;
    FightingStatus m_status;
    bool m_dead = false;
    bool m_hit = false;
    bool m_win = false;
    //if player is colliding with level bounds
    bool m_bounds = false;
    //if player is starting to go throw the enemy
    bool m_isGoingThrough = false;
    FightingSlenerAI m_enemy;

    readonly int m_upperHeadAttackCount = 4;
    readonly int m_attackCount = 2;
    readonly float m_speed = 3f;

    public FightingStatus PlayerStatus => m_status;
    float HitPoint => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.3f : 0.7f;

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
        m_enemy = GameObject.Find("Slender").GetComponent<FightingSlenerAI>();

        //m_input.LockInput();
        m_healthBar.maxValue = m_health;
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        m_anim.SetBool(m_HashDie, m_dead);

        if (!m_dead && m_status != FightingStatus.None)
        {
            m_hit = m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "BodyHit" || m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "HeadHit";
            // starts win animation and locks input if the enemy is dead
            if (m_enemy.Dead && !m_win)
            {
                m_win = true;
                m_anim.SetBool(m_HashWin, true);
                m_input.LockInput();
                StartCoroutine(UIController.Instance.Win());
                return;
            }

            m_anim.SetFloat(m_HashHorizontal, m_input.Move.x);
            m_anim.SetFloat(m_HashVertical, m_input.Move.y);

            if (m_input.UpperAttack && !m_hit)
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
            else if (m_input.LowerAttack && !m_hit)
            {
                m_anim.SetTrigger(m_HashLowerAttack);
                m_status = m_input.Move.y > 0.1 ? FightingStatus.MiddleAttack : FightingStatus.LowerAttack;
                m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount + 1));
            }

            if (m_input.MiddleBlock && !m_hit)
            {
                m_status = FightingStatus.MiddleBlock;
            }
            else if (m_input.UpperBlock && !m_hit)
            {
                m_status = FightingStatus.UpperBlock;
            }
            if (!m_hit)
            {
                m_status = FightingStatus.Idle;
            }
            m_anim.SetBool(m_HashUpperBlock, m_input.UpperBlock);
            m_anim.SetBool(m_HashMiddleBlock, m_input.MiddleBlock);
        }
    }

    public void StartGame()
    {
        m_status = FightingStatus.Idle;
        m_input.LockInput();
    }

    private void OnAnimatorMove()
    {
        m_isGoingThrough = m_input.Move.x > 0 && Vector3.Distance(transform.position, m_enemy.transform.position) < 0.7f;
        m_rb.MovePosition(m_rb.position + (m_bounds || m_isGoingThrough ? 0 : 1) * m_anim.deltaPosition.magnitude * m_input.Move.x * m_speed * transform.forward);
    }

    void Hit(int hitPart)
    {
        m_health -= HitPoint;
        m_healthBar.value = m_health; ;
        if (m_health <= 0&&!m_win)
        {
            m_dead = true;
            m_healthBar.value = 0;
            m_input.Die();
            m_status = FightingStatus.Die;
        }
        else if(m_health > 0)
        {
            m_voice.PlayOneShot(m_hitSounds[Random.Range(0, m_hitSounds.Length)]);
            if (!m_bounds)
                m_rb.MovePosition(m_rb.position - transform.forward * 0.1f);
            m_anim.SetInteger(m_HashHitTarget, hitPart);
            m_anim.SetTrigger(m_HashHit);
            m_status = FightingStatus.Hit;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if player collides with enemy's hands and foots
        if (!m_dead && other.gameObject.CompareTag("attack"))
        {
            //collide point to detect hit part
            Vector3 point = other.ClosestPoint(transform.position);
            //divide playter collider into 3 parts and check in which part collide point is
            float part = m_col.bounds.size.y / 3f;
            // if collide part has block, do nothing
            if (m_col.bounds.max.y - part <= point.y && !m_input.UpperBlock)
            {
               // Debug.Log("Player Upper hit");
                Hit(1);
            }
            if (m_col.bounds.max.y - part > point.y && !m_input.MiddleBlock)
            {
               // Debug.Log("Player Middle hit");
                Hit(2);
            }
        }
        // if player collides with level bound and tries to go through it, srop moving
        else if (!m_dead && other.gameObject.CompareTag("bound"))
        {
            m_bounds = Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) < 0.7f && m_input.Move.x < 0
       || Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) > 0.7f && m_input.Move.x > 0;
        }
    }
    /// <summary>
    /// Reset player's values, when the player is dead
    /// </summary>
    public void Reset()
    {
        m_status = FightingStatus.None;
        m_dead = false;
        transform.SetPositionAndRotation(m_startPosition.position, m_startPosition.rotation);
        m_rb.position = m_startPosition.position;
        m_rb.rotation = m_startPosition.rotation;
        m_health = m_healthBar.maxValue;
        m_healthBar.value = m_health;
        m_anim.Rebind();
        m_anim.Update(0f);

    }
}
