using System.Collections;
using UnityEngine;

public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float duration = 5f;

    [Header("Visuals")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private ParticleSystem collectEffect;

    [Header("Bob & Spin")]
    [SerializeField] private float bobAmplitude = 0.3f;
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float spinSpeed = 90f;

    private bool collected;
    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;

        float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        var player = other.GetComponent<PlayerRigidbodyMovement>();
        if (player == null) return;

        collected = true;
        player.ApplyTimedSpeedEffect(speedMultiplier, duration);
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);

        if (collectEffect != null)
        {
            collectEffect.transform.SetParent(null);
            collectEffect.Play();
        }

        yield return null;
        Destroy(gameObject);
    }
}
