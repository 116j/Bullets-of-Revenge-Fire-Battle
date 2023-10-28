using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    Transform m_target;
    NavMeshAgent m_agent;
    Animator m_anim;

    Action m_currrentAction;

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
    readonly float m_stayDist = 30f;
    // Start is called before the first frame update
    void Start()
    {
        m_target = GameObject.FindWithTag("Player").transform;
        m_agent = GetComponent<NavMeshAgent>();
        m_currrentAction = Pursue;
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_dead)
        {
            m_currrentAction();
            m_anim.SetFloat(m_HashSpeed, m_agent.speed);
        }
    }

    void Pursue()
    {
        m_agent.speed = m_runSpeed;
        m_agent.SetDestination(m_target.position);
        if(m_agent.hasPath)
        {
            if(m_agent.remainingDistance<m_stayDist)
            {
                FindObstacle();
            }
        }
    }

    
    void FindObstacle()
    {

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

    void Hide()
    {

    }

    void Attack()
    {

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
