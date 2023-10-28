using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OnTriggerSend : MonoBehaviour
{
    [SerializeField]
    CommandReceiver receiver;
    bool m_commandSend;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!m_commandSend && other.gameObject.CompareTag("Player"))
        {
            m_commandSend = true;
            receiver.Receive();
        }
    }
}
