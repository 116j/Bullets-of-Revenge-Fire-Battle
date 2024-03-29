using UnityEngine;

public class ShootScript : MonoBehaviour
{
    [SerializeField]
    GameObject m_bulletPrefab;
    [SerializeField]
    GameObject m_casingPrefab;
    [SerializeField]
    ParticleSystem m_muzzleFlashParticles;
    [SerializeField]
    Transform m_barrelLocation;
    [SerializeField]
    Transform m_casingExitLocation;

    public Transform BarrelLocation => m_barrelLocation;

    //"Specify time to destory the casing object
    readonly float m_destroyTimer = 4f;
    //Bullet Speed
    readonly float m_shotPower = 1800f;
    readonly float m_ejectPower = 150f;

    private Animator m_gunAnimator;
    private AudioSource m_shootSound;
    Vector3 m_targetPos;

    void Start()
    {
        m_gunAnimator = GetComponent<Animator>();
        m_shootSound = GetComponent<AudioSource>();
    }

    public void Fire(Vector3 target)
    {
        m_targetPos = target;
        m_gunAnimator.SetTrigger("Fire");
    }


    //This function creates the bullet behavior
    public void Shoot()
    {
        m_muzzleFlashParticles.Play();
        m_shootSound.Play();
        Instantiate(m_bulletPrefab, m_barrelLocation.position, m_barrelLocation.rotation).GetComponent<Rigidbody>().AddForce((m_targetPos-m_barrelLocation.position) * m_shotPower);
    }

    //This function creates a casing at the ejection slot
    public void CasingRelease()
    {
        GameObject tempCasing = Instantiate(m_casingPrefab, m_casingExitLocation.position, m_casingExitLocation.rotation);
        //Add force on casing to push it out
        tempCasing.GetComponent<Rigidbody>().AddExplosionForce(Random.Range(m_ejectPower * 0.7f, m_ejectPower), (m_casingExitLocation.position - m_casingExitLocation.right * 0.3f - m_casingExitLocation.up * 0.6f), 1f);
        //Add torque to make casing spin in random direction
        tempCasing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(100f, 1000f)), ForceMode.Impulse);

        //Destroy casing after X seconds
        Destroy(tempCasing, m_destroyTimer);
    }
}

