using Cinemachine;
using Cinemachine.PostFX;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ShooterPlayerController : MonoBehaviour
{
    [Header("Objects and transforms")]

    [SerializeField]
    Transform aim;
    [SerializeField]
    ShootScript gun;
    [SerializeField]
    CinemachineVirtualCamera aimCamera;
    //transform to rotate moveCamera
    [SerializeField]
    Transform moveCameraTarget;
    [SerializeField]
    //transform to rotate aimCamera
    Transform aimCameraTarget;
    [SerializeField]
    GameObject aimImage;
    [SerializeField]
    Slider healthBar;
    [SerializeField]
    VolumeProfile damageVolume;

    PlayerInput m_input;
    Animator m_anim;
    Rigidbody m_rb;
    CapsuleCollider m_col;
    Rig m_aimRig;
    Rig m_armsRig;
    Transform m_pistolPos;
    Vignette m_damageVignette;

    bool m_dead = false;
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
    readonly float m_aimPitchOffset = -10f;
    //upper border for the camera movement
    readonly float m_upperCameraBorder = 60f;
    //upper border for the camera movement
    readonly float m_lowerCameraBorder = -25f;
    //height of the camera offset when the player crouches
    readonly float m_crouchOffset = 0.35f;
    //time to recover from a hit
    readonly float m_recoverTime = 3f;
    readonly float m_vignetteMax = 0.64f;
    readonly float m_vignetteMin = 0.46f;

    Vector3 m_baseAim;
    Vector3 m_basePistolPos;
    Quaternion m_basePistolRot;

    float m_armsRigWeight = 1f;
    float m_aimRigWeight = 0f;
    //current player speed
    float m_currentPlayerSpeed;
    float m_recoverWaitTime;
    // vertical movement of camera
    float m_gunPitch;
    //horizontal movement of camera
    float m_gunYaw;
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
        Transform aimRig = transform.Find("AimRig");
        m_armsRig = transform.Find("ArmsRig").GetComponent<Rig>();
        m_armsRigWeight = m_armsRig.weight = 1;
        m_pistolPos = aimRig.GetChild(0);
        m_aimRig = aimRig.GetComponent<Rig>();
        m_baseAim = transform.Find("Aim").localPosition;
        m_basePistolPos = m_pistolPos.localPosition;
        m_basePistolRot = m_pistolPos.localRotation;
        m_anim = GetComponent<Animator>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();
        m_col = GetComponent<CapsuleCollider>();
        m_damageVignette = (Vignette)damageVolume.components[0];

        healthBar.gameObject.SetActive(true);
        healthBar.maxValue = m_health;
        healthBar.value = m_health;
    }

    /// <summary>
    /// Update player physics and animations
    /// </summary>
    void FixedUpdate()
    {
        if (!m_dead)
        {
            Aim(m_input.Aim);

            if (m_input.Fire)
            {
                Fire();
                m_input.SetFireDone();
            }

            Move();
        }
    }
    /// <summary>
    /// Rotate camera and player in aim mode
    /// </summary>
    void LateUpdate()
    {
        if (!m_isAiming || (m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Cover To Stand"&&!m_anim.IsInTransition(0)))
        {
            m_armsRig.weight = Mathf.Lerp(m_armsRig.weight, m_armsRigWeight, Time.deltaTime * m_cameraTurn);
            m_aimRig.weight = Mathf.Lerp(m_aimRig.weight, m_aimRigWeight, Time.deltaTime * m_cameraTurn);
        }

        MoveCamera();

        if (!m_dead)
        {
            if (m_health < healthBar.maxValue)
            {
                m_recoverWaitTime += Time.deltaTime;
                if (m_recoverWaitTime >= m_recoverTime)
                {
                    m_recoverWaitTime = 0;
                    AddHealth(m_hit);
                }

                if (m_health < healthBar.maxValue / 2)
                {
                    if (Mathf.Approximately(m_damageVignette.intensity.value, m_vignetteMin)|| m_damageVignette.intensity.value< m_vignetteMin)
                    {
                        m_damageVignette.intensity.value = Mathf.Lerp(m_damageVignette.intensity.value, m_vignetteMax, Time.deltaTime);
                    }
                    else if (Mathf.Approximately(m_damageVignette.intensity.value, float.MaxValue))
                    {
                        m_damageVignette.intensity.value = Mathf.Lerp(m_damageVignette.intensity.value, m_vignetteMin, Time.deltaTime);
                    }
                }
                else
                {
                    m_damageVignette.intensity.value = Mathf.Lerp(m_damageVignette.intensity.value, 0f, Time.deltaTime);
                }
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

        if (m_isAiming && (!m_isCrouched || !m_isAiming || m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Idle"))
        {
            Vector3 aimDir = m_aimTarget - gun.BarrelLocation.position - gun.BarrelLocation.forward * (m_aimTarget - gun.BarrelLocation.position).magnitude;

            if (Mathf.Abs(aimDir.x) > 0.02f)
            {
                m_gunYaw += (Mathf.Abs(aimDir.x) <= 0.25f ? aimDir.x : (0.25f * Mathf.Sign(aimDir.x))) * Time.deltaTime * Mathf.Sign(m_aimTarget.z - transform.position.z);
            }
            if (Mathf.Abs(aimDir.y) > 0.02f)
            {
                m_gunPitch += (Mathf.Abs(aimDir.y) <= 0.25f ? aimDir.y : (0.25f * Mathf.Sign(aimDir.y))) * Time.deltaTime;
            }
            aim.localPosition = Vector3.Slerp(aim.localPosition, new Vector3(m_gunYaw, m_gunPitch, aim.localPosition.z), Time.deltaTime * m_cameraTurn);
        }
    }
    /// <summary>
    /// Moves player and sets animator's parameters
    /// </summary>
    void Move()
    {
        float nextSpeed = m_input.Run ? m_playerRunSpeed : m_playerWalkSpeed;
        if (m_input.Move == Vector2.zero)
            nextSpeed = 0f;
        //smoothly change player speed
        m_currentPlayerSpeed = Mathf.Lerp(m_currentPlayerSpeed, nextSpeed, Time.fixedDeltaTime * m_speedChange);
        if (m_currentPlayerSpeed < 0.01f)
            m_currentPlayerSpeed = 0f;

        //player move vector based on input
        if (m_isAiming)
        {
            if (!m_isCrouched)
            {
                transform.forward = Vector3.Slerp(transform.forward, new Vector3(moveCameraTarget.forward.x, 0f, moveCameraTarget.forward.z), Time.fixedDeltaTime * m_cameraTurn);
            }
            Aim();
        }
        else if (!m_isAiming && !m_isCrouched)
        {
            //moves player according to camera forward vector
            Vector3 move = m_input.Move.x * moveCameraTarget.right + m_input.Move.y * moveCameraTarget.forward;
            move.y = 0f;

            //rotate to face input direction relative to camera position
            transform.forward = Vector3.Slerp(transform.forward, move, Time.fixedDeltaTime * m_cameraTurn);
        }

        if (m_isCrouched != m_input.Crouch)
        {
            if (m_input.Crouch && Physics.Raycast(transform.position, transform.forward, 2f, 1 << LayerMask.NameToLayer("Cover")) || !m_input.Crouch)
            {
                m_isCrouched = m_input.Crouch;
               if(!m_isAiming)
                    SetCameraCrouchOffset(m_isCrouched);
            }
            else if (m_input.Crouch)
            {
                m_input.DisableCrouch();
            }
        }
        else if (m_isCrouched &&
            (m_input.Move.y < -0.5f ||
           !Physics.Raycast(m_col.bounds.center, transform.forward, 3f, 1 << LayerMask.NameToLayer("Cover"))) )
        //   || (!Physics.Raycast(new(m_col.bounds.max.x, m_col.bounds.center.y, m_col.bounds.center.z), transform.forward, 4f, 1 << LayerMask.NameToLayer("Cover")) &&
        //    !Physics.Raycast(new(m_col.bounds.min.x, m_col.bounds.center.y, m_col.bounds.center.z), transform.forward, 4f, 1 << LayerMask.NameToLayer("Cover")))))
        {
            m_isCrouched = false;
            if (!m_isAiming)
                SetCameraCrouchOffset(m_isCrouched);
            m_input.DisableCrouch();
        }

        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x);
        m_anim.SetFloat(m_HashVertical, m_input.Move.y);
        m_anim.SetBool(m_HashCrouching, m_isCrouched);
        m_anim.SetFloat(m_HashSpeed, m_currentPlayerSpeed);
    }

    void SetCameraCrouchOffset(bool isCrouch)
    {
        Vector3 offset = new(0f, aimCameraTarget.localPosition.y + (isCrouch ? -1 : 1) * m_crouchOffset, 0f);
        m_col.center = new(m_col.center.x, m_col.center.y + (isCrouch ? -1 : 1) * m_crouchOffset, 0f);
        m_col.height += (isCrouch ? -1 : 1) * (m_crouchOffset + 0.05f);
        aimCameraTarget.localPosition = moveCameraTarget.localPosition = offset;
    }

    private void OnAnimatorMove()
    {
        //player move vector based on input
        Vector3 move = Vector3.zero;
        if (m_isAiming || m_isCrouched)
        {
            //moves player according to aim camera forward vector
            move = m_input.Move.x * aimCameraTarget.right + m_input.Move.y * aimCameraTarget.forward;
        }
        else if (!m_isAiming && !m_isCrouched)
        {
            //moves player according to camera forward vector
            move = m_input.Move.x * moveCameraTarget.right + m_input.Move.y * moveCameraTarget.forward;
        }

        move.y = 0f;
       // transform.position += move * m_anim.deltaPosition.magnitude;
        m_rb.MovePosition(m_rb.position + move * m_anim.deltaPosition.magnitude);
        //m_rb.MoveRotation(m_anim.deltaRotation);
    }

    /// <summary>
    /// Casts a sphere from the center of the screen to detect an enemy
    /// If an enemy is detected, cast another sphere from gun to hitpoint to make sure that bullet wouldn't have obstacles
    /// If bullet's path is free, rotates player and camera towards the hitpoint
    /// </summary>
    void Aim()
    {
        m_aimTarget = Camera.main.transform.position + Camera.main.transform.forward * m_aimDistance;

        if (Physics.SphereCast(Camera.main.transform.position, 0.5f, Camera.main.transform.forward, out RaycastHit hitInfo, m_aimDistance, 1 << LayerMask.NameToLayer("Enemy")))
        {
            if (Physics.SphereCast(gun.BarrelLocation.position, 0.2f, hitInfo.point - gun.BarrelLocation.position, out RaycastHit gunHitInfo, m_aimDistance, 1 << LayerMask.NameToLayer("Enemy")))
            {
                var rotateDir = Vector3.RotateTowards(Camera.main.transform.forward, new Vector3(hitInfo.collider.transform.position.x, hitInfo.point.y, hitInfo.point.z) - Camera.main.transform.position, Time.fixedDeltaTime * m_cameraTurn, 0f).normalized;
                if (Vector3.Angle(Camera.main.transform.forward, rotateDir) > 5f)
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
            aimImage.SetActive(isAiming);
            if (m_isCrouched)
            {
                SetCameraCrouchOffset(!isAiming);
            }
            if (isAiming)
            {
                aim.localPosition = m_baseAim;
                m_gunPitch = m_baseAim.y;
                m_gunYaw = m_baseAim.x;

                m_pistolPos.SetLocalPositionAndRotation(m_basePistolPos, m_basePistolRot);

                m_aimRigWeight = 1f;
                m_cameraPitch = 0f;
                aimCamera.Priority = 11;
            }
            else
            {
                m_aimRigWeight = 0f;
                aimCamera.Priority = 9;
            }
            m_isAiming = isAiming;
        }
    }
    /// <summary>
    /// Start shooting
    /// </summary>
    void Fire()
    {
        m_anim.SetTrigger(m_HashShooting);
        gun.Fire(m_aimTarget);
    }
    /// <summary>
    /// Takes the damage from enemy's bullet
    /// </summary>
    void Hit()
    {
        AddHealth(-m_hit);
        if (m_health <= 0)
        {
            m_dead = true;
            m_armsRigWeight = 0f;
            m_anim.SetTrigger(m_HashDie);
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

    //Detects bullets
    private void OnParticleCollision(GameObject other)
    {
        Hit();
        Debug.Log("PLAYER HIT");
    }

    private void OnDisable()
    {
        aimImage.transform.parent.gameObject.SetActive(false);
        gun.transform.parent.gameObject.SetActive(false);
        GetComponent<RigBuilder>().enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(gun.BarrelLocation.position, m_aimTarget);
    }
}
