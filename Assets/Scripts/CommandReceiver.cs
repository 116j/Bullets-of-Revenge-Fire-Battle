using UnityEngine;
using UnityEngine.Events;

public class CommandReceiver : MonoBehaviour
{
    [SerializeField]
    bool m_isBoss;
    [SerializeField]
    UnityEvent<Vector3> m_command;

    public void Receive(Vector3 location)
    {
        if (m_isBoss)
        {
            foreach(var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                if(!enemy.GetComponent<EnemyAIController>().IsDead)
                    return;
            }
        }
        m_command.Invoke(location);
    }
}
