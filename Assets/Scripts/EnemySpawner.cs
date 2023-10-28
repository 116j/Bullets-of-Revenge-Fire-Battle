using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    GameObject enemyPerfab;
    [SerializeField]
    int amountToSpawn;

    public void SpawnEnemies()
    {
        for (int i = 0; i < amountToSpawn; i++)
        {
            Instantiate(enemyPerfab,transform);
        }
    }
}
