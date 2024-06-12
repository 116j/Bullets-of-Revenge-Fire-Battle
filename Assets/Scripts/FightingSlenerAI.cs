using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FightingSlenerAI : MonoBehaviour
{
    [SerializeField]
    Transform m_startPosition;
    [SerializeField]
    Slider m_healthBar;
    [SerializeField]
    AudioClip[] m_hitSounds;

    Collider m_col;
    Animator m_anim;
    Rigidbody m_rb;
    FightingPlayerController m_player;
    AudioSource m_audio;

    float m_health = 20;
    //move direction
    int m_move = 0;
    bool m_bounds = false;
    //move away timer
    float m_runAway = 0;
    bool m_middleBlock = false;
    bool m_lowerBlock = false;
    bool m_dead = false;

    readonly float m_speed = 0.8f;
    //max attack and block dist
    readonly float m_lowerAttackDist = 2.25f;
    readonly float m_upperAttack1Dist = 1.34f;
    readonly float m_upperAttack2Dist = 1.23f;
    readonly float m_middleBlockDist = 1.6f;
    readonly float m_lowerBlockDist = 1.72f;
    readonly float m_runawayDist = 0.85f;
    readonly int m_attackCount = 2;

    //hashes for animator parameters
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");
    readonly int m_HashUpperAttack = Animator.StringToHash("UpperAttack");
    readonly int m_HashLowerAttack = Animator.StringToHash("LowerAttack");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashHitTarget = Animator.StringToHash("HitTarget");
    readonly int m_HashMiddleBlock = Animator.StringToHash("MiddleBlock");
    readonly int m_HashLowerBlock = Animator.StringToHash("LowerBlock");
    readonly int m_HashRandom = Animator.StringToHash("Random");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashWin = Animator.StringToHash("Win");

    float HitPoint => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.7f : 0.3f;
    //enemy block reaction
    float Reaction => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 3f : 1.5f;
    public bool Dead => m_dead;

    // Start is called before the first frame update
    void Start()
    {
        m_col = GetComponent<Collider>();
        m_anim = GetComponent<Animator>();
        m_rb = GetComponent<Rigidbody>();
        m_audio = GetComponent<AudioSource>();
        m_rb.detectCollisions = true;
        m_player = GameObject.FindWithTag("Player").GetComponent<FightingPlayerController>();

        m_healthBar.maxValue = m_health;
        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        m_anim.SetBool(m_HashDie, m_dead);

        if (!m_dead)
        {
            //run away timer
            if (m_move < 0)
            {
                m_runAway -= Time.deltaTime;
                if (m_runAway <= 0)
                    m_move = 0;
            }
            else if (Vector3.Distance(transform.position, m_player.transform.position) <= m_runawayDist)
            {
                m_runAway = Random.Range(0.1f, Reaction);
                m_move = -1;
            }
            else
                // does actions based on player status
                switch (m_player.PlayerStatus)
                {
                    // the begining of the game - do nothing
                    case FightingStatus.None:
                        break;
                    // player is not attacking - attack
                    case FightingStatus.Idle:
                    case FightingStatus.UpperBlock:
                    case FightingStatus.MiddleBlock:
                    case FightingStatus.Hit:
                        Attack(m_player.PlayerStatus == FightingStatus.UpperBlock);
                        break;
                    // if player attacks too far - attack, otherwise - block
                    case FightingStatus.MiddleAttack:
                    case FightingStatus.LowerAttack:
                        if ((m_player.PlayerStatus == FightingStatus.LowerAttack && Vector3.Distance(transform.position, m_player.transform.position) <= m_lowerBlockDist) ||
                            m_player.PlayerStatus == FightingStatus.MiddleAttack && (Vector3.Distance(transform.position, m_player.transform.position) <= m_middleBlockDist))
                        {
                            goto default;
                        }
                        else
                        {
                            goto case FightingStatus.Idle;
                        }
                    case FightingStatus.Die:
                        m_move = 0;
                        StartCoroutine(Block(0f, false, false));
                        m_anim.SetBool(m_HashWin, true);
                        break;
                    //Randomly block player attack or move away
                    default:
                        if (Random.value < 0.65f)
                            StartCoroutine(Block(Reaction,
                                m_player.PlayerStatus == FightingStatus.LowerAttack, m_player.PlayerStatus == FightingStatus.MiddleAttack));
                        else
                        {
                            m_move = -1;
                            m_runAway = Reaction;
                        }
                        break;
                }


            m_anim.SetFloat(m_HashHorizontal, m_move);
        }

    }
    /// <summary>
    /// Attack player
    /// </summary>
    /// <param name="upperBlock"></param>
    void Attack(bool upperBlock)
    {
        // remove all blocks
        StartCoroutine(Block(0f, false, false));
        //if the enemy is close enough to make lower attack 
        if (Vector3.Distance(transform.position, m_player.transform.position) <= m_lowerAttackDist && !m_anim.IsInTransition(0))
        {
            //stop moving
            m_move = 0;
            //if the enemy is far enough to make the first upper attack 
            if (Vector3.Distance(transform.position, m_player.transform.position) <= m_upperAttack1Dist && !m_anim.IsInTransition(0))
            {
                //if the enemy is far enough to make the second upper attack 
                if (Vector3.Distance(transform.position, m_player.transform.position) <= m_upperAttack2Dist && !m_anim.IsInTransition(0))
                {
                    // randomly either make any attack or move away
                    float rnd = Random.value;
                    if (rnd <= 1f / 3f && !m_anim.IsInTransition(0))
                    {
                        LowerAttack(upperBlock);
                    }
                    else if (!upperBlock && rnd < 0.9f && !m_anim.IsInTransition(0))
                    {
                        m_anim.ResetTrigger(m_HashLowerAttack);
                        m_anim.SetTrigger(m_HashUpperAttack);
                        m_anim.SetInteger(m_HashRandom, rnd < 2f / 3f ? 1 : 2);
                    }
                    else if (!m_anim.IsInTransition(0))
                    {
                        m_runAway = Random.Range(0.1f, Reaction);
                        m_move = -1;
                    }
                }
                else
                {
                    // if player cant do the first upper attack - randomly either make lower attack, make the first upper attack, move closer, or move away
                    if (Random.value <= 0.4f && !m_anim.IsInTransition(0))
                    {
                        LowerAttack(upperBlock);

                    }
                    else if (!upperBlock && Random.value < 0.8f && !m_anim.IsInTransition(0))
                    {
                        m_anim.ResetTrigger(m_HashLowerAttack);
                        m_anim.SetTrigger(m_HashUpperAttack);
                        m_anim.SetInteger(m_HashRandom, 1);
                    }
                    else if (Random.value < 0.85f && !m_anim.IsInTransition(0))
                    {
                        m_move = 1;
                    }
                    else if (!m_anim.IsInTransition(0))
                    {
                        m_move = -1;
                    }
                }
            }
            else
            {
                // if player cant do the first upper attack - randomly either make lower attack or move closer
                if (Random.value < 0.7f && !m_anim.IsInTransition(0))
                {
                    LowerAttack(upperBlock);

                }
                else if (!m_anim.IsInTransition(0))
                {
                    m_move = 1;
                }
            }
        }
        // if the enemy is too far to attack - move towards player
        else if (!m_anim.IsInTransition(0))
        {
            m_move = 1;
        }
    }
    /// <summary>
    /// Random Attack based on player's upper block
    /// </summary>
    /// <param name="upperBlock">if player blocks</param>
    void LowerAttack(bool upperBlock)
    {
        m_anim.SetTrigger(m_HashLowerAttack);
        if (upperBlock)
        {
            m_anim.SetFloat(m_HashVertical, -1);
        }
        else
        {
            m_anim.SetFloat(m_HashVertical, 1);
        }

        m_anim.SetInteger(m_HashRandom, Random.Range(1, m_attackCount + 1));
    }
    /// <summary>
    /// Wait for <paramref name="time"/> and than set blocks 
    /// </summary>
    /// <param name="time">reaction time</param>
    /// <param name="lower">is lower block</param>
    /// <param name="middle">is middle block</param>
    /// <returns></returns>
    IEnumerator Block(float time, bool lower, bool middle)
    {
        yield return new WaitForSeconds(time);
        m_anim.SetBool(m_HashLowerBlock, lower);
        m_lowerBlock = lower;
        m_anim.SetBool(m_HashMiddleBlock, middle);
        m_middleBlock = middle;
    }

    private void OnAnimatorMove()
    {
        m_rb.MovePosition(m_rb.position + (m_bounds ? 0 : 1) * m_anim.deltaPosition.magnitude * m_move * m_speed * transform.forward);
    }

    void Hit(int hitPart)
    {
        m_health -= HitPoint;
        m_healthBar.value = m_health;
        if (m_health <= 0)
        {
            m_dead = true;
            m_healthBar.value = 0;
        }
        else
        {
            m_audio.PlayOneShot(m_hitSounds[Random.Range(0, m_hitSounds.Length)]);
            if (!m_bounds)
                m_rb.MovePosition(m_rb.position - transform.forward * 0.1f);
            m_anim.SetInteger(m_HashHitTarget, hitPart);
            m_anim.SetTrigger(m_HashHit);
            if (Random.value < 0.6f)
            {
                StartCoroutine(Block(Reaction, hitPart == 2, hitPart == 1));
            }
            else
            {
                Attack(m_player.PlayerStatus == FightingStatus.UpperBlock);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // if the enemy collides with players's hands and foots and player is attacking now

        if (!m_dead && other.gameObject.CompareTag("attack"))
        // && m_player?.PlayerStatus == FightingStatus.MiddleAttack || m_player?.PlayerStatus == FightingStatus.LowerAttack)
        {
            Vector3 point = other.ClosestPoint(transform.position);
            float part = m_col.bounds.size.y / 3f;
            if (m_col.bounds.min.y + part >= point.y && !m_lowerBlock)
            {
              //  Debug.Log("Slender Lower hit");
                Hit(2);
            }
            else if (m_col.bounds.min.y + part < point.y && !m_middleBlock)
            {
               // Debug.Log("Slender Middle hit");
                Hit(1);
            }
        }
        // if player collides with level bound and tries to go through it, srop moving
        else if (!m_dead && other.gameObject.CompareTag("bound"))
        {
            m_bounds = Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) < 0 && m_move < 0
                   || Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) > 0 && m_move > 0;
        }

    }
    /// <summary>
    /// Reset enemy's values, when the player is dead
    /// </summary>
    public void Reset()
    {
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
