using UnityEngine;
using UnityEngine.Events;

public class CommandReceiver : MonoBehaviour
{
    [SerializeField]
    bool m_isBoss;
    [SerializeField]
    UnityEvent<Vector3, Quaternion> m_command;

    public bool Receive(Transform location)
    {
        if (m_isBoss)
        {
            foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
            {
                if (!enemy.GetComponent<EnemyAIController>().IsDead)
                    return false;
            }
        }
        m_command.Invoke(location.position, location.rotation);
        return true;
    }
}
