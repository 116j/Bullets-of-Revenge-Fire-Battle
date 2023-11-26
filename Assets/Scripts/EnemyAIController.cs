using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField]
    Transform POV;
    [SerializeField]
    Transform centerOfMass;
    [SerializeField]
    Transform barrelLocation;
    [SerializeField]
    LayerMask spotOccupiedMask;

    Transform m_target;
    NavMeshAgent m_agent;
    NavMeshObstacle m_obstacle;
    Animator m_anim;
    ParticleSystem m_bullet;

    Action m_currentAction;
    Action m_afterSpotAction;
    GameObject m_currentCover;
    Vector3 m_standSpot;
    GameObject m_spotCol;

    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashReload = Animator.StringToHash("Reload");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");

    bool m_dead;
    float m_health = 5;
    int m_magazineStore;
    float m_waitTimer;
    bool m_rotating;
    bool m_hiding = false;
    bool m_noFree = false;
    bool m_onTheWay = false;
    bool m_cantSee = false;
    Vector3 m_findCoverForward;
    Vector3 m_targetPoint;

    readonly float m_walkSpeed = 2;
    readonly float m_runSpeed = 6;
    readonly float m_hit = 1.5f;
    readonly float m_detectDist = 40f;
    readonly float m_shootDist = 27f;
    readonly float m_safeDist = 5f;
    readonly float m_turnSpeed = 60f;
    readonly float m_waitTime = 2f;
    readonly int m_rifleCapacity = 30;
    readonly float m_crouchingPOV = 1.34f;
    // Start is called before the first frame update
    void Start()
    {
        m_target = GameObject.FindWithTag("Player").transform;
        m_agent = GetComponent<NavMeshAgent>();
        m_obstacle = GetComponent<NavMeshObstacle>();
        m_obstacle.enabled = false;
        m_anim = GetComponent<Animator>();
        m_bullet = GetComponentInChildren<ParticleSystem>();
        m_magazineStore = m_rifleCapacity;
        m_currentAction = Pursue;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(gameObject.name + " " + m_currentAction.Method.Name);
        if (!m_dead)
        {
            if (m_currentAction != Pursue && Vector3.Distance(transform.position, m_target.position) > m_detectDist)
            {
                EnableObstacle(false);
                m_currentAction = Pursue;
            }

            m_anim.SetFloat(m_HashVertical, m_agent.speed * (m_onTheWay || m_rotating ? 1f : 0f));
            m_currentAction();
        }
    }

    void EnableObstacle(bool enable)
    {
        m_agent.enabled = !enable;
        m_obstacle.enabled = enable;
    }

    void Pursue()
    {
        m_agent.speed = m_runSpeed;
        m_agent.SetDestination(m_target.position);
        if (!m_agent.pathPending && m_agent.hasPath)
        {
            m_onTheWay = true;
            if (Vector3.Distance(m_target.position, transform.position) < m_detectDist)
            {
                m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
                m_currentAction = FindCover;
            }
        }
    }


    void FindCover()
    {
        Debug.DrawLine(barrelLocation.position, barrelLocation.position + m_findCoverForward * m_shootDist, Color.green);
        var rayhits = Physics.SphereCastAll(barrelLocation.position, 3.5f, m_findCoverForward, m_detectDist, 1 << LayerMask.NameToLayer("Cover")).OrderBy(c => Vector3.Distance(m_target.position, c.collider.gameObject.transform.position)).ToList();
        if (rayhits.Count > 0)
        {
            float distZ = 0;
            foreach (var rayhit in rayhits)
            {
                distZ = Mathf.Abs(m_target.position.z - rayhit.collider.gameObject.transform.position.z);
                if (rayhit.collider.gameObject != m_currentCover
                    && distZ < m_shootDist
                    && distZ > m_safeDist)
                {
                    Debug.DrawLine(barrelLocation.position, barrelLocation.position + m_findCoverForward * m_shootDist, Color.black);
                    m_currentCover = rayhit.collider.gameObject;
                    if (Vector3.Distance(transform.position, m_currentCover.transform.position) > 5f)
                    {
                        m_agent.speed = m_runSpeed;
                    }
                    EnableObstacle(false);
                    m_currentAction = Reveal;
                    return;
                }
            }

            if (distZ > m_shootDist && !m_noFree)
            {
                m_noFree = true;
                return;
            }
        }

        bool dir = false;
        if (Physics.Raycast(transform.position, Vector3.left, out RaycastHit leftHit, m_detectDist, 1 << LayerMask.NameToLayer("Enviroment")))
        {
            if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit rightHit, m_detectDist, 1 << LayerMask.NameToLayer("Enviroment")))
            {
                dir = leftHit.distance > rightHit.distance;
            }
        }

        m_findCoverForward = Quaternion.AngleAxis(dir ? 1 : -1 * m_turnSpeed / 2, Vector3.up) * m_findCoverForward;
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.rotation.eulerAngles.y + (dir ? 7 : -7), 0f), Time.deltaTime * 7f);
    }


    bool DetectPlayerShoot()
    {
        RaycastHit[] objsInArea = Physics.SphereCastAll(transform.position + (Vector3.up * 1.5f), 1.5f, transform.forward, 0.001f, 1 << LayerMask.NameToLayer("Bullet"), QueryTriggerInteraction.Collide);
        return objsInArea.Length > 0;
    }

    bool ChooseCoverSpot(bool hide)
    {
        var coverBounds = m_currentCover.GetComponent<Collider>().bounds;
        bool found;
        Vector3 coverPos = new Vector3(barrelLocation.position.x, hide ? m_crouchingPOV : POV.position.y, coverBounds.center.z);
        if (m_target.position.z < m_currentCover.transform.position.z)
        {
            coverPos += Vector3.forward;
        }
        else
        {
            coverPos -= Vector3.forward;
        }
        if (coverBounds.max.x < transform.position.x)
        {
            coverPos.x = coverBounds.max.x;
            m_standSpot = FindSpot(hide, -0.1f, coverBounds, coverPos, out found, out _);
        }
        else if (coverBounds.min.x > transform.position.x)
        {
            coverPos.x = coverBounds.min.x;
            m_standSpot = FindSpot(hide, 0.1f, coverBounds, coverPos, out found, out _);
        }
        else
        {
            Vector3 rightSpot = FindSpot(hide, 0.1f, coverBounds, coverPos, out bool rfound, out bool rpriority);
            Vector3 leftSpot = FindSpot(hide, -0.1f, coverBounds, coverPos, out bool lfound, out bool lpriority);
            found = lfound | rfound;
            if (found && Vector3.Distance(transform.position, rightSpot) < Vector3.Distance(transform.position, leftSpot))
            {
                if ((lpriority && !rpriority) || !rfound)
                    m_standSpot = leftSpot;
                else if (rfound)
                    m_standSpot = rightSpot;
            }
            else if (found)
            {
                if ((rpriority && !lpriority) || !lfound)
                    m_standSpot = rightSpot;
                else if (lfound)
                    m_standSpot = leftSpot;
            }
        }

        if (!found && !m_noFree)
        {
            return false;
        }

        if (m_noFree && m_standSpot.x >= coverBounds.max.x)
        {
            m_standSpot.x += 0.5f;
        }
        else if (m_noFree && m_standSpot.x <= coverBounds.min.x)
        {
            m_standSpot.x -= 0.5f;
        }

        var rayhits = Physics.SphereCastAll(m_standSpot, 0.8f, Vector3.forward, 0.001f, spotOccupiedMask, QueryTriggerInteraction.Collide);
        if (rayhits.Length == 0 || (rayhits.Length == 1 && rayhits[0].collider.gameObject == gameObject))
        {
            m_spotCol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_spotCol.GetComponent<Renderer>().enabled = false;
            var col = m_spotCol.GetComponent<SphereCollider>();
            col.radius = 0.8f;
            col.isTrigger = true;
            m_spotCol.transform.position = m_standSpot;
            m_spotCol.layer = LayerMask.NameToLayer("Spot");
        }
        else
            return false;

        m_noFree = m_cantSee = false;

        return true;
    }

    Vector3 FindSpot(bool hide, float delta, Bounds coverBounds, Vector3 coverPos, out bool found, out bool priority)
    {
        found = priority = false;
        Vector3 potentialPos = Vector3.one;
        NavMeshPath path = new NavMeshPath();
        while (!found && coverPos.x >= coverBounds.min.x && coverPos.x <= coverBounds.max.x)
        {
            coverPos.x += delta;
            var rayhits = Physics.SphereCastAll(coverPos, 0.8f, Vector3.forward, 0.001f, spotOccupiedMask, QueryTriggerInteraction.Collide);
            NavMesh.CalculatePath(transform.position, new Vector3(coverPos.x, transform.position.y, coverPos.z), NavMesh.AllAreas, path);
            if (path.status == NavMeshPathStatus.PathComplete && (rayhits.Length == 0 || (rayhits.Length == 1 && rayhits[0].collider.gameObject == gameObject)))
            {
                if (Physics.Raycast(coverPos, new Vector3(0f, 0f, m_target.position.z - coverPos.z), out RaycastHit hitInfo, m_detectDist, 1 << LayerMask.NameToLayer("Cover")))
                {
                    found = hide && hitInfo.collider.gameObject == m_currentCover;
                    priority = found;
                }
                else if (!hide &&
                    !Physics.Raycast(new Vector3(coverPos.x, barrelLocation.position.y - 0.1f, coverPos.z), m_target.position - new Vector3(coverPos.x, barrelLocation.position.y, coverPos.z), m_safeDist, 1 << LayerMask.NameToLayer("Cover")))
                {
                    if (m_cantSee && (Vector3.Distance(transform.position, coverPos) < 0.5f))
                    {
                        continue;
                    }
                    if (Physics.Raycast(new Vector3(coverPos.x, centerOfMass.position.y, coverPos.z), new Vector3(0f, 0f, m_target.position.z - coverPos.z), out hitInfo, m_detectDist, 1 << LayerMask.NameToLayer("Cover")))
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
        if (!found && potentialPos != Vector3.one)
        {
            found = true;
            return potentialPos;
        }
        return coverPos;
    }

    void Reveal()
    {
        if (Mathf.Abs(m_target.position.z - m_currentCover.transform.position.z) > m_shootDist)
        {
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }
        if (ChooseCoverSpot(false))
        {
            EnableObstacle(false);
            m_onTheWay = true;
            m_afterSpotAction = Attack;
            m_currentAction = GoToSpot;
        }
        else
        {
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
        }
    }

    void Hide()
    {
        if (m_spotCol != null)
        {
            Destroy(m_spotCol);
            m_spotCol = null;
        }
        if (ChooseCoverSpot(true))
        {
            EnableObstacle(false);
            m_onTheWay = true;
            m_afterSpotAction = Wait;
            m_currentAction = GoToSpot;
        }
        else
        {
            m_anim.SetBool(m_HashCrouching, false);
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
        }
    }

    void GoToSpot()
    {
        m_agent.SetDestination(m_standSpot);

        if (!m_agent.pathPending)
        {
            if (m_agent.remainingDistance <= m_agent.stoppingDistance)
            {
                if (!m_agent.hasPath || m_agent.velocity.sqrMagnitude == 0f)
                {
                    Destroy(m_spotCol);
                    m_spotCol = null;
                    m_onTheWay = false;
                    m_agent.ResetPath();
                    m_agent.speed = m_walkSpeed;
                    EnableObstacle(true);
                    m_currentAction = m_afterSpotAction;
                }
            }
        }

    }

    void Attack()
    {
        if (m_magazineStore <= 0)
        {
            m_magazineStore = m_rifleCapacity;
            m_anim.SetTrigger(m_HashReload);
        }
        if (Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_standSpot.z), m_target.position - m_standSpot) < 0.2f
            || Mathf.Abs(m_currentCover.transform.position.z - m_standSpot.z) > m_shootDist)
        {
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }
        else if (DetectPlayerShoot())
        {
            m_hiding = true;
            m_currentAction = Hide;
        }
        else
        {
            Quaternion rotateDir = Quaternion.RotateTowards(barrelLocation.rotation, Quaternion.LookRotation(new Vector3(m_target.position.x - barrelLocation.position.x, 0f, m_target.position.z - barrelLocation.position.z)), Time.deltaTime * m_turnSpeed);
            m_rotating = Quaternion.Dot(barrelLocation.rotation, rotateDir) < 0.95f;
            if (m_rotating)
            {
                transform.rotation = rotateDir;
            }
            if (!m_rotating && Physics.Raycast(barrelLocation.position, m_target.position - barrelLocation.position, out RaycastHit hitInfo, m_shootDist, 1 << LayerMask.NameToLayer("Cover")))
            {
                m_cantSee = hitInfo.collider.gameObject == m_currentCover;
                if (m_cantSee)
                {
                    m_currentAction = Reveal;
                    return;
                }
            }

            if (Physics.SphereCast(barrelLocation.position, 1f, m_target.position - barrelLocation.position, out hitInfo, m_shootDist, 1 << m_target.gameObject.layer))
            {
                if (Physics.Raycast(barrelLocation.position, hitInfo.point - barrelLocation.position, m_shootDist, 1 << m_target.gameObject.layer))
                {
                    m_targetPoint = hitInfo.point;
                    m_anim.SetTrigger(m_HashShooting);
                }
            }
        }
    }

    void Wait()
    {
        m_anim.SetBool(m_HashCrouching, true);
        if (Physics.Raycast(barrelLocation.position, m_target.position - barrelLocation.position, m_detectDist, 1 << m_target.gameObject.layer)
            && Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_standSpot.z), m_target.position - m_standSpot) < 0.2f)
        {
            m_anim.SetBool(m_HashCrouching, false);
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }
        m_waitTimer += Time.deltaTime;
        Quaternion rotateDir = Quaternion.RotateTowards(barrelLocation.rotation, Quaternion.LookRotation(new Vector3(m_target.position.x - barrelLocation.position.x, 0f, m_target.position.z - barrelLocation.position.z)), Time.deltaTime * m_turnSpeed);
        m_rotating = Quaternion.Dot(barrelLocation.rotation, rotateDir) < 0.95f;
        if (m_rotating)
        {
            transform.rotation = rotateDir;
        }
        if (m_waitTimer >= m_waitTime && !DetectPlayerShoot())
        {
            m_hiding = false;
            m_anim.SetBool(m_HashCrouching, false);
            m_waitTimer = 0;
            m_currentAction = Reveal;
        }
    }

    void Hit()
    {
        m_anim.SetTrigger(m_HashHit);
        m_health -= m_hit;
        if (m_health <= 0)
        {
            Die();
        }
        if (!m_hiding)
        {
            m_hiding = true;
            EnableObstacle(false);
            m_currentAction = Hide;
        }
    }

    void Die()
    {
        m_dead = true;
        EnableObstacle(true);
        m_obstacle.radius = m_agent.height;
        m_obstacle.height = m_agent.radius;
        if (m_spotCol != null)
        {
            Destroy(m_spotCol);
            m_spotCol = null;
        }
        m_anim.SetTrigger(m_HashDie);
    }


    public void Shoot()
    {
        m_magazineStore--;
        m_bullet.transform.LookAt(m_targetPoint);
        m_bullet.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("bullet"))
        {
            Debug.Log("HIT");
            Hit();
        }
    }
}
