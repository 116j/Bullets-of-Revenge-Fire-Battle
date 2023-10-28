using Cinemachine;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

public class ShooterPlayerController : MonoBehaviour
{
    [Header("Objects and transforms")]
    [SerializeField]
    ShootScript gun;
    [SerializeField]
    CinemachineVirtualCamera aimCamera;
    //transform to rotate moveCamera
    [SerializeField]
    Transform cameraTarget;
    [SerializeField]
    Transform aimTarget;
    [SerializeField]
    GameObject aimImage;
    [SerializeField]
    Slider healthBar;
    [SerializeField]
    LayerMask enemyLayerMask;

    PlayerInput m_input;
    Animator m_anim;
    Rigidbody m_rb;

    //if player is in aiming mode
    bool m_isAiming = false;
    //if player is crouching
    bool m_isCrouched = false;
    //health points left
    float m_health = 10;
    //a point for hit
    float m_hit = 0.8f;

    //hashes for animator parameters
    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashAiming = Animator.StringToHash("Aiming");
    readonly int m_HashDie = Animator.StringToHash("Die");
    readonly int m_HashHit = Animator.StringToHash("Hit");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");

    //a player speed for the run movement
    readonly float m_playerRunSpeed = 5;
    //a player speed for the walk movement
    readonly float m_playerWalkSpeed = 2.5f;
    //a speed for the player speed change
    readonly float m_speedChange = 10f;
    //a speed for the camera turn change
    readonly float m_cameraTurn = 7f;
    //a speed of the camera rotation
    readonly float m_cameraRotationSpeed = 90f;
    //the distance of the aim traget
    readonly float m_aimDistance = 30f;
    //a value of an y-axis rotation in aim camera
    readonly float m_aimPitchOffset = -20f;
    //upper border for the camera movement
    readonly float m_upperCameraBorder = 60f;
    //upper border for the camera movement
    readonly float m_lowerCameraBorder = -25f;
    //height of the camera offset when the player crouches
    readonly float m_crouchOffset = 0.25f;
    //time to recover from a hit
    readonly float m_recoverTime = 3f;

    //current player speed
    float m_currentPlayerSpeed;
    float m_recoverWaitTime;

    // vertical movement of camera
    float m_cameraPitch;
    //horizontal movement of camera
    float m_cameraYaw;
    //vector of move camera rotation
    Vector3 m_cameraChange;
    Vector3 m_aimChange;
    //vector of aim camera rotation
    Vector3 m_aimTarget;

    // Start is called before the first frame update
    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();

        healthBar.maxValue = m_health;
        healthBar.value = m_health;
    }

    /// <summary>
    /// Update player physics and animations
    /// </summary>
    void FixedUpdate()
    {
        Aim(m_input.Aim);

        if (m_input.Fire)
        {
            Fire();
            m_input.SetFireDone();
        }

        Move();
    }
    /// <summary>
    /// Rotate camera and player in aim mode
    /// </summary>
    void LateUpdate()
    {
        MoveCamera();

        if (m_health < healthBar.maxValue)
        {
            m_recoverWaitTime += Time.deltaTime;
            if (m_recoverWaitTime >= m_recoverTime)
            {
                m_recoverWaitTime = 0;
                AddHealth(m_hit);
            }
        }
    }

    /// <summary>
    /// Rotate camera around player (changes player's child's transform rotation)
    /// </summary>
    void MoveCamera()
    {
        m_cameraPitch += (m_input.Look.y - m_aimChange.y) * Time.deltaTime * m_cameraRotationSpeed;
        m_cameraYaw += (m_aimChange.x + m_input.Look.x) * Time.deltaTime * m_cameraRotationSpeed;

        m_aimChange.x -= m_aimChange.x * Time.deltaTime * m_cameraRotationSpeed;
        m_aimChange.y -= m_aimChange.y * Time.deltaTime * m_cameraRotationSpeed;

        if (Mathf.Approximately(m_aimChange.x, 0f))
        {
            m_aimChange.x = 0f;
        }
        if (Mathf.Approximately(m_aimChange.y, 0f))
        {
            m_aimChange.y = 0f;
        }
       
        //borders for vertical movement
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, m_lowerCameraBorder, m_upperCameraBorder);
        //smoothly rotate transform
         m_cameraChange = Vector3.Slerp(m_cameraChange, new Vector3(m_cameraPitch, m_cameraYaw, 0f), Time.deltaTime * m_cameraTurn);
        cameraTarget.rotation = Quaternion.Euler(m_cameraChange);
        aimTarget.localRotation = Quaternion.Euler(m_cameraChange.x, m_aimPitchOffset, 0f);
    }
    /// <summary>
    /// Moves player and sets animator's parameters
    /// </summary>
    void Move()
    {
        //player move vector based on input
        Vector3 move;
        float nextSpeed = m_input.Run ? m_playerRunSpeed : m_playerWalkSpeed;
        if (!m_isAiming)
        {
            //moves player according to camera forward vector
            move = m_input.Move.x * cameraTarget.right + m_input.Move.y * cameraTarget.forward;
            move.y = 0f;

            //rotate to face input direction relative to camera position
            transform.forward = Vector3.Slerp(transform.forward, move, Time.fixedDeltaTime * m_cameraTurn);
        }
        else
        {
            //moves player according to aim camera forward vector
            move = m_input.Move.x * aimTarget.right + m_input.Move.y * aimTarget.forward;
            transform.forward = Vector3.Slerp(transform.forward, new Vector3(cameraTarget.forward.x, 0f, cameraTarget.forward.z), Time.fixedDeltaTime * m_cameraTurn);
            Aim();
        }
        move.y = 0f;
        m_rb.velocity = move * m_currentPlayerSpeed;

        if (m_input.Move == Vector2.zero)
            nextSpeed = 0f;
        //smoothly change player speed
        m_currentPlayerSpeed = Mathf.Lerp(m_currentPlayerSpeed, nextSpeed, Time.fixedDeltaTime * m_speedChange);
        if (m_currentPlayerSpeed < 0.01f)
            m_currentPlayerSpeed = 0f;

        if (m_isCrouched != m_input.Crouch)
        {
            m_isCrouched = m_input.Crouch;
            m_anim.SetBool(m_HashCrouching, m_isCrouched);
            Vector3 offset = new(0f, aimTarget.localPosition.y + (m_isCrouched ? -1 : 1) * m_crouchOffset, 0f);
            aimTarget.localPosition = cameraTarget.localPosition = offset;
        }
        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x * (m_currentPlayerSpeed / m_playerRunSpeed));
        m_anim.SetFloat(m_HashVertical, m_input.Move.y * (m_currentPlayerSpeed / m_playerRunSpeed));
        m_anim.SetFloat(m_HashSpeed, m_currentPlayerSpeed);
    }

    /// <summary>
    /// Casts a sphere from the center of the screen to detect an enemy
    /// If an enemy is detected, cast another sphere from gun to hitpoint to make sure that bullet wouldn't have obstacles
    /// If bullet's path is free, rotates player and camera towards the hitpoint
    /// </summary>
    void Aim()
    {
        m_aimTarget = Camera.main.transform.position + Camera.main.transform.forward * m_aimDistance;
        if (Physics.Raycast(Camera.main.transform.position, m_aimTarget - Camera.main.transform.position, out RaycastHit camInfo, enemyLayerMask))
        {
            m_aimTarget = camInfo.point;
            m_aimChange = Vector3.zero;
        }
        else if (Physics.SphereCast(Camera.main.transform.position, 0.5f, m_aimTarget - Camera.main.transform.position, out RaycastHit hitInfo, m_aimDistance, enemyLayerMask))
        {
            if (Physics.SphereCast(gun.transform.position,0.2f, hitInfo.point - gun.transform.position, out RaycastHit gunHitInfo, m_aimDistance, enemyLayerMask))
            {
                    m_aimChange = hitInfo.point - m_aimTarget;
                    m_aimChange.x = hitInfo.collider.transform.position.x - m_aimTarget.x;
                    Debug.Log(" HT  " + hitInfo.point);
                    Debug.DrawRay(Camera.main.transform.position, m_aimChange + m_aimTarget - Camera.main.transform.position, Color.green);
                    m_aimChange.Normalize();

                    m_aimTarget = gunHitInfo.point;
                    m_aimTarget.x = gunHitInfo.collider.transform.position.x;
            }
        }
    }
    /// <summary>
    /// Check if to start or stop aiming
    /// </summary>
    /// <param name="isAiming">if we are aiming at the moment</param>
    void Aim(bool isAiming)
    {
        if (m_isAiming != isAiming)
        {
            m_anim.SetBool(m_HashAiming, isAiming);
            m_isAiming = isAiming;
            aimImage.SetActive(m_isAiming);
            if (m_isAiming)
            {
                aimCamera.Priority = 11;
            }
            else
            {
                aimCamera.Priority = 9;
            }
        }
    }
    /// <summary>
    /// Start shooting
    /// </summary>
    void Fire()
    {
        m_anim.SetTrigger(m_HashShooting);
        gun.Fire(m_isAiming ? m_aimTarget : Vector3.zero);
    }
    /// <summary>
    /// Takes the damage from enemy's bullet
    /// </summary>
    void Hit()
    {
        AddHealth(-m_hit);
        if (m_health == 0)
        {
            Die();
        }
        else
        {
            m_anim.SetTrigger(m_HashHit);
        }
    }

    void AddHealth(float value)
    {
        m_health += value;
        if (m_health < 0)
        {
            m_health = 0;
        }
        healthBar.value = m_health;
    }
    /// <summary>
    /// The player dies
    /// </summary>
    void Die()
    {
        m_anim.SetTrigger(m_HashDie);
    }

    //Detects bullets
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("bullet"))
        {
            Hit();
        }
    }

}
