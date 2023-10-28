using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleEnumerator : IEnumerator<Transform>
{
    Transform[] m_obstacles;
    int m_index;
    public Transform Current => m_obstacles[m_index];

    object IEnumerator.Current => Current;

    public ObstacleEnumerator(GameObject[] obstacles)
    {
        m_obstacles = Array.ConvertAll(obstacles, o => o.transform);
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
        m_index++;
        return m_index < m_obstacles.Length;
    }

    public void Reset()
    {
        m_index = -1;
    }
}

public sealed class Obstacles : IEnumerable<Transform>
{
    Obstacles m_instance;
    ObstacleEnumerator m_obstacles;

    public Obstacles()
    {
        m_obstacles = new ObstacleEnumerator(GameObject.FindGameObjectsWithTag("Obstacle"));
    }
    public Obstacles Instance 
    {
        get
        {
            if(Instance == null)
            {
                m_instance = new Obstacles();
            }
            return m_instance;
        }
    }
    public IEnumerator<Transform> GetEnumerator() => m_obstacles;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
