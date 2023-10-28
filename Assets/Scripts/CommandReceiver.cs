using UnityEngine;
using UnityEngine.Events;

public class CommandReceiver : MonoBehaviour
{
    [SerializeField]
    UnityEvent command;

    public void Receive()
    {
        command.Invoke();
    }
}
