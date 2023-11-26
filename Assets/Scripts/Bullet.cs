using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    float m_lifeTime = 0;
    readonly float m_maxLifeTime = 4f;

    private void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        m_lifeTime += Time.deltaTime;
        if (m_lifeTime > m_maxLifeTime)
        {
            Destroy(gameObject);
        }
    }
}
