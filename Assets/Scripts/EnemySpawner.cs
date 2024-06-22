using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    GameObject m_enemyPerfab;

    [Header("Enemy materials:")]
    [SerializeField]
    Material[] m_enemyMaterials;

    List<GameObject> m_enemies;

    readonly float m_spawnBreak = 1.5f;
    int m_amountToSpawn = 5;
    int IncreaseAmount => UIController.Instance.GameDifficulty == GameDifficulty.Normal ? 1 : 2;

    static EnemySpawner m_instance;
    public static EnemySpawner Instance => m_instance;
    Transform m_location;
    int m_startAmount;

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
        m_location = transform;
        m_startAmount = m_amountToSpawn;
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

        m_amountToSpawn += IncreaseAmount;
    }

    public void DestroyAll()
    {
        foreach (var enemy in m_enemies)
        {
            Destroy(enemy);
        }
        m_amountToSpawn = m_startAmount;
        m_enemies.Clear();
    }
}
