using Cinemachine;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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
    Canvas canvas;
    [SerializeField]
    GameObject aimImage;
    [SerializeField]
    LayerMask enemyLayerMask;

    PlayerInput m_input;
    Animator m_anim;
    Rigidbody m_rb;

    //if player is in aiming mode
    bool m_isAiming = false;
    bool m_isCrouched = false;

    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashAiming = Animator.StringToHash("Aiming");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");

    //player speed for run movement
    readonly float m_playerRunSpeed = 5;
    //player speed for walk movment
    readonly float m_playerWalkSpeed = 2;
    //speed for player speed change
    readonly float m_speedChange = 10f;
    //speed for camnera turn change
    readonly float m_cameraTurn = 7f;
    readonly float m_cameraRotationSpeed = 90f;
    //upper border for camera movement
    readonly float m_upperCameraBorder = 60f;
    //upper border for camera movement
    readonly float m_lowerCameraBorder = -25f;
    readonly float m_crouchOffset = 0.25f;

    //current player speed
    float m_currentPlayerSpeed;

    // vertical movement of camera
    float m_cameraPitch;
    //horizontal movement of camera
    float m_cameraYaw;
    //vector of current camera rotation
    Vector3 m_cameraChange;
    Vector3 m_aimTarget;
    // Start is called before the first frame update
    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();
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

        if (m_isAiming)
        {
            // Aim();
            // transform.forward = Vector3.Slerp(transform.forward, new Vector3(cameraTarget.forward.x, 0f, cameraTarget.forward.z), Time.deltaTime * m_cameraTurn);
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
        cameraTarget.rotation = Quaternion.Euler(m_cameraChange);
        aimTarget.localRotation = Quaternion.Euler(m_cameraChange.x, -20f, 0f);
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
            move = m_input.Move.x * cameraTarget.right + m_input.Move.y * cameraTarget.forward;
            move.y = 0f;

            //rotate to face input direction relative to camera position
            transform.forward = Vector3.Slerp(transform.forward, move, Time.fixedDeltaTime * m_cameraTurn);
        }
        else
        {
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
    /// Autoaming
    /// </summary>
    void Aim()
    {
        m_aimTarget = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 30f));
        if (Physics.SphereCast(Camera.main.transform.position, 0.5f, m_aimTarget - Camera.main.transform.position, out RaycastHit hitInfo, 30f, enemyLayerMask))
        {
            Debug.DrawRay(gun.transform.position, hitInfo.point - gun.transform.position, Color.yellow);
            if (Physics.SphereCast(gun.transform.position, 0.5f, hitInfo.point - gun.transform.position, out RaycastHit gunHitInfo, 30f, enemyLayerMask))
            {
                Vector3 targetDir = (gunHitInfo.transform.position - transform.position).normalized;
                Quaternion playerRot = Quaternion.LookRotation(new Vector3(targetDir.x, 0f, targetDir.z));
                targetDir = (gunHitInfo.transform.position - aimTarget.transform.position).normalized;
                Quaternion aimRot = Quaternion.LookRotation(new Vector3(0f, targetDir.y, 0f));
                transform.rotation = Quaternion.Lerp(transform.rotation, playerRot, Time.fixedDeltaTime * 5f);
                aimTarget.localRotation = Quaternion.Lerp(aimTarget.localRotation, aimRot, Time.fixedDeltaTime * 5f);
                m_aimTarget = gunHitInfo.point;
                Debug.DrawLine(transform.position, m_aimTarget, Color.magenta);
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
                // aimImage.transform.position = Vector3.zero;
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

    void Die()
    {

    }
    //Detects collisions with character controller
    void OnTriggerEnter(Collider other)
    {

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
       // Gizmos.DrawLine(gun.transform.position, m_aimTarget);
        Gizmos.DrawLine(Camera.main.transform.position, m_aimTarget);

    }

}
