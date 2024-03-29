using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    GameObject m_enemyPerfab;
    [SerializeField]
    int m_amountToSpawn;

    [Header("Enemy materials:")]
    [SerializeField]
    Material[] m_enemyMaterials;

    readonly float m_spawnBreak = 1.5f;

    public void SpawnEnemies(Vector3 location)
    {
        StartCoroutine(nameof(CreateEnemies), location);
    }

    IEnumerator CreateEnemies(Vector3 location)
    {
        for (int i = 0; i < m_amountToSpawn; i++)
        {
            GameObject enemy = Instantiate(m_enemyPerfab, location, m_enemyPerfab.transform.rotation);
            enemy.name = m_enemyPerfab.name + i;

            int materialNum = Random.Range(0, 3);
            for (int j = 0; j < 4; j++)
            {
                if(!enemy.transform.GetChild(j).TryGetComponent<Renderer>(out var render))
                {
                    render = enemy.transform.GetChild(j).GetComponentInChildren<Renderer>();
                }

                render.material = m_enemyMaterials[j * 3 + materialNum];
            }
            yield return new WaitForSeconds(m_spawnBreak);
        }
    }
}
