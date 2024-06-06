using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnTriggerSend : MonoBehaviour
{
    [SerializeField]
    CommandReceiver m_receiver;
    [SerializeField]
    bool m_isBoss;
    bool m_commandSend;


    private void OnTriggerEnter(Collider other)
    {
        if (!m_commandSend && other.gameObject.CompareTag("Player"))
        {
            m_commandSend = m_receiver.Receive(m_isBoss ? other.gameObject.transform : transform.GetChild(0));
        }
    }

    public void Reset()
    {
        m_commandSend = false;
    }
}
