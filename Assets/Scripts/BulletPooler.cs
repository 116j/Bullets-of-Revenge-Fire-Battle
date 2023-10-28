using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPooler : MonoBehaviour
{
    [SerializeField]
    int amountToPool;
    [SerializeField]
    GameObject bulletPrefab;

    List<GameObject> pooledBullets;

    public static BulletPooler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    void Start()
    {
        pooledBullets = new List<GameObject>();
        for (int i = 0; i < amountToPool; i++)
        {
            CreateBullet();
        }
    }

    public GameObject GetBullet()
    {
        for (int i = 0; i < pooledBullets.Count; ++i)
        {
            if (!pooledBullets[i].activeInHierarchy)
            {
                return pooledBullets[i];
            }
        }
        return CreateBullet();
    }

    GameObject CreateBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);
        bullet.transform.parent = transform;
        pooledBullets.Add(bullet);
        return bullet;
    }
}
