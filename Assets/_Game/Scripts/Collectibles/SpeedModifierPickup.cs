using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SpeedModifierPickup : MonoBehaviour
{
    private enum ModifierTarget
    {
        MovementSpeed,
        JumpForce,
        SprintSpeed
    }

    [Header("Modifier")]
    [SerializeField] private ModifierTarget modifierTarget = ModifierTarget.MovementSpeed;
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float duration = 5f;

    [Header("Collect")]
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private Color collectEffectColor = Color.white;
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
        ApplyModifier(player);
        StartCoroutine(CollectRoutine());
    }

    private void ApplyModifier(PlayerRigidbodyMovement player)
    {
        switch (modifierTarget)
        {
            case ModifierTarget.JumpForce:
                player.ApplyTimedJumpEffect(speedMultiplier, duration);
                break;
            case ModifierTarget.SprintSpeed:
                player.ApplyTimedSprintSpeedEffect(speedMultiplier, duration);
                break;
            default:
                player.ApplyTimedSpeedEffect(speedMultiplier, duration);
                break;
        }
    }

    private IEnumerator CollectRoutine()
    {
        PlayCollectSound();

        if (collectEffect == null)
            collectEffect = CreateCollectEffect();

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

    private ParticleSystem CreateCollectEffect()
    {
        var effectObject = new GameObject("CollectEffect");
        effectObject.transform.SetParent(transform, false);
        effectObject.transform.localPosition = Vector3.zero;
        effectObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        var particles = effectObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = 4.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = collectEffectColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 36)
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(collectEffectColor, 0.18f),
                new GradientColorKey(collectEffectColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.2f, 1f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.maxParticleSize = 0.5f;

        return particles;
    }
}
