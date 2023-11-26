using UnityEngine;
using UnityEngine.Events;

public class CommandReceiver : MonoBehaviour
{
    [SerializeField]
    UnityEvent<Vector3> command;

    public void Receive(Vector3 location)
    {
        command.Invoke(location);
    }
}
