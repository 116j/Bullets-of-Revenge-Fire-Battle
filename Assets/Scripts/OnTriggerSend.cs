using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnTriggerSend : MonoBehaviour
{
    [SerializeField]
    Transform spawnLocation;  
    [SerializeField]
    CommandReceiver receiver;
    bool m_commandSend;

    private void OnTriggerEnter(Collider other)
    {
        if (!m_commandSend && other.gameObject.CompareTag("Player"))
        {
            m_commandSend = true;
            receiver.Receive(spawnLocation.position);
        }
    }
}
