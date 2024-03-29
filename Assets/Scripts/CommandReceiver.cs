using UnityEngine;
using UnityEngine.Events;

public class CommandReceiver : MonoBehaviour
{
    [SerializeField]
    UnityEvent<Vector3> m_command;

    public void Receive(Vector3 location)
    {
        m_command.Invoke(location);
    }
}
