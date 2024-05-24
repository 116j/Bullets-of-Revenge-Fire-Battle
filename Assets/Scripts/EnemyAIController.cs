using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
    [SerializeField]
    //center of the body
    Transform m_center;
    [SerializeField]
    //left arm rig target
    Transform m_TargetL;
    [SerializeField]
    //right arm rig target
    Transform m_TargetR;
    [SerializeField]
    //top of the rifle
    Transform m_barrelLocation;
    [SerializeField]
    //mask for detecting an ocupped spot in the cover
    LayerMask m_spotOccupiedMask;
    [SerializeField]
    AudioClip m_stepSound;
    [SerializeField]
    AudioClip m_reloadSound;

    //the number of created spots
    static int m_spotNum = 0;

    //player
    Transform m_target;
    NavMeshAgent m_agent;
    NavMeshObstacle m_obstacle;
    Animator m_anim;
    CapsuleCollider m_col;
    ParticleSystem m_bullet;
    AudioSource m_shootSound;
    AudioSource m_audio;

    Action m_currentAction;
    GameObject m_currentCover;
    //collider of spot where enemy is standing behind a cover
    GameObject m_spotCol;

    //hashes for animator parameters
    readonly int m_HashHide = Animator.StringToHash("Hide");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashAiming = Animator.StringToHash("Aiming");
    readonly int m_HashReload = Animator.StringToHash("Reload");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");
    readonly int m_HashShootSpeed = Animator.StringToHash("ShootSpeed");
    readonly int m_HashReloadSpeed = Animator.StringToHash("ReloadSpeed");

    bool m_dead;
    bool m_hide = false;
    float m_health = 3;
    //current amount of bullets in the riffle
    int m_magazineStore;
    //timer for hide
    float m_waitTimer;
    float m_hideTimer;
    //if enemy is rotating
    bool m_rotating;
    //if there's no free cover in shoot dist
    bool m_noFree = false;
    //if witch direction covers will be detected
    Vector3 m_findCoverForward;
    //shoot direction
    Vector3 m_targetPoint;
    //vertical movement of left arm rig
    float m_LPitch;
    //horizontal movement of left arm rig
    float m_LYaw;
    //vertical movement of right arm rig
    float m_RPitch;
    //horizontal movement of right arm rig
    float m_RYaw;

    readonly float m_walkSpeed = 4f;
    readonly float m_runSpeed = 7;
    //stop pursuing and start finding a cover
    readonly float m_detectDist = 40f;
    //distance where enemy can shoot the player
    readonly float m_shootDist = 30f;
    //distance where the enemy shood go away from the player
    readonly float m_safeDist = 8f;
    readonly float m_turnSpeed = 60f;
    //radius for spherecast
    readonly float m_detectRadius = 0.5f;
    //offset of collider change when enemy is hidding
    readonly float m_hideOffset = 0.35f;
    //max step per frame for riffle rig movement
    readonly float m_riffleRotationStep = 0.5f;

    //start riffle rig rotations
    Quaternion m_baseTL;
    Quaternion m_baseTR;
    //step per frame for riffle rig movement
    float m_rifleRotation;

    int RifleCapacity => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 30 : 50;
    //time when the player should stop hiding
    float WaitTime => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 2f : 4f;
    //how many point decrease from health when the enemy is hit
    float HitPoint => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 1f : 0.5f;
    float ShootSpeed => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.6f: 0.75f;
    float ReloadSpeed => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.2f: 0.25f;
    public bool IsDead => m_dead;

    // Start is called before the first frame update
    void Start()
    {
        m_baseTL = m_TargetL.transform.localRotation;
        m_baseTR = m_TargetR.transform.localRotation;

        m_target = GameObject.FindWithTag("Player").transform;
        m_audio = GetComponent<AudioSource>();
        m_agent = GetComponent<NavMeshAgent>();
        m_obstacle = GetComponent<NavMeshObstacle>();
        m_obstacle.enabled = false;
        m_anim = GetComponent<Animator>();
        m_col = GetComponent<CapsuleCollider>();
        m_bullet = GetComponentInChildren<ParticleSystem>();
        m_shootSound = m_barrelLocation.GetComponent<AudioSource>();
        m_magazineStore = RifleCapacity;
        m_currentAction = Pursue;
         m_dead = true;
        ResetAim();
    }

    // Update is called once per frame
    void Update()
    {
        m_anim.SetBool(m_HashDie, m_dead);

        Debug.Log(gameObject.name + " " + m_currentAction.Method.Name);
        if (!m_dead)
        {
            //if an enemy go to hide spot for waitTime without being hit - go to attack spot
            if (m_hide)
            {
                m_hideTimer += Time.deltaTime;
                if (m_hideTimer >= WaitTime)
                {
                    m_hideTimer = 0;
                    m_hide = false;
                }
            }
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
                m_findCoverForward = (m_target.position - m_barrelLocation.position).normalized;
                m_currentAction = FindCover;
            }
            //if an enemy is attacked while walking - find hide spot and go to it
            // if an enemy is hit while walking to hide spot - reset timer
            else if (m_currentCover != null && m_currentAction != Attack && DetectPlayerShoot())
            {
                m_hideTimer = 0;
                if (!m_hide)
                {
                    m_hide = true;
                    m_currentAction = FindCover;
                }
            }

            m_anim.SetBool(m_HashHide, m_currentAction == Hide);
            m_anim.SetFloat(m_HashSpeed, m_agent.speed * (m_agent.velocity.sqrMagnitude > 0.01f || m_rotating ? 1f : 0f));
            m_anim.SetFloat(m_HashShootSpeed, ShootSpeed);
            m_anim.SetFloat(m_HashReloadSpeed, ReloadSpeed);

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
        m_TargetL.localRotation = m_baseTL;
        m_TargetR.localRotation = m_baseTR;
        m_RPitch = m_baseTR.eulerAngles.y;
        m_RYaw = m_baseTR.eulerAngles.z;
        m_LPitch = m_baseTL.eulerAngles.y;
        m_LYaw = m_baseTL.eulerAngles.z;
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
                m_findCoverForward = (m_target.position - m_barrelLocation.position).normalized;
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
        RaycastHit[] rayhits;
        if (!m_hide)
        {
            rayhits = Physics.SphereCastAll(transform.position + Vector3.up * m_detectRadius, 3.5f, m_findCoverForward, m_detectDist, LayerMask.GetMask("Cover"))
               .OrderBy(c => Vector3.Distance(m_target.position, c.collider.gameObject.transform.position)).ToArray();
        }
        else
        {
            rayhits = Physics.SphereCastAll(transform.position + Vector3.up * m_detectRadius, 7f, transform.forward, 0.01f, LayerMask.GetMask("Cover"))
               .OrderBy(c => Vector3.Distance(transform.position, c.collider.gameObject.transform.position)).ToArray();
        }

        if (rayhits.Length > 0)
        {
            float CoverPlayerDistZ = 0;
            foreach (var rayhit in rayhits)
            {
                //distance from a cover to the enemy
                CoverPlayerDistZ = Mathf.Abs(m_target.position.z - rayhit.collider.gameObject.transform.position.z);
                if (rayhit.collider.gameObject != m_currentCover
                    && Mathf.Abs(m_target.position.z - transform.position.z)> Mathf.Abs(transform.position.z - rayhit.collider.gameObject.transform.position.z)
                    && CoverPlayerDistZ < m_shootDist
                    && CoverPlayerDistZ > m_safeDist)
                // && distZ > Mathf.Abs(transform.position.z - rayhit.collider.gameObject.transform.position.z))
                {
                    m_currentCover = rayhit.collider.gameObject;
                    EnableObstacle(false);
                    m_currentAction = ChooseCoverSpot;
                    return;
                }
            }
            //if there's no free cover in shootDist
            if (CoverPlayerDistZ > m_shootDist && !m_noFree)
            {
                m_noFree = true;
                return;
            }
        }

        bool dir = false;
        //If player is closer to the left or to the right
        if (Physics.Raycast(transform.position, Vector3.left, out RaycastHit leftHit, m_detectDist, LayerMask.GetMask("Enviroment")))
        {
            if (Physics.Raycast(transform.position, Vector3.right, out RaycastHit rightHit, m_detectDist, LayerMask.GetMask("Enviroment")))
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
        RaycastHit[] objsInArea = Physics.SphereCastAll(transform.position + Vector3.up * m_detectRadius, m_detectRadius, transform.forward, 0.01f, LayerMask.GetMask("Bullet"));
        return objsInArea.Length > 0;
    }
    /// <summary>
    /// Choses a free spot in the cover
    /// </summary>
    void ChooseCoverSpot()
    {
        var coverBounds = m_currentCover.GetComponent<Collider>().bounds;
        bool found;
        Vector3 standSpot = new Vector3(transform.position.x, m_center.position.y, coverBounds.center.z);

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
        var rayhits = Physics.SphereCastAll(standSpot, m_detectRadius, Vector3.forward, 0.001f, m_spotOccupiedMask, QueryTriggerInteraction.Collide);
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
            var rayhits = Physics.SphereCastAll(coverPos, m_detectRadius, Vector3.forward, 0.001f, m_spotOccupiedMask, QueryTriggerInteraction.Collide);
            NavMesh.CalculatePath(transform.position, new Vector3(coverPos.x, transform.position.y, coverPos.z), NavMesh.AllAreas, path);
            if (path.status == NavMeshPathStatus.PathComplete && (rayhits.Length == 0 || (rayhits.Length == 1 && rayhits[0].collider.gameObject == gameObject)))
            {
                if (Physics.Raycast(coverPos + Vector3.right * 0.3f, new Vector3(0f, 0f, m_target.position.z - coverPos.z), out RaycastHit hitInfo, m_detectDist, LayerMask.GetMask("Cover")))
                {
                    if (hitInfo.collider.gameObject == m_currentCover &&
                        Physics.Raycast(coverPos - Vector3.right * 0.3f, new Vector3(0f, 0f, m_target.position.z - coverPos.z), out hitInfo, m_detectDist, LayerMask.GetMask("Cover")))
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
        var rayhits = Physics.SphereCastAll(m_spotCol.transform.position, m_detectRadius, Vector3.forward, 0.001f, LayerMask.GetMask("Spot"), QueryTriggerInteraction.Collide);

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
                if (m_hide)
                {
                    ResetAim();
                    m_anim.SetBool(m_HashAiming, false);
                    m_col.center = new(m_col.center.x, m_col.center.y - m_hideOffset, 0f);
                    m_col.height -= (m_hideOffset + 0.2f);
                    m_currentAction = Hide;
                }
                else
                {
                    m_currentAction = Attack;
                }
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
            m_magazineStore = RifleCapacity;
            m_anim.SetTrigger(m_HashReload);
            m_audio.PlayOneShot(m_reloadSound);
        }
        //if player is near - find new cover
        if (m_currentAction == Attack && Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_spotCol.transform.position.z), m_target.position - m_spotCol.transform.position) < 0.4f)
        {
            ResetAim();
            m_anim.SetBool(m_HashAiming, false);
            m_findCoverForward = (m_target.position - m_barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }
        //if the enemy is being shot - hide
        else if (m_currentAction == Attack && DetectPlayerShoot())
        {
            ResetAim();
            m_anim.SetBool(m_HashAiming, false);
            m_col.center = new(m_col.center.x, m_col.center.y - m_hideOffset, 0f);
            m_col.height -= (m_hideOffset + 0.2f);
            m_hide = true;
            m_currentAction = Hide;
        }
        //if the player is in shootDist
        else if (Mathf.Abs(transform.position.z - m_target.position.z) < m_shootDist)
        {
            m_anim.SetBool(m_HashAiming, true);
            //if the enemy is not moving - rotate towards the player
            if (m_agent.velocity.sqrMagnitude < 0.01f)
            {
                Vector3 rotateDir = Vector3.RotateTowards(transform.forward, m_target.position + Vector3.up * 1.2f - transform.position, Time.deltaTime * m_turnSpeed, 0f);
                rotateDir.y = transform.forward.y;
                m_rotating = Vector3.Angle(transform.forward, rotateDir) > 5f;
                if (m_rotating)
                {
                    transform.rotation = Quaternion.LookRotation(rotateDir);
                }
            }
            if (!m_anim.IsInTransition(1) && m_anim.GetCurrentAnimatorClipInfo(1)[0].clip.name == "Rifle_Aiming_Idle" && Vector3.Angle(m_barrelLocation.forward, (m_target.position + Vector3.up - m_barrelLocation.position).normalized) < 15f)
            {
                //direction from the rifle's top to the player 
                Vector3 aimDir = m_target.position + Vector3.up - m_barrelLocation.position - m_barrelLocation.forward * (m_target.position + Vector3.up - m_barrelLocation.position).magnitude;

                if (Physics.SphereCast(m_barrelLocation.position, m_detectRadius, m_target.position + Vector3.up * 1.2f - m_barrelLocation.position, out RaycastHit hit, m_shootDist, 1 << m_target.gameObject.layer))
                {
                    aimDir = hit.point - m_barrelLocation.position - m_barrelLocation.forward * (hit.point - m_barrelLocation.position).magnitude;
                }
                // slowly moves aim towards the player
                if (Mathf.Abs(aimDir.x) > 0.02f)
                {
                    m_rifleRotation = Mathf.Abs(aimDir.x) <= m_riffleRotationStep ? aimDir.x : (m_riffleRotationStep * Mathf.Sign(aimDir.x) * Time.deltaTime * Mathf.Sign(m_target.position.z - transform.position.z) * m_turnSpeed);
                    m_RYaw += m_rifleRotation;
                    m_LYaw += m_rifleRotation;
                }
                if (Mathf.Abs(aimDir.y) > 0.02f)
                {
                    m_rifleRotation = Mathf.Abs(aimDir.y) <= m_riffleRotationStep ? aimDir.y : (m_riffleRotationStep * Mathf.Sign(aimDir.y) * Time.deltaTime * m_turnSpeed);
                    m_LPitch += m_rifleRotation;
                    m_RPitch += m_rifleRotation;
                }
                m_TargetR.localRotation = Quaternion.Slerp(m_TargetR.localRotation, Quaternion.Euler(m_TargetR.localRotation.eulerAngles.x, m_RPitch, m_RYaw), Time.deltaTime * m_turnSpeed);
                m_TargetL.localRotation = Quaternion.Slerp(m_TargetL.localRotation, Quaternion.Euler(m_TargetL.localRotation.eulerAngles.x, m_LPitch, m_LYaw), Time.deltaTime * m_turnSpeed);
                //if the rifle is pointinfg at the player -sets shoot target with a deviation
                if (Physics.SphereCast(m_barrelLocation.position, m_detectRadius, m_barrelLocation.forward, out hit, m_shootDist, 1 << m_target.gameObject.layer))
                {
                    m_targetPoint = hit.point;
                    if (Physics.Raycast(m_barrelLocation.position, m_barrelLocation.forward, out hit, m_shootDist, 1 << m_target.gameObject.layer))
                    {
                        m_targetPoint = hit.point + Vector3.right * UnityEngine.Random.Range(-m_detectRadius, m_detectRadius) + Vector3.up * UnityEngine.Random.Range(-m_detectRadius, m_detectRadius);
                    }
                    m_anim.SetTrigger(m_HashShooting);
                }
            }
            else if (Vector3.Angle(m_barrelLocation.forward, (m_target.position + Vector3.up - m_barrelLocation.position).normalized) >= 15f)
            {
                ResetAim();
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
        if (Physics.Raycast(m_barrelLocation.position, m_target.position + Vector3.up - m_barrelLocation.position, m_detectDist, 1 << m_target.gameObject.layer)
            && Vector3.Dot(new Vector3(0f, 0f, m_currentCover.transform.position.z - m_spotCol.transform.position.z), m_target.position + Vector3.up - m_spotCol.transform.position) < 0.2f)
        {
            m_findCoverForward = (m_target.position - m_barrelLocation.position).normalized;
            m_currentAction = FindCover;
            return;
        }

        m_waitTimer += Time.deltaTime;

        if (m_waitTimer >= WaitTime && !DetectPlayerShoot())
        {
            m_waitTimer = 0;
            m_col.center = new(m_col.center.x, m_col.center.y + m_hideOffset, 0f);
            m_col.height += (m_hideOffset + 0.2f);
            m_hide = false;
            m_hideTimer = 0;
            m_currentAction = Attack;
        }
    }
    /// <summary>
    /// The enemy is hit by the player
    /// </summary>
    void Hit()
    {
        m_health -= HitPoint;
        if (m_health <= 0)
        {
            Die();
        }
        else if (m_currentAction == Attack && !m_hide)
        {
            ResetAim();
            m_anim.SetBool(m_HashAiming, false);
            m_col.center = new(m_col.center.x, m_col.center.y - m_hideOffset, 0f);
            m_col.height -= (m_hideOffset + 0.2f);
            m_currentAction = Hide;
            m_hide = true;
        }
        //if an enemy is attacked while walking - find hide spot and go to it
        // if an enemy is hit while walking to hide spot - reset timer
        else if (m_currentAction != Attack)
        {
            m_hideTimer = 0;
            if (!m_hide)
            {
                m_hide = true;
                m_currentAction = FindCover;
            }
        }

        m_anim.SetTrigger(m_HashHit);
    }
    /// <summary>
    /// When enemy is dead, make it an obstacle 
    /// </summary>
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
    }

    public void Shoot()
    {
        m_magazineStore--;
        m_bullet.transform.LookAt(m_targetPoint);
        m_bullet.Play();
    }
    /// <summary>
    /// Playes shoot sound in animation
    /// </summary>
    public void PlayShotSound()
    {
        m_shootSound.Play();
        Debug.Log("Enemy bullet sound plays: " + m_shootSound.isPlaying);

    }
    /// <summary>
    /// Playes step sound in animation
    /// </summary>
    public void PlayStep()
    {
        m_audio.PlayOneShot(m_stepSound);
    }

    //detect bullets
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
            Gizmos.DrawLine(m_barrelLocation.position, m_target.position + Vector3.up * 1.5f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(m_barrelLocation.position, m_barrelLocation.position + m_barrelLocation.forward * m_shootDist);

    }

    private void OnDestroy()
    {
        if (m_spotCol != null)
        {
            Destroy(m_spotCol);
            m_spotCol = null;
        }
    }
}
