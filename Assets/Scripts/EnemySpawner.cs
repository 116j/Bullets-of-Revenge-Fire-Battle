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

    readonly float m_spawnBreak = 3f;

    public void SpawnEnemies(Vector3 location)
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            StartCoroutine(nameof(CreateEnemy), location);
        }
    }

    IEnumerator CreateEnemy(Vector3 location)
    {
        GameObject enemy = Instantiate(enemyPerfab, location, enemyPerfab.transform.rotation);

        int materialNum = Random.Range(0, 3);
        for (int i = 0; i < 3; i++)
        {
            enemy.transform.GetChild(i).GetComponent<Renderer>().material = enemyMaterials[i * 3 + materialNum];
        }
        yield return new WaitForSeconds(m_spawnBreak);
    }
}
