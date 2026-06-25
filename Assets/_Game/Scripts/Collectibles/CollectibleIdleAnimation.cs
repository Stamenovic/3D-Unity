using UnityEngine;

[DisallowMultipleComponent]
public class CollectibleIdleAnimation : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private Space rotationSpace = Space.Self;

    [Header("Floating")]
    [SerializeField] private bool floatUpDown = true;
    [SerializeField] private Vector3 floatDirection = Vector3.up;
    [SerializeField] private float floatAmplitude = 0.18f;
    [SerializeField] private float floatFrequency = 1.2f;
    [SerializeField] private float phaseOffset = 0f;

    [Header("Glow")]
    [SerializeField] private Light glowLight;
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField] private float baseLightIntensity = 1.4f;
    [SerializeField] private float pulseLightIntensity = 0.7f;
    [SerializeField] private float glowRange = 2.5f;
    [SerializeField] private float glowFrequency = 1.4f;

    private Vector3 startLocalPosition;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        ConfigureLight();
    }

    private void Update()
    {
        if (rotate && rotationAxis.sqrMagnitude > 0.0001f)
            transform.Rotate(rotationAxis.normalized, rotationSpeed * Time.deltaTime, rotationSpace);

        if (floatUpDown && floatDirection.sqrMagnitude > 0.0001f)
        {
            float wave = Mathf.Sin((Time.time + phaseOffset) * floatFrequency * Mathf.PI * 2f);
            transform.localPosition = startLocalPosition + floatDirection.normalized * (wave * floatAmplitude);
        }

        if (glowLight != null)
        {
            float wave = (Mathf.Sin((Time.time + phaseOffset) * glowFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            glowLight.color = glowColor;
            glowLight.range = glowRange;
            glowLight.intensity = baseLightIntensity + pulseLightIntensity * wave;
        }
    }

    private void ConfigureLight()
    {
        if (glowLight == null)
        {
            var lightObject = new GameObject("Pickup Glow");
            lightObject.transform.SetParent(transform, false);
            glowLight = lightObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
        }

        glowLight.color = glowColor;
        glowLight.range = glowRange;
        glowLight.intensity = baseLightIntensity;
    }
}
