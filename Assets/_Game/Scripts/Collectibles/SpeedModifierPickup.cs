using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SpeedModifierPickup : MonoBehaviour
{
    private const int SparkleTextureSize = 32;
    private static Texture2D sparkleTexture;

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
        main.duration = 1.15f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.95f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, collectEffectColor);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.gravityModifier = 0.15f;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 24),
            new ParticleSystem.Burst(0.08f, 18),
            new ParticleSystem.Burst(0.18f, 10)
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;
        shape.radiusThickness = 0.65f;

        var velocityOverLifetime = particles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.45f, 1.15f);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 1.8f;
        noise.scrollSpeed = 0.75f;

        var rotationOverLifetime = particles.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-8f, 8f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(collectEffectColor, 0.22f),
                new GradientColorKey(collectEffectColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.08f),
                new GradientAlphaKey(0.85f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.45f, 0.55f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var trails = particles.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.lifetime = 0.18f;
        trails.ratio = 0.45f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.55f);
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(new Gradient
        {
            colorKeys = new[]
            {
                new GradientColorKey(collectEffectColor, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            alphaKeys = new[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        });

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateSparkleMaterial();
        renderer.trailMaterial = CreateSparkleMaterial();
        renderer.maxParticleSize = 0.5f;
        renderer.sortingFudge = 1f;

        return particles;
    }

    private static Material CreateSparkleMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        var material = new Material(shader);
        material.mainTexture = GetSparkleTexture();
        material.color = Color.white;
        return material;
    }

    private static Texture2D GetSparkleTexture()
    {
        if (sparkleTexture != null)
            return sparkleTexture;

        sparkleTexture = new Texture2D(SparkleTextureSize, SparkleTextureSize, TextureFormat.RGBA32, false)
        {
            name = "Runtime Sparkle Particle",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (SparkleTextureSize - 1) * 0.5f;
        for (int y = 0; y < SparkleTextureSize; y++)
        {
            for (int x = 0; x < SparkleTextureSize; x++)
            {
                float dx = Mathf.Abs((x - center) / center);
                float dy = Mathf.Abs((y - center) / center);
                float radial = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                float cross = Mathf.Max(
                    Mathf.Pow(Mathf.Clamp01(1f - dx), 5f) * Mathf.Clamp01(1f - dy * 4f),
                    Mathf.Pow(Mathf.Clamp01(1f - dy), 5f) * Mathf.Clamp01(1f - dx * 4f)
                );
                float diagonal = Mathf.Max(
                    Mathf.Clamp01(1f - Mathf.Abs(dx - dy) * 8f),
                    Mathf.Clamp01(1f - Mathf.Abs(dx + dy) * 8f)
                ) * radial * 0.35f;
                float alpha = Mathf.Clamp01(radial * radial * 0.35f + cross + diagonal);
                sparkleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        sparkleTexture.Apply(false, true);
        return sparkleTexture;
    }
}
