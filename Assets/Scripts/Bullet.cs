using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    bool isPooled;

    float m_lifeTime = 0;
    bool m_isActive = true;
    readonly float m_maxLifeTime = 4f;

    private void OnCollisionEnter(Collision collision)
    {
        DestroyBullet();
    }

    private void Update()
    {
        if (m_isActive)
        {
            m_lifeTime += Time.deltaTime;
            if (m_lifeTime > m_maxLifeTime)
            {
                DestroyBullet();
            }
        }
    }

    void RespawnBullet()
    {

    }

    void DestroyBullet()
    {
        if (isPooled)
        {
            m_isActive = false;
            m_lifeTime = 0;
            gameObject.SetActive(m_isActive);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
