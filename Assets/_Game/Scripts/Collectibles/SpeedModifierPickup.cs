using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SpeedModifierPickup : MonoBehaviour
{
    [Header("Speed Modifier")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float duration = 5f;

    [Header("Collect")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioSource collectAudio;
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private float collectVolume = 0.85f;
    [SerializeField] private bool destroyOnCollect = true;
    [SerializeField] private float respawnDelay = 0f;

    private Collider triggerCollider;
    private bool collected;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;

        if (visualRoot == null)
            visualRoot = gameObject;

        if (collectClip == null)
            collectClip = Resources.Load<AudioClip>("Audio/CollectItemSoundEffect");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        var player = other.GetComponentInParent<PlayerRigidbodyMovement>();
        if (player == null)
            return;

        collected = true;
        player.ApplyTimedSpeedEffect(speedMultiplier, duration);
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        PlayCollectSound();

        if (collectEffect != null)
        {
            collectEffect.transform.SetParent(null);
            collectEffect.Play();
        }

        if (visualRoot != null)
            visualRoot.SetActive(false);

        triggerCollider.enabled = false;

        if (destroyOnCollect)
        {
            float waitTime = collectClip != null ? collectClip.length : 0f;
            if (collectEffect != null)
                waitTime = Mathf.Max(waitTime, collectEffect.main.duration);

            if (waitTime > 0f)
                yield return new WaitForSeconds(waitTime);

            Destroy(gameObject);
            yield break;
        }

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);

        if (visualRoot != null)
            visualRoot.SetActive(true);

        triggerCollider.enabled = true;
        collected = false;
    }

    private void PlayCollectSound()
    {
        if (!GameUIBootstrap.EffectsEnabled)
            return;

        float volume = collectVolume * GameUIBootstrap.EffectsVolume;
        if (collectAudio != null)
        {
            collectAudio.clip = collectAudio.clip != null ? collectAudio.clip : collectClip;
            collectAudio.volume = volume;
            collectAudio.Play();
            return;
        }

        if (collectClip != null)
            AudioSource.PlayClipAtPoint(collectClip, transform.position, volume);
    }
}
