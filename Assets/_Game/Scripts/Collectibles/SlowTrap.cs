using UnityEngine;

public class SlowTrap : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private float speedMultiplier = 0.4f;

    [Header("Visuals")]
    [SerializeField] private ParticleSystem pulseEffect;

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerRigidbodyMovement>();
        if (player == null) return;

        player.SetZoneSpeedEffect(speedMultiplier);

        if (pulseEffect != null && !pulseEffect.isPlaying)
            pulseEffect.Play();
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<PlayerRigidbodyMovement>();
        if (player == null) return;

        player.SetZoneSpeedEffect(1f);

        if (pulseEffect != null)
            pulseEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
