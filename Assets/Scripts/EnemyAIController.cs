using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField]
    Transform POV;
    [SerializeField]
    Transform centerOfMass;
    [SerializeField]
    LayerMask playerLayer;
    [SerializeField]
    LayerMask coverLayer;

    Transform m_target;
    NavMeshAgent m_agent;
    Animator m_anim;

    Action m_currentAction;
    Action m_afterSpotAction;
    GameObject m_currentCover;
    Vector3 m_standSpot;

    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");

    bool m_dead;
    float m_health = 5;

    readonly float m_walkSpeed = 2;
    readonly float m_runSpeed = 5;
    readonly float m_hit = 1.5f;
    readonly float m_detectDist = 50f;
    readonly float m_visAngle = 60f;
    // Start is called before the first frame update
    void Start()
    {
        m_target = GameObject.FindWithTag("Player").transform;
        m_agent = GetComponent<NavMeshAgent>();
        m_anim = GetComponent<Animator>();
        m_currentAction = Pursue;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_dead)
        {
            m_currentAction();
            m_anim.SetFloat(m_HashSpeed, m_agent.speed);
        }
    }

    void Pursue()
    {
        m_agent.speed = m_runSpeed;
        m_agent.SetDestination(m_target.position);
        if (m_agent.hasPath)
        {
            if (m_agent.remainingDistance < m_detectDist)
            {
                m_agent.isStopped = true;
                m_currentAction = FindCover;
            }
        }
    }


    void FindCover()
    {
        if (Physics.SphereCast(POV.position, 3.5f, POV.forward, out RaycastHit hitInfo, m_detectDist, coverLayer))
        {
            m_currentCover = hitInfo.collider.gameObject;

            m_currentAction = Reveal;
        }
    }


    bool DetectPlayerShoot()
    {
        int layerMask = ~(1 << (playerLayer | coverLayer | gameObject.layer));
        RaycastHit[] objsInArea = Physics.SphereCastAll(transform.position + Vector3.up * 1.5f, 1.5f, transform.forward, 0.1f, layerMask);
        foreach (RaycastHit obj in objsInArea)
        {
            if (obj.collider.gameObject.CompareTag("bullet"))
                return true;
        }
        return false;
    }

    void Hit()
    {
        m_anim.SetTrigger(m_HashHit);
        m_health -= m_hit;
        if (m_health <= 0)
        {
            Die();
        }
    }

    void ChooseCoverSpot(bool hide)
    {
        var coverBounds = m_currentCover.GetComponent<Collider>().bounds;
        if (coverBounds.max.x < transform.position.x)
        {
            m_standSpot = FindSpot(hide, -0.1f, coverBounds, new Vector3(coverBounds.max.x, POV.position.y, coverBounds.center.z), out _);
        }
        else if (coverBounds.min.x > transform.position.x)
        {
            m_standSpot = FindSpot(hide, 0.1f, coverBounds, new Vector3(coverBounds.min.x, POV.position.y, coverBounds.center.z), out _);

        }
        else
        {
            Vector3 coverPos = new Vector3(POV.position.x, POV.position.y, coverBounds.center.z);
            Vector3 rightSpot = FindSpot(hide, 0.1f, coverBounds, coverPos, out bool rpriority);
            Vector3 leftSpot = FindSpot(hide, -0.1f, coverBounds, coverPos, out bool lpriority);
            if (Vector3.Distance(transform.position, rightSpot) < Vector3.Distance(transform.position, leftSpot))
            {
                if (lpriority && !rpriority)
                    m_standSpot = leftSpot;
                else
                    m_standSpot = rightSpot;
            }
            else
            {
                if (rpriority && !lpriority)
                    m_standSpot = rightSpot;
                else
                    m_standSpot = rightSpot;
            }
        }

        if (m_standSpot.x >= coverBounds.max.x)
        {
            m_standSpot.x += 0.5f;
        }
        else if (m_standSpot.x <= coverBounds.min.x)
        {
            m_standSpot.x -= 0.5f;
        }
        if (Vector3.Dot(transform.position - m_standSpot, m_target.position - m_standSpot) < 0)
        {
            m_standSpot += Vector3.forward;
        }
        else
        {
            m_standSpot -= Vector3.forward;
        }
    }

    Vector3 FindSpot(bool hide, float delta, Bounds coverBounds, Vector3 coverPos, out bool priority)
    {
        bool found = priority = false;
        Vector3 potentialPos = coverPos;
        NavMeshPath path = new NavMeshPath();
        while (!found && coverPos.x >= coverBounds.min.x && coverPos.x <= coverBounds.max.x)
        {
            coverPos.x += delta;
            if (m_agent.CalculatePath(coverPos,path) && !Physics.SphereCast(coverPos, 0.8f, Vector3.forward, out RaycastHit hitInfo, 0.1f, gameObject.layer))
            {
                if (Physics.Raycast(new Vector3(coverPos.x, coverPos.y, POV.position.z), new Vector3(0f, 0f, coverPos.z - POV.position.z), out  hitInfo, m_detectDist, coverLayer))
                {
                    found = hide && hitInfo.collider.gameObject == m_currentCover;
                    priority = found;
                }
                else if (!hide)
                {
                    if (Physics.Raycast(new Vector3(coverPos.x, centerOfMass.position.y, centerOfMass.position.z), new Vector3(0f, 0f, coverPos.z - centerOfMass.position.z), out hitInfo, m_detectDist, coverLayer))
                    {
                        found = hitInfo.collider.gameObject == m_currentCover;
                        priority = found;
                    }
                    else
                    {
                        potentialPos = coverPos;
                    }
                }
            }

        }
        if (coverPos.x > coverBounds.max.x || coverPos.x < coverBounds.min.x)
        {
            coverPos = potentialPos;
        }
        return coverPos;
    }

    void Reveal()
    {
        ChooseCoverSpot(false);

        m_afterSpotAction = Attack;
        m_currentAction = GoToSpot;
    }

    void Hide()
    {
        ChooseCoverSpot(true);
    }

    void GoToSpot()
    {
        m_agent.isStopped = false;
        m_agent.SetDestination(m_standSpot);
        if (m_agent.hasPath)
        {
            if (m_agent.remainingDistance <= m_agent.stoppingDistance)
            {
                m_currentAction = m_afterSpotAction;
            }
        }
    }

    void Attack()
    {
        if(DetectPlayerShoot())
        {
            m_currentAction = Hide;
        }
        else
        {

        }
    }

    void Die()
    {
        m_dead = true;
        m_anim.SetTrigger(m_HashDie);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("bullet"))
        {
            Hit();
        }
    }
}
