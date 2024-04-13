using UnityEngine;

public class ShootScript : MonoBehaviour
{
    [SerializeField]
    GameObject m_bulletPrefab;
    [SerializeField]
    ParticleSystem m_muzzleFlashParticles;
    [SerializeField]
    Transform m_barrelLocation;

    public Transform BarrelLocation => m_barrelLocation;

    //Bullet Speed
    readonly float m_shotPower = 1800f;

    private Animator m_gunAnimator;
    private AudioSource m_shootSound;
    Vector3 m_targetPos;

    void Start()
    {
        m_gunAnimator = GetComponent<Animator>();
        m_shootSound = GetComponent<AudioSource>();
        m_targetPos = BarrelLocation.transform.position + BarrelLocation.transform.forward;
    }

    public void Fire()
    {
        m_gunAnimator.SetTrigger("Fire");
    }

    public void Fire(Vector3 target)
    {
        m_targetPos = target;
        Fire();
    }


    //This function creates the bullet behavior
    public void Shoot()
    {
        m_muzzleFlashParticles.Play();
        m_shootSound.Play();
        Instantiate(m_bulletPrefab, m_barrelLocation.position, m_barrelLocation.rotation).GetComponent<Rigidbody>().AddForce((m_targetPos - m_barrelLocation.position) * m_shotPower);
    }

}

