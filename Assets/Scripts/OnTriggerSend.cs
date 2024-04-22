using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnTriggerSend : MonoBehaviour
{
    [SerializeField]
    CommandReceiver m_receiver;
    bool m_commandSend;

    private void OnTriggerEnter(Collider other)
    {
        if (!m_commandSend && other.gameObject.CompareTag("Player"))
        {
            m_commandSend = true;
            m_receiver.Receive(transform.GetChild(0).position);
        }
    }

    public void Reset()
    {
        m_commandSend = false;
    }
}
