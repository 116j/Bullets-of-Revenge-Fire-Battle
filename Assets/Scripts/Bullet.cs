using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    bool isPooled;

    float m_lifeTime = 0;

    readonly float m_maxLifeTime = 4f;

    private void OnCollisionEnter(Collision collision)
    {
        DestroyBullet();
    }

    private void Update()
    {
        m_lifeTime += Time.deltaTime;
        if (m_lifeTime > m_maxLifeTime)
        {
            DestroyBullet();
        }
    }

    void DestroyBullet()
    {
        if (isPooled)
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
