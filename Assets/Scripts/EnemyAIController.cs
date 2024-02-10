using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField]
    //center of the body
    Transform center;
    [SerializeField]
    //aimRig's aim
    Transform TargetL;
    [SerializeField]
    Transform TargetR;
    [SerializeField]
    //top of the rifle
    Transform barrelLocation;
    [SerializeField]
    //mask for detecting an ocupped spot in the cover
    LayerMask spotOccupiedMask;

    //the number of created spots
    static int m_spotNum = 0;

    //player
    Transform m_target;
    NavMeshAgent m_agent;
    NavMeshObstacle m_obstacle;
    Animator m_anim;
    CapsuleCollider m_col;
    ParticleSystem m_bullet;

    Action m_currentAction;
    GameObject m_currentCover;
    GameObject m_spotCol;

    readonly int m_HashHide = Animator.StringToHash("Hide");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashAiming = Animator.StringToHash("Aiming");
    readonly int m_HashReload = Animator.StringToHash("Reload");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");

    bool m_dead;
    float m_health = 5;
    int m_magazineStore;
    //
    float m_waitTimer;
    bool m_rotating;
    //if there's no free cover in shoot dist
    bool m_noFree = false;
    //if witch direction covers will be detected
    Vector3 m_findCoverForward;
    //shoot direction
    Vector3 m_targetPoint;
    float m_LPitch;
    float m_LYaw;
    float m_RPitch;
    float m_RYaw;

    readonly float m_walkSpeed = 4f;
    readonly float m_runSpeed = 7;
    //how many point decrease from health when the enemy is hit
    readonly float m_hit = 1.5f;
    //stop pursuing and start finding a cover
    readonly float m_detectDist = 40f;
    //distance where enemy can shoot the player
    readonly float m_shootDist = 30f;
    //distance where the enemy shood go away from the player
    readonly float m_safeDist = 8f;
    readonly float m_turnSpeed = 60f;
    //time when the player should stop hiding
    readonly float m_waitTime = 2f;
    readonly int m_rifleCapacity = 30;
    readonly float m_detectRadius = 0.6f;
    readonly float m_hideOffset = 0.35f;
    readonly float m_riffleRotationStep = 0.5f;

    Quaternion m_baseTL;
    Quaternion m_baseTR;
    float m_rifleRotation;

    // Start is called before the first frame update
    void Start()
    {
        Transform aimRig = transform.Find("AimRig");
        m_baseTL = TargetL.transform.localRotation;
        m_baseTR = TargetR.transform.localRotation;

        m_target = GameObject.FindWithTag("Player").transform;
        m_agent = GetComponent<NavMeshAgent>();
        m_obstacle = GetComponent<NavMeshObstacle>();
        m_obstacle.enabled = false;
        m_anim = GetComponent<Animator>();
        m_col = GetComponent<CapsuleCollider>();
        m_bullet = GetComponentInChildren<ParticleSystem>();
        m_magazineStore = m_rifleCapacity;
        m_currentAction = Pursue;
       // m_dead = true;
        ResetAim();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(gameObject.name + " " + m_currentAction.Method.Name);

        if (!m_dead)
        {
            //if the player is feather than detectDist - start pursuing
            if (m_currentAction != Pursue && Mathf.Abs(m_target.position.z - transform.position.z) > m_detectDist)
            {
                if (m_spotCol != null)
                {
                    Destroy(m_spotCol);
                    m_spotCol = null;
                }
                if (m_currentAction == Hide)
                {
                    m_col.center = new(m_col.center.x, m_col.center.y + m_hideOffset, 0f);
                    m_col.height += (m_hideOffset + 0.2f);
                }
                m_anim.SetBool(m_HashAiming, false);
                EnableObstacle(false);
                m_currentAction = Pursue;
            }
            //if the player is too far to shoot or too close - find new cover
            else if ((m_currentCover != null && Mathf.Abs(m_target.position.z - m_currentCover.transform.position.z) > m_shootDist))
            {
                if (m_currentAction == Hide)
                {
                    m_col.center = new(m_col.center.x, m_col.center.y + m_hideOffset, 0f);
                    m_col.height += (m_hideOffset + 0.2f);
                }
                EnableObstacle(false);
                m_anim.SetBool(m_HashAiming, false);
                m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
                m_currentAction = FindCover;
            }

            m_anim.SetBool(m_HashHide, m_currentAction == Hide);
            m_anim.SetFloat(m_HashSpeed, m_agent.speed * (m_agent.velocity.sqrMagnitude > 0.01f || m_rotating ? 1f : 0f));
            m_currentAction();

            if (m_currentAction != Attack)
            {
                Attack();
            }
        }
    }
    /// <summary>
    /// Enables obstacle and disables agent, or otherwise
    /// </summary>
    /// <param name="enable">if enable or disable</param>
    void EnableObstacle(bool enable)
    {
        m_agent.enabled = !enable;
        m_obstacle.enabled = enable;
    }
    /// <summary>
    /// Sets aim and rifle transforms to the start point and disables aimRig
    /// </summary>
    void ResetAim()
    {
        TargetL.localRotation = m_baseTL;
        TargetR.localRotation = m_baseTR;
        m_RPitch = m_baseTR.eulerAngles.y;
        m_RYaw = m_baseTR.eulerAngles.z;
        m_LPitch = m_baseTL.eulerAngles.y;
        m_LYaw = m_baseTL.eulerAngles.z;

        m_anim.SetBool(m_HashAiming, false);
    }
    /// <summary>
    /// The enemy starts running to the player using NavMeshAgent until detectDist
    /// </summary>
    void Pursue()
    {
        m_agent.speed = m_runSpeed;
        m_agent.SetDestination(m_target.position);
        if (!m_agent.pathPending && m_agent.hasPath)
        {
            if (Mathf.Abs(m_target.position.z - transform.position.z) < m_detectDist)
            {
                m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
                m_currentAction = FindCover;
            }
        }
    }

    /// <summary>
    /// Finds cover that is 
    /// </summary>
    void FindCover()
    {
        if (m_spotCol != null)
        {
            Destroy(m_spotCol);
            m_spotCol = null;
        }

        var rayhits = Physics.SphereCastAll(barrelLocation.position, 3.5f, m_findCoverForward, m_detectDist, 1 << LayerMask.NameToLayer("Cover"))
            .OrderBy(c => Vector3.Distance(m_target.position, c.collider.gameObject.transform.position)).ToList();
        if (rayhits.Count > 0)
        {
            float distZ = 0;
            foreach (var rayhit in rayhits)
            {
                //distance from a cover to the enemy
                distZ = Mathf.Abs(m_target.position.z - rayhit.collider.gameObject.transform.position.z);
                if (rayhit.collider.gameObject != m_currentCover
                    && distZ < m_shootDist
                    && distZ > m_safeDist)
                // && distZ > Mathf.Abs(transform.position.z - rayhit.collider.gameObject.transform.position.z))
                {
                    m_currentCover = rayhit.collider.gameObject;
                    if (Vector3.Distance(transform.position, m_currentCover.transform.position) > 5f)
                    {
                        m_agent.speed = m_runSpeed;
                    }
                    else
                    {
                        m_agent.speed = m_walkSpeed;
                    }
                    EnableObstacle(false);
                    m_currentAction = ChooseCoverSpot;
                    return;
                }
            }
            //if there's no free cover in shootDist
            if (distZ > m_shootDist && !m_noFree)
            {
                m_noFree = true;
                return;
            }
        }

        bool dir = false;
        //If player is closer to the left or to the right
        if (Physics.Raycast(transform.position, Vector3.left, out RaycastHit leftHit, m_detectDist, 1 << LayerMask.NameToLayer("Enviroment")))
        {
            if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit rightHit, m_detectDist, 1 << LayerMask.NameToLayer("Enviroment")))
            {
                dir = leftHit.distance > rightHit.distance;
            }
        }
        //rotates cover detection vector
        m_findCoverForward = Quaternion.Euler(0f, dir ? 1 : -1 * m_turnSpeed / 2, 0f) * m_findCoverForward;
    }

    /// <summary>
    /// Detect if there are  bullets around the enemy in the moment
    /// </summary>
    /// <returns></returns>
    bool DetectPlayerShoot()
    {
        RaycastHit[] objsInArea = Physics.SphereCastAll(transform.position + Vector3.up * m_detectRadius, m_detectRadius, transform.forward, 0.001f, 1 << LayerMask.NameToLayer("Bullet"));
        return objsInArea.Length > 0;
    }
    /// <summary>
    /// Choses a free spot in the cover
    /// </summary>
    void ChooseCoverSpot()
    {
        var coverBounds = m_currentCover.GetComponent<Collider>().bounds;
        bool found;
        Vector3 standSpot = new Vector3(transform.position.x, center.position.y, coverBounds.center.z);

        //an indention from the cover
        if (m_target.position.z < m_currentCover.transform.position.z)
        {
            standSpot += Vector3.forward * 1.5f;
        }
        else
        {
            standSpot -= Vector3.forward * 1.5f;
        }

        if (coverBounds.max.x < transform.position.x)
        {
            standSpot.x = coverBounds.max.x;
            standSpot = FindSpot(-0.1f, coverBounds, standSpot, out found);
        }
        else if (coverBounds.min.x > transform.position.x)
        {
            standSpot.x = coverBounds.min.x;
            standSpot = FindSpot(0.1f, coverBounds, standSpot, out found);
        }
        else
        {
            Vector3 rightSpot = FindSpot(0.1f, coverBounds, standSpot, out bool rfound);
            Vector3 leftSpot = FindSpot(-0.1f, coverBounds, standSpot, out bool lfound);
            found = lfound | rfound;

            if (found && Vector3.Distance(transform.position, rightSpot) < Vector3.Distance(transform.position, leftSpot))
            {
                if (!rfound)
                    standSpot = leftSpot;
                else
                    standSpot = rightSpot;
            }
            else if (found)
            {
                if (!lfound)
                    standSpot = rightSpot;
                else
                    standSpot = leftSpot;
            }
        }
        //if a spot is not found - seeking a new cover
        if (!found && !m_noFree)
        {
            m_currentAction = FindCover;
            return;
        }

        //if a spot is not behind the cover - add indention
        if (m_noFree && standSpot.x >= coverBounds.max.x)
        {
            standSpot.x += 0.5f;
        }
        else if (m_noFree && standSpot.x <= coverBounds.min.x)
        {
            standSpot.x -= 0.5f;
        }

        //if there's no taken spots - create new spot
        var rayhits = Physics.SphereCastAll(standSpot, m_detectRadius, Vector3.forward, 0.001f, spotOccupiedMask, QueryTriggerInteraction.Collide);
        if (rayhits.Length == 0 || (rayhits.Length == 1 && rayhits[0].collider.gameObject == gameObject))
        {
            m_spotCol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_spotCol.name = m_spotNum++.ToString();
            m_spotCol.GetComponent<Renderer>().enabled = false;
            var col = m_spotCol.GetComponent<SphereCollider>();
            col.radius = m_detectRadius;
            col.isTrigger = true;
            standSpot.y = transform.position.y;
            m_spotCol.transform.position = standSpot;
            m_spotCol.layer = LayerMask.NameToLayer("Spot");
        }
        else
        {
            m_currentAction = FindCover;
            return;
        }

        m_noFree = false;
        m_currentAction = GoToSpot;
    }

    /// <summary>
    /// Find
    /// </summary>
    /// <param name="delta">step </param>
    /// <param name="coverBounds"></param>
    /// <param name="coverPos"></param>
    /// <param name="found"></param>
    /// <returns></returns>
    Vector3 FindSpot(float delta, Bounds coverBounds, Vector3 coverPos, out bool found)
    {
        found = false;
        NavMeshPath path = new NavMeshPath();
        while (!found && coverPos.x >= coverBounds.min.x && coverPos.x <= coverBounds.max.x)
        {
            coverPos.x += delta;
            var rayhits = Physics.SphereCastAll(coverPos, m_detectRadius, Vector3.forward, 0.001f, spotOccupiedMask, QueryTriggerInteraction.Collide);
            NavMesh.CalculatePath(transform.position, new Vector3(coverPos.x, transform.position.y, coverPos.z), NavMesh.AllAreas, path);
            if (path.status == NavMeshPathStatus.PathComplete && (rayhits.Length == 0 || (rayhits.Length == 1 && rayhits[0].collider.gameObject == gameObject)))
            {
                if (Physics.Raycast(coverPos + Vector3.right * 0.3f, new Vector3(0f, 0f, m_target.position.z - coverPos.z), out RaycastHit hitInfo, m_detectDist, 1 << LayerMask.NameToLayer("Cover")))
                {
                    if (hitInfo.collider.gameObject == m_currentCover &&
                        Physics.Raycast(coverPos - Vector3.right * 0.3f, new Vector3(0f, 0f, m_target.position.z - coverPos.z), out hitInfo, m_detectDist, 1 << LayerMask.NameToLayer("Cover")))
                    {
                        found = hitInfo.collider.gameObject == m_currentCover;
                    }
                }
            }
        }
        return coverPos;
    }

    /// <summary>
    /// Goes to the spot
    /// </summary>
    void GoToSpot()
    {
        //if our spot is deleted - choosing a new spot
        if (m_spotCol == null)
        {
            m_currentAction = ChooseCoverSpot;
            m_agent.ResetPath();
            return;
        }
        //spots in rhe current spot's place
        var rayhits = Physics.SphereCastAll(m_spotCol.transform.position, m_detectRadius, Vector3.forward, 0.001f, 1 << LayerMask.NameToLayer("Spot"), QueryTriggerInteraction.Collide);

        foreach (var hit in rayhits)
        {
            //if spot's place is taken by a spot or an enemy and spot's name is less than detected spot's name
            if (hit.collider.gameObject != gameObject && hit.collider.gameObject != m_spotCol)
            {
                if (Int32.Parse(hit.collider.gameObject.name) > Int32.Parse(m_spotCol.name))
                {
                    //delete the current spot
                    Destroy(m_spotCol);
                    m_spotCol = null;
                    return;
                }
            }
        }

        m_agent.SetDestination(m_spotCol.transform.position);

        if (!m_agent.pathPending && m_agent.remainingDistance <= m_agent.stoppingDistance
            || Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(m_spotCol.transform.position.x, 0f, m_spotCol.transform.position.z)) < 0.1f)
        {
            if (!m_agent.hasPath || m_agent.velocity.sqrMagnitude < 0.01f)
            {
                m_agent.ResetPath();
                m_agent.speed = m_walkSpeed;
                EnableObstacle(true);
                m_currentAction = Attack;
            }
        }
    }
    /// <summary>
    /// Ataacks the player
    /// </summary>
    void Attack()
    {
        //reloads the rifle
        if (m_magazineStore <= 0)
        {
            m_magazineStore = m_rifleCapacity;
            m_anim.SetTrigger(m_HashReload);
        }
        //if player is near - find new cover
        if (m_currentAction == Attack && Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_spotCol.transform.position.z), m_target.position - m_spotCol.transform.position) < 0.2f)
        {
            ResetAim();
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }
        //if the enemy is being shot - hide
        else if (m_currentAction == Attack && DetectPlayerShoot())
        {
            ResetAim();
            m_col.center = new(m_col.center.x, m_col.center.y - m_hideOffset, 0f);
            m_col.height -= (m_hideOffset + 0.2f);
            m_currentAction = Hide;
        }
        //if the player is in shootDist
        else if (Mathf.Abs(transform.position.z - m_target.position.z) < m_shootDist)
        {
            m_anim.SetBool(m_HashAiming, true);
            //if the enemy is not moving - rotate towards the player
            if (m_agent.velocity.sqrMagnitude < 0.01f)
            {
                Vector3 rotateDir = Vector3.RotateTowards(transform.forward, m_target.position - transform.position, Time.deltaTime * m_turnSpeed, 0f);
                rotateDir.y = transform.forward.y;
                m_rotating = Vector3.Angle(transform.forward, rotateDir) > 5f;
                if (m_rotating)
                {
                    transform.rotation = Quaternion.LookRotation(rotateDir);
                }
            }
            if (!m_anim.IsInTransition(1) && m_anim.GetCurrentAnimatorClipInfo(1)[0].clip.name == "Rifle_Aiming_Idle" && Vector3.Angle(barrelLocation.forward, (m_target.position - barrelLocation.position).normalized) < 15f)
            {
                //direction from the rifle's top to the player 
                Vector3 aimDir = m_target.transform.position + Vector3.up * 1.5f - barrelLocation.position - barrelLocation.forward * (m_target.transform.position + Vector3.up * 1.5f - barrelLocation.position).magnitude;

                if (Physics.SphereCast(barrelLocation.position, m_detectRadius, m_target.transform.position - barrelLocation.position, out RaycastHit hit, m_shootDist, 1 << m_target.gameObject.layer))
                {
                    aimDir = hit.point - barrelLocation.position - barrelLocation.forward * (hit.point - barrelLocation.position).magnitude;
                }
                // slowly moves aim towards the player
                if (Mathf.Abs(aimDir.x) > 0.02f)
                {
                    m_rifleRotation = (Mathf.Abs(aimDir.x) <= m_riffleRotationStep ? aimDir.x : (m_riffleRotationStep * Mathf.Sign(aimDir.x))) * Time.deltaTime * Mathf.Sign(m_target.position.z - transform.position.z) * m_turnSpeed;
                    m_RYaw += m_rifleRotation;
                    m_LYaw += m_rifleRotation;
                }
                if (Mathf.Abs(aimDir.y) > 0.02f)
                {
                    m_rifleRotation = (Mathf.Abs(aimDir.y) <= m_riffleRotationStep ? aimDir.y : (m_riffleRotationStep * Mathf.Sign(aimDir.y))) * Time.deltaTime * m_turnSpeed;
                    m_LPitch += m_rifleRotation;
                    m_RPitch += m_rifleRotation;
                }
                TargetR.localRotation = Quaternion.Slerp(TargetR.localRotation, Quaternion.Euler(TargetR.localRotation.eulerAngles.x, m_RPitch, m_RYaw), Time.deltaTime * m_turnSpeed);
                TargetL.localRotation = Quaternion.Slerp(TargetL.localRotation, Quaternion.Euler(TargetL.localRotation.eulerAngles.x, m_LPitch, m_LYaw), Time.deltaTime * m_turnSpeed);
                //if the rifle is pointinfg at the player - sets shoot target with a deviation
                if (Physics.SphereCast(barrelLocation.position, m_detectRadius, barrelLocation.forward, out hit, m_shootDist, 1 << m_target.gameObject.layer))
                {
                    if (Physics.Raycast(barrelLocation.position, barrelLocation.forward, out hit, m_shootDist, 1 << m_target.gameObject.layer))
                    {
                        m_targetPoint = hit.point + Vector3.right * UnityEngine.Random.Range(-(m_detectRadius - 0.1f), m_detectRadius - 0.1f) + Vector3.up * UnityEngine.Random.Range(-m_detectRadius, m_detectRadius);
                    }
                    else
                    {
                        m_targetPoint = hit.point;
                    }

                    m_anim.SetTrigger(m_HashShooting);
                }
            }

        }
        else
        {
            m_anim.SetBool(m_HashAiming, false);
        }
    }
    /// <summary>
    /// Hides behind the cover for the time
    /// </summary>
    void Hide()
    {
        //if the player is near the enemy - find new cover
        if (Physics.Raycast(barrelLocation.position, m_target.position - barrelLocation.position, m_detectDist, 1 << m_target.gameObject.layer)
            && Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_spotCol.transform.position.z), m_target.position - m_spotCol.transform.position) < 0.2f)
        {
            m_findCoverForward = (m_target.position - barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }

        m_waitTimer += Time.deltaTime;

        if (m_waitTimer >= m_waitTime && !DetectPlayerShoot())
        {
            m_waitTimer = 0;
            m_col.center = new(m_col.center.x, m_col.center.y + m_hideOffset, 0f);
            m_col.height += (m_hideOffset + 0.2f);
            m_currentAction = Attack;
        }
    }
    /// <summary>
    /// The enemy is hit by the player
    /// </summary>
    void Hit()
    {
        m_anim.SetTrigger(m_HashHit);
        m_health -= m_hit;
        if (m_health <= 0)
        {
            Die();
        }
        if (m_currentAction == Attack)
        {
            ResetAim();
            m_col.center = new(m_col.center.x, m_col.center.y - m_hideOffset, 0f);
            m_col.height -= (m_hideOffset + 0.2f);
            m_currentAction = Hide;
        }
    }

    void Die()
    {
        m_dead = true;
        EnableObstacle(true);
        gameObject.layer = LayerMask.NameToLayer("Enviroment");
        m_obstacle.center = Vector3.up * 0.5f;
        m_obstacle.size = new Vector3(m_agent.radius, 1f, m_agent.height);
        if (m_spotCol != null)
        {
            Destroy(m_spotCol);
            m_spotCol = null;
        }
        m_anim.SetTrigger(m_HashDie);
    }


    public void Shoot()
    {
        Debug.Log(gameObject.name + " Shooting...");
        m_magazineStore--;
        m_bullet.transform.LookAt(m_targetPoint);
        m_bullet.Play();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("bullet") && !m_dead)
        {
            Debug.Log(gameObject.name + " HIT");
            Hit();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        if (m_target != null)
            Gizmos.DrawLine(barrelLocation.position, m_target.position + Vector3.up * 1.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(barrelLocation.position, barrelLocation.position + barrelLocation.forward * m_shootDist);

    }
}
