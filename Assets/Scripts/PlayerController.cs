using Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    CinemachineVirtualCamera moveCamera;
    [SerializeField]
    CinemachineVirtualCamera aimCamera;
    //transform to rotate moveCamera
    [SerializeField]
    Transform cameraTarget;
    [SerializeField]
    Image aimImage;

    PlayerInput m_input;
    Animator m_anim;
    CharacterController m_ccontroller;

    //if player is in aiming mode
    bool m_isAiming = false;

    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashShooting = Animator.StringToHash("Shooting");
    readonly int m_HashSpeed = Animator.StringToHash("Speed");
    readonly int m_HashVertical = Animator.StringToHash("Vertical");
    readonly int m_HashHorizontal = Animator.StringToHash("Horizontal");

    //player speed for run movement
    readonly float m_playerRunSpeed = 5f;
    //player speed for walk movment
    readonly float m_playerWalkSpeed = 2f;
    //speed for player rotation
    readonly float m_playerTurn = 85f;
    //speed for player speed change
    readonly float m_speedChange = 10f;
    //speed for camnera turn change
    readonly float m_cameraTurn = 6f;
    //upper border for camera movement
    readonly float m_upperCameraBorder = 60f;
    //upper border for camera movement
    readonly float m_lowerCameraBorder = -30f;

    //current player speed
    float m_currentPlayerSpeed;

    // vertical movement of camera
    float m_cameraPitch;
    //horizontal movement of camera
    float m_cameraYaw;
    //vector of current camera rotation
    Vector3 m_cameraChange;

    // Start is called before the first frame update
    void Start()
    {
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_ccontroller = GetComponent<CharacterController>();
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

        Rotate(!m_isAiming && !Mathf.Approximately(m_input.Move.y, 0f), Time.fixedDeltaTime);
        Move();
    }
    /// <summary>
    /// Rotate camera and player in aim mode
    /// </summary>
    void LateUpdate()
    {
        MoveCamera();
        Rotate(m_isAiming, Time.deltaTime);
    }
    /// <summary>
    /// Rotate player based on condition
    /// </summary>
    /// <param name="condition">when to rotate player</param>
    /// <param name="deltaTime">delta time (depends on update type)</param>
    void Rotate(bool condition, float deltaTime)
    {
        if (!Mathf.Approximately(m_input.Look.sqrMagnitude, 0))
        {
            if (condition)
            {
                transform.Rotate(transform.up * m_input.Look.x, deltaTime * m_playerTurn);
            }
        }
    }
    /// <summary>
    /// Rotate camera around player (changes player's child's transform rotation)
    /// </summary>
    void MoveCamera()
    {
        m_cameraPitch += m_input.Look.y * Time.deltaTime;
        m_cameraYaw += m_input.Look.x * Time.deltaTime;

        //borders for vertical movement
        m_cameraPitch = Mathf.Clamp(m_cameraPitch, -m_lowerCameraBorder, m_upperCameraBorder);
        //smoothly rotate transform
        m_cameraChange = Vector3.Lerp(m_cameraChange, new Vector3(m_cameraPitch, m_cameraYaw, 0f), Time.deltaTime * m_cameraTurn);
        cameraTarget.rotation = Quaternion.Euler(m_cameraChange);
    }
    /// <summary>
    /// Moves player and sets animator's parameters
    /// </summary>
    void Move()
    {
        m_anim.SetBool(m_HashCrouching, m_input.Crouch);
        //smoothly change player speed
        m_currentPlayerSpeed = Mathf.Lerp(m_currentPlayerSpeed, m_input.Run ? m_playerRunSpeed : m_playerWalkSpeed, Time.fixedDeltaTime * m_speedChange);
        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x * (m_currentPlayerSpeed / m_playerRunSpeed));
        m_anim.SetFloat(m_HashVertical, m_input.Move.y * (m_currentPlayerSpeed / m_playerRunSpeed));
        m_anim.SetFloat(m_HashSpeed, m_currentPlayerSpeed);
        //player move vector based on input
        Vector3 move = m_input.Move.x * transform.right + m_input.Move.y * transform.forward;
        m_ccontroller.Move(m_currentPlayerSpeed * Time.fixedDeltaTime * move + Physics.gravity);
    }
    /// <summary>
    /// Autoaming
    /// </summary>
    void Aim()
    {

    }
    /// <summary>
    /// Check if to start or stop aiming
    /// </summary>
    /// <param name="isAiming">if we are aiming at the moment</param>
    void Aim(bool isAiming)
    {
        if (m_isAiming != isAiming)
        {
            m_isAiming = isAiming;
            aimImage.enabled = m_isAiming;
            if (m_isAiming)
            {
                aimCamera.Priority = 11;
                Aim();
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
    }

    void Die()
    {

    }
    //Detects collisions with character controller
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("bullet"))
        {

        }
    }
}
