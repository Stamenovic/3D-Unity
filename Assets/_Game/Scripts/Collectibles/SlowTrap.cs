using UnityEngine;

/// <summary>
/// A persistent zone trap that slows the player while they remain inside it.
/// Does not get consumed — the player is slowed on enter and restored on exit.
/// </summary>
public class SlowTrap : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private float speedMultiplier = 0.4f;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem ambientEffect;

    [Header("Bob")]
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobFrequency = 0.8f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;

        if (ambientEffect != null)
            ambientEffect.Play();
    }

    private void Update()
    {
        float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerRigidbodyMovement>();
        if (player != null)
            player.SetZoneSpeedEffect(speedMultiplier);
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<PlayerRigidbodyMovement>();
        if (player != null)
            player.SetZoneSpeedEffect(1f);
    }
}
