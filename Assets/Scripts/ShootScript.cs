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

    //"Specify time to destory the casing object
    float destroyTimer = 10f;
    //Bullet Speed
    float shotPower = 500f;
    float ejectPower = 150f;
    Vector3 shootDirection;

    private Animator gunAnimator;
    private AudioSource shootSound;


    void Start()
    {
        gunAnimator = GetComponent<Animator>();
        shootSound = GetComponent<AudioSource>();
    }

    public void Fire(Vector3 firePos)
    {
        gunAnimator.SetTrigger("Fire");
        shootSound.Play();
        if (firePos != Vector3.zero)
        {
            shootDirection = firePos - barrelLocation.position;
        }
        else
        {
            shootDirection = barrelLocation.forward;
        }
    }


    //This function creates the bullet behavior
    public void Shoot()
    {
        muzzleFlashParticles.Play();
        Instantiate(bulletPrefab, barrelLocation.position, barrelLocation.rotation).GetComponent<Rigidbody>().AddForce(shootDirection * shotPower);
    }

    //This function creates a casing at the ejection slot
    public void CasingRelease()
    {
        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);
        //Add force on casing to push it out
        tempCasing.GetComponent<Rigidbody>().AddExplosionForce(Random.Range(ejectPower * 0.7f, ejectPower), (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f), 1f);
        //Add torque to make casing spin in random direction
        tempCasing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(100f, 1000f)), ForceMode.Impulse);

        //Destroy casing after X seconds
        Destroy(tempCasing, destroyTimer);
    }
}

