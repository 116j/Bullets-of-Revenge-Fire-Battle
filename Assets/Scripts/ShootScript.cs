using UnityEngine;

public class ShootScript : MonoBehaviour
{
    [SerializeField]
    GameObject bulletPrefab;
    [SerializeField]
    GameObject casingPrefab;
    [SerializeField]
    ParticleSystem muzzleFlashParticles;
    [SerializeField]
    Transform barrelLocation;
    [SerializeField]
    Transform casingExitLocation;

    public Transform BarrelLocation => barrelLocation;

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
        muzzleFlashParticles.Play();
        m_shootSound.Play();
        Instantiate(bulletPrefab, barrelLocation.position, barrelLocation.rotation).GetComponent<Rigidbody>().AddForce((m_targetPos-barrelLocation.position) * m_shotPower);
    }

    //This function creates a casing at the ejection slot
    public void CasingRelease()
    {
        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);
        //Add force on casing to push it out
        tempCasing.GetComponent<Rigidbody>().AddExplosionForce(Random.Range(m_ejectPower * 0.7f, m_ejectPower), (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f), 1f);
        //Add torque to make casing spin in random direction
        tempCasing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(100f, 1000f)), ForceMode.Impulse);

        //Destroy casing after X seconds
        Destroy(tempCasing, m_destroyTimer);
    }
}

