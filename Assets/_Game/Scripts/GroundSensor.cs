using UnityEngine;

public class GroundSensor : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;

    private int groundContacts;

    public bool IsGrounded => groundContacts > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            groundContacts++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsInGroundLayer(other.gameObject))
        {
            groundContacts--;
            groundContacts = Mathf.Max(groundContacts, 0);
        }
    }

    private bool IsInGroundLayer(GameObject obj)
    {
        return (groundLayer.value & (1 << obj.layer)) != 0;
    }

    public void ResetContacts()
    {
        groundContacts = 0;
    }
}