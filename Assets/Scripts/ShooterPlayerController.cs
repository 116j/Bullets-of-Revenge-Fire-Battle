using Cinemachine;
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
    Transform moveCameraTarget;
    [SerializeField]
    Transform aimCameraTarget;
    [SerializeField]
    GameObject aimImage;
    [SerializeField]
    Slider healthBar;

    PlayerInput m_input;
    Animator m_anim;
    Rigidbody m_rb;

    //if player is in aiming mode
    bool m_isAiming = false;
    //if player is crouching
    bool m_isCrouched = false;
    //health points left
    float m_health = 10;

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
    //a point for hit
    readonly float m_hit = 0.8f;
    //a speed for the camera turn change
    readonly float m_cameraTurn = 7f;
    //a speed of the camera rotation
    readonly float m_cameraRotationSpeed = 80f;
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
        m_cameraPitch += m_input.Look.y * Time.deltaTime * m_cameraRotationSpeed;
        m_cameraYaw += m_input.Look.x * Time.deltaTime * m_cameraRotationSpeed;

        //borders for vertical movement
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, m_lowerCameraBorder, m_upperCameraBorder);
        //smoothly rotate transform
        m_cameraChange = Vector3.Slerp(m_cameraChange, new Vector3(m_cameraPitch, m_cameraYaw, 0f), Time.deltaTime * m_cameraTurn);
        moveCameraTarget.rotation = Quaternion.Euler(m_cameraChange);
        aimCameraTarget.rotation = Quaternion.Euler(m_cameraChange + Vector3.up * m_aimPitchOffset);
        //aimCameraTarget.localRotation = Quaternion.Euler(m_cameraChange.x,m_aimPitchOffset,0f);
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
            move = m_input.Move.x * moveCameraTarget.right + m_input.Move.y * moveCameraTarget.forward;
            move.y = 0f;

            //rotate to face input direction relative to camera position
            transform.forward = Vector3.Slerp(transform.forward, move, Time.fixedDeltaTime * m_cameraTurn);
        }
        else
        {
            //moves player according to aim camera forward vector
            move = m_input.Move.x * aimCameraTarget.right + m_input.Move.y * aimCameraTarget.forward;
            transform.forward = Vector3.Slerp(transform.forward, new Vector3(moveCameraTarget.forward.x, 0f, moveCameraTarget.forward.z), Time.fixedDeltaTime * m_cameraTurn);
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
            Vector3 offset = new(0f, aimCameraTarget.localPosition.y + (m_isCrouched ? -1 : 1) * m_crouchOffset, 0f);
            aimCameraTarget.localPosition = moveCameraTarget.localPosition = offset;
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

        if (Physics.SphereCast(Camera.main.transform.position, 0.8f, Camera.main.transform.forward, out RaycastHit hitInfo, m_aimDistance, 1 << LayerMask.NameToLayer("Enemy")))
        {
            if (Physics.SphereCast(gun.transform.position, 0.4f, hitInfo.point - gun.transform.position, out RaycastHit gunHitInfo, m_aimDistance, 1 << LayerMask.NameToLayer("Enemy")))
            {
                var rotateDir = Vector3.RotateTowards(Camera.main.transform.forward, new Vector3(hitInfo.collider.transform.position.x, hitInfo.point.y, hitInfo.point.z) - Camera.main.transform.position, Time.fixedDeltaTime * m_cameraTurn, 0f).normalized;
                if (Vector3.Dot(Camera.main.transform.forward, rotateDir) < 0.99f)
                {
                    m_cameraPitch += rotateDir.y * Time.fixedDeltaTime * m_cameraRotationSpeed;
                    m_cameraYaw += rotateDir.x * Time.fixedDeltaTime * m_cameraRotationSpeed;
                   // m_cameraChange = Vector3.Slerp(m_cameraChange, new Vector3(rotateDir.x, rotateDir.y, 0f), Time.fixedDeltaTime * m_cameraTurn);
                }
                m_aimTarget = gunHitInfo.point;
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
    private void OnParticleCollision(GameObject other)
    {
        // Hit();
        // Debug.Log("HIT");
    }
}
