using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    GameObject m_enemyPerfab;
    [SerializeField]
    int m_amountToSpawn;

    [Header("Enemy materials:")]
    [SerializeField]
    Material[] m_enemyMaterials;

    List<GameObject> m_enemies;

    readonly float m_spawnBreak = 1.5f;

    static EnemySpawner m_instance;
    public static EnemySpawner Instance => m_instance;
    Transform m_location;

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        m_location = transform;

        m_enemies = new List<GameObject>();
    }

    public void SpawnEnemies(Vector3 position, Quaternion rotation)
    {
        m_location.position = position;
        m_location.rotation = rotation;
        StartCoroutine(nameof(CreateEnemies));
    }

    IEnumerator CreateEnemies()
    {
        for (int i = 0; i < m_amountToSpawn; i++)
        {
            GameObject enemy = Instantiate(m_enemyPerfab, m_location.position, m_location.rotation);
            enemy.name = m_enemyPerfab.name + i;

            int materialNum = Random.Range(0, 2);
            for (int j = 0; j < 4; j++)
            {
                if (!enemy.transform.GetChild(j).TryGetComponent<Renderer>(out var render))
                {
                    render = enemy.transform.GetChild(j).GetComponentInChildren<Renderer>();
                }

                render.material = m_enemyMaterials[j * 2 + materialNum];
            }

            m_enemies.Add(enemy);
            yield return new WaitForSeconds(m_spawnBreak);
        }
    }

    public void DestroyAll()
    {
        foreach (var enemy in m_enemies)
        {
            Destroy(enemy);
        }

        m_enemies.Clear();
    }
}
