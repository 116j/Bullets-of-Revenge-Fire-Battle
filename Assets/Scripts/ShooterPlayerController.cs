using Cinemachine;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class ShooterPlayerController : MonoBehaviour
{
    [Header("Objects and transforms")]

    [SerializeField]
    Transform m_startPosition;
    [SerializeField]
    //left arm rig target
    Transform m_TargetL;
    [SerializeField]
    //right arm rig target
    Transform m_TargetR;
    [SerializeField]
    ShootScript m_gun;
    [SerializeField]
    CinemachineVirtualCamera m_aimCamera;
    //transform to rotate moveCamera
    [SerializeField]
    Transform m_moveCameraTarget;
    [SerializeField]
    //transform to rotate aimCamera
    Transform m_aimCameraTarget;
    [SerializeField]
    GameObject m_aimImage;
    [SerializeField]
    Slider m_healthBar;
    [SerializeField]
    VolumeProfile m_damageVolume;
    [SerializeField]
    AudioSource m_voice;
    [SerializeField]
    AudioClip m_crouchStep;
    [SerializeField]
    AudioClip[] m_stepSounds;
    [SerializeField]
    AudioClip[] m_hitSounds;

    PlayerInput m_input;
    Animator m_anim;
    AudioSource m_steps;
    Rigidbody m_rb;
    CapsuleCollider m_col;
    Vignette m_damageVignette;

    bool m_dead = false;
    //if player is in aiming mode
    bool m_isAiming = false;
    //if player is crouching
    bool m_isCrouched = false;
    bool m_aimCrouched = false;
    //health points left
    float m_health = 10;

    //hashes for animator parameters
    readonly int m_HashCrouching = Animator.StringToHash("Crouching");
    readonly int m_HashAiming = Animator.StringToHash("Aiming");
    readonly int m_HashAimCrouch = Animator.StringToHash("AimCrouch");
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
    readonly float m_crouchOffset = 0.4f;
    //max step per frame for gun rig movement
    readonly float m_gunRotationStep = 0.5f;
    readonly float m_vignetteMax = 0.8f;
    readonly float m_vignetteMin = 0.48f;

    //start gun rig rotations
    Quaternion m_baseTR;
    Quaternion m_baseTL;

    //current player speed
    float m_currentPlayerSpeed;
    //timer for health recovery
    float m_recoverWaitTime;
    // vertical movement of left arm rig
    float m_LPitch;
    //horizontal movement of left arm rig
    float m_LYaw;
    // vertical movement of right arm rig
    float m_RPitch;
    //horizontal movement of right arm rig
    float m_RYaw;
    // vertical movement of camera
    float m_cameraPitch;
    //horizontal movement of camera
    float m_cameraYaw;
    //vector of move camera rotation
    Vector3 m_cameraChange;
    //vector of aim camera rotation
    Vector3 m_aimTarget;

    //step per frame for gun rig movement
    float m_gunRotation;

    //a point for hit
    float HitPoint => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 0.6f : 1f;
    //time to recover from a hit
    float RecoverTime => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 1.5f : 3f;

    // Start is called before the first frame update
    void Start()
    {
        m_baseTL = m_TargetL.localRotation;
        m_baseTR = m_TargetR.localRotation;
        m_anim = GetComponent<Animator>();
        m_steps = GetComponent<AudioSource>();
        m_input = GetComponent<PlayerInput>();
        m_rb = GetComponent<Rigidbody>();
        m_col = GetComponent<CapsuleCollider>();
        m_damageVignette = (Vignette)m_damageVolume.components[0];

        m_healthBar.maxValue = m_health;
        Reset();
        // m_input.LockInput();
    }

    /// <summary>
    /// Update player physics and animations
    /// </summary>
    void FixedUpdate()
    {
        m_anim.SetBool(m_HashDie, m_dead);
        if (!m_dead)
        {
            Aim(m_input.Aim);

            if (m_input.Fire)
            {
                if (m_anim.GetCurrentAnimatorClipInfo(1)[0].clip.name != "Hit" ||
                    m_isCrouched && !m_aimCrouched && m_anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "Aiming")
                {
                    Fire();
                    m_input.SetFireDone();
                }
            }

            Move();
        }
    }
    /// <summary>
    /// Rotate camera and player in aim mode
    /// </summary>
    void LateUpdate()
    {
        MoveCamera();

        if (!m_dead)
        {
            if (m_health < m_healthBar.maxValue)
            {
                //recover health
                m_recoverWaitTime += Time.deltaTime;
                if (m_recoverWaitTime >= RecoverTime)
                {
                    m_recoverWaitTime = 0;
                    AddHealth(HitPoint);
                }

                // if health is less than 50%, show red pulsing vignette
                if (m_health < m_healthBar.maxValue / 2)
                {
                    if (Mathf.Approximately(m_damageVignette.intensity.value, m_vignetteMin) || m_damageVignette.intensity.value < m_vignetteMin)
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
        m_moveCameraTarget.rotation = Quaternion.Euler(m_cameraChange);
        m_aimCameraTarget.rotation = Quaternion.Euler(m_cameraChange + Vector3.up * m_aimPitchOffset);

        if (m_isAiming)
        {
            //direction of aim
            Vector3 aimDir = m_gun.BarrelLocation.position + m_gun.BarrelLocation.forward * (m_aimTarget - m_gun.BarrelLocation.position).magnitude - m_aimTarget;

            //calculate offset
            if (Mathf.Abs(aimDir.x) > 0.02f)
            {
                m_gunRotation = Mathf.Abs(aimDir.x) <= m_gunRotationStep ? aimDir.x : (m_gunRotationStep * Mathf.Sign(aimDir.x) * Time.deltaTime * Mathf.Sign(m_aimTarget.z - transform.position.z) * m_cameraRotationSpeed);
                m_LYaw += m_gunRotation;
                m_RYaw += m_gunRotation;
            }
            if (Mathf.Abs(aimDir.y) > 0.02f)
            {
                m_gunRotation = Mathf.Abs(aimDir.y) <= m_gunRotationStep ? aimDir.y : (m_gunRotationStep * Mathf.Sign(aimDir.y) * Time.deltaTime * m_cameraRotationSpeed);
                m_LPitch += m_gunRotation;
                m_RPitch += m_gunRotation;
            }
            //move gun rigs
            m_TargetR.localRotation = Quaternion.Slerp(m_TargetR.localRotation, Quaternion.Euler(m_RPitch, m_TargetR.localRotation.eulerAngles.y, m_RYaw), Time.deltaTime * m_cameraRotationSpeed);
            m_TargetL.localRotation = Quaternion.Slerp(m_TargetL.localRotation, Quaternion.Euler(m_LPitch, m_TargetL.localRotation.eulerAngles.y, m_LYaw), Time.deltaTime * m_cameraRotationSpeed);
            if (Vector3.Angle(m_gun.BarrelLocation.forward, (m_aimTarget - m_gun.BarrelLocation.position).normalized) >= 15f)
            {
                ResetAim();
            }
        }
    }
    /// <summary>
    /// Moves player and sets animator's parameters
    /// </summary>
    void Move()
    {
        float nextSpeed = m_input.Run ? m_playerRunSpeed : m_playerWalkSpeed;
        if (m_input.Move == Vector2.zero)
        {
            m_steps.Stop();
            nextSpeed = 0f;
        }
        //smoothly change player speed
        m_currentPlayerSpeed = Mathf.Lerp(m_currentPlayerSpeed, nextSpeed, Time.fixedDeltaTime * m_speedChange);
        if (m_currentPlayerSpeed < 0.01f)
            m_currentPlayerSpeed = 0f;

        //player move vector based on input
        if (m_isAiming)
        {
            if (!m_isCrouched)
            {
                transform.forward = Vector3.Slerp(transform.forward, new Vector3(m_moveCameraTarget.forward.x, 0f, m_moveCameraTarget.forward.z), Time.fixedDeltaTime * m_cameraTurn);
            }
            Aim();
        }
        else if (!m_isAiming && !m_isCrouched)
        {
            //moves player according to camera forward vector
            Vector3 move = m_input.Move.x * m_moveCameraTarget.right + m_input.Move.y * m_moveCameraTarget.forward;
            move.y = 0f;

            //rotate to face input direction relative to camera position
            transform.forward = Vector3.Slerp(transform.forward, move, Time.fixedDeltaTime * m_cameraTurn);
        }

        if (m_isCrouched != m_input.Crouch)
        {
            //if player is not standing behind a cover and crouch button is pressed, do nothing
            if (m_input.Crouch && Physics.Raycast(m_col.bounds.center, transform.forward, 2f, LayerMask.GetMask("Cover")) || !m_input.Crouch)
            {
                m_isCrouched = m_input.Crouch;
                if (!m_isAiming)
                    SetCrouchOffset(m_isCrouched);
            }
            else if (m_input.Crouch)
            {
                m_input.DisableCrouch();
            }
        }
        //if player moves away from a cover or stops standing behind it, stop crouching, despite crouch button hasn't been pressed
        else if (m_isCrouched &&
            (m_input.Move.y < -0.5f ||
           !Physics.Raycast(m_col.bounds.center, transform.forward, 3f, LayerMask.GetMask("Cover"))))
        //   || (!Physics.Raycast(new(m_col.bounds.max.x, m_col.bounds.center.y, m_col.bounds.center.z), transform.forward, 4f, 1 << LayerMask.NameToLayer("Cover")) &&
        //    !Physics.Raycast(new(m_col.bounds.min.x, m_col.bounds.center.y, m_col.bounds.center.z), transform.forward, 4f, 1 << LayerMask.NameToLayer("Cover")))))
        {
            m_isCrouched = false;
            if (!m_isAiming)
                SetCrouchOffset(m_isCrouched);
            m_input.DisableCrouch();
        }

        if (m_aimCrouched && Vector3.Dot(transform.forward, Camera.main.transform.forward) >= 0)
        {
            m_aimCrouched = false;
            SetCrouchOffset(!m_isAiming);
            ResetAim();
            m_anim.SetBool(m_HashAimCrouch, m_aimCrouched);
        }

        m_anim.SetFloat(m_HashHorizontal, m_input.Move.x);
        m_anim.SetFloat(m_HashVertical, m_input.Move.y);
        m_anim.SetBool(m_HashCrouching, m_isCrouched);
        m_anim.SetFloat(m_HashSpeed, m_currentPlayerSpeed);
    }
    /// <summary>
    /// Moves camera vericaly and changes collider
    /// </summary>
    /// <param name="isCrouch">begin or stop crouching</param>
    void SetCrouchOffset(bool isCrouch)
    {
        //moves cameras
        Vector3 offset = new(0f, m_aimCameraTarget.localPosition.y + (isCrouch ? -1 : 1) * m_crouchOffset, 0f);
        //changes collider according to crouch animations
        m_col.center = new(m_col.center.x, m_col.center.y + (isCrouch ? -1 : 1) * m_crouchOffset, 0f);
        m_col.height += (isCrouch ? -1 : 1) * (m_crouchOffset + 0.2f);
        m_aimCameraTarget.localPosition = m_moveCameraTarget.localPosition = offset;
    }

    private void OnAnimatorMove()
    {
        //player move vector based on input
        Vector3 move = Vector3.zero;
        if (m_isAiming || m_isCrouched)
        {
            //moves player according to aim camera forward vector
            move = m_input.Move.x * m_aimCameraTarget.right + m_input.Move.y * m_aimCameraTarget.forward;
        }
        else if (!m_isAiming && !m_isCrouched)
        {
            //moves player according to camera forward vector
            move = m_input.Move.x * m_moveCameraTarget.right + m_input.Move.y * m_moveCameraTarget.forward;
        }

        move.y = 0f;
        m_rb.MovePosition(m_rb.position + move * m_anim.deltaPosition.magnitude);
    }

    /// <summary>
    /// Casts a sphere from the center of the screen to detect an enemy
    /// If an enemy is detected, cast another sphere from gun to hitpoint to make sure that bullet wouldn't have obstacles
    /// If bullet's path is free, rotates player and camera towards the hitpoint
    /// </summary>
    void Aim()
    {
        m_aimTarget = Camera.main.transform.position + Camera.main.transform.forward * m_aimDistance;

        if (!Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, m_aimDistance, LayerMask.GetMask("Enemy")))
        {
            if (Physics.SphereCast(Camera.main.transform.position, 0.3f, Camera.main.transform.forward, out RaycastHit hitInfo, m_aimDistance, LayerMask.GetMask("Enemy")))
            {
                var rotateDir = Vector3.RotateTowards(Camera.main.transform.forward, new Vector3(hitInfo.collider.transform.position.x, hitInfo.point.y, hitInfo.point.z) - Camera.main.transform.position, Time.fixedDeltaTime * m_cameraTurn, 0f).normalized;
                //if the nearest enemy is not close, move aiming camera
                if (Vector3.Angle(Camera.main.transform.forward, rotateDir) > 5f)
                {
                    m_cameraPitch += rotateDir.y * Time.fixedDeltaTime * m_cameraRotationSpeed;
                    m_cameraYaw += rotateDir.x * Time.fixedDeltaTime * m_cameraRotationSpeed;
                }
                m_aimTarget = hitInfo.point;
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
            m_aimImage.SetActive(isAiming);
            if (isAiming)
            {
                m_aimCrouched = m_isCrouched && Vector3.Dot(transform.forward, Camera.main.transform.forward) < 0;
                ResetAim();
                m_aimCamera.Priority = 11;
            }
            else
            {
                m_aimCrouched = false;
                ResetAim();
                m_aimCamera.Priority = 9;
            }
            //when crouch and aiming, stand up
            if (m_isCrouched && !m_aimCrouched)
            {
                SetCrouchOffset(!isAiming);
            }
            m_anim.SetBool(m_HashAimCrouch, m_aimCrouched);
            m_isAiming = isAiming;
        }
    }
    /// <summary>
    /// Reset rig values
    /// </summary>
    void ResetAim()
    {
        m_TargetL.localRotation = m_baseTL;
        m_TargetR.localRotation = m_baseTR;
        m_RPitch = m_baseTR.eulerAngles.x;
        m_RYaw = m_baseTR.eulerAngles.z;
        m_LPitch = m_baseTL.eulerAngles.x;
        m_LYaw = m_baseTL.eulerAngles.z;
    }
    /// <summary>
    /// Start shooting
    /// </summary>
    void Fire()
    {

        if (m_isAiming)
        {
            m_gun.Fire(m_aimTarget);
        }
        else
        {
            m_gun.Fire(m_gun.BarrelLocation.position + m_gun.BarrelLocation.forward);
        }
    }
    /// <summary>
    /// Playes walk step sound in animation
    /// </summary>
    public void PlayStep()
    {
        m_steps.clip = m_stepSounds[Random.Range(0, m_stepSounds.Length)];
        m_steps.Play();
    }
    /// <summary>
    /// Playes crouch step sound in animation
    /// </summary>
    public void PlayCrouch()
    {
        m_steps.clip = m_crouchStep;
        m_steps.Play();
    }

    /// <summary>
    /// Takes the damage from enemy's bullet
    /// </summary>
    void Hit()
    {
        AddHealth(-HitPoint);
        if (m_health <= 0)
        {
            Aim(false);
            m_dead = true;
            m_input.Die();
        }
        else
        {
            m_voice.PlayOneShot(m_hitSounds[Random.Range(0, m_hitSounds.Length)]);
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

        m_healthBar.value = m_health;
    }

    //Detects bullets
    private void OnParticleCollision(GameObject other)
    {
        if (!m_dead)
        {
            Hit();
           // Debug.Log("PLAYER HIT");
        }
    }

    public void SwitchController()
    {
        m_input.LockInput();
        m_anim.SetFloat(m_HashSpeed, 0f);
        m_damageVignette.intensity.value = 0f;
        GetComponent<RigBuilder>().enabled = false;
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawLine(m_gun.BarrelLocation.position, m_aimTarget);
    //    Gizmos.color = Color.magenta;
    //    Gizmos.DrawLine(m_gun.BarrelLocation.position, m_gun.BarrelLocation.position + m_gun.BarrelLocation.forward * m_aimDistance);
    //}

    /// <summary>
    /// Reset player's values, when the player is dead
    /// </summary>
    public void Reset()
    {
        m_dead = false;
        transform.SetPositionAndRotation(m_startPosition.position, m_startPosition.rotation);
        m_rb.position = m_startPosition.position;
        m_rb.rotation = m_startPosition.rotation;
        m_health = m_healthBar.maxValue;
        m_healthBar.value = m_health;
        m_damageVignette.intensity.value = 0f;
        m_anim.Rebind();
        m_anim.Update(0f);
        m_cameraPitch = m_cameraYaw = 0f;
    }
}
