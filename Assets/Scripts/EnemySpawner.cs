using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    GameObject enemyPerfab;
    [SerializeField]
    int amountToSpawn;

    [Header("Enemy materials:")]
    [SerializeField]
    Material[] enemyMaterials;

    readonly float m_spawnBreak = 1.5f;

    public void SpawnEnemies(Vector3 location)
    {
        StartCoroutine(nameof(CreateEnemies), location);
    }

    IEnumerator CreateEnemies(Vector3 location)
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            GameObject enemy = Instantiate(enemyPerfab, location, enemyPerfab.transform.rotation);
            enemy.name = enemyPerfab.name + i;

            int materialNum = Random.Range(0, 3);
            for (int j = 0; j < 4; j++)
            {
                if(!enemy.transform.GetChild(j).TryGetComponent<Renderer>(out var render))
                {
                    render = enemy.transform.GetChild(j).GetComponentInChildren<Renderer>();
                }

                render.material = enemyMaterials[j * 3 + materialNum];
            }
            yield return new WaitForSeconds(m_spawnBreak);
        }
    }
}
