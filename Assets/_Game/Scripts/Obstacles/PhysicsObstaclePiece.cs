using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PhysicsObstaclePiece : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float mass = 2f;
    [SerializeField] private float drag = 0.12f;
    [SerializeField] private float angularDrag = 0.1f;
    [SerializeField] private CollisionDetectionMode collisionDetection = CollisionDetectionMode.ContinuousDynamic;
    [SerializeField] private bool lockUntilPlayerImpact = false;
    [SerializeField] private float linkedActivationRadius = 2.5f;
    [SerializeField] private float extraGravity = 8f;
    [SerializeField] private float maxHorizontalSpeed = 3f;
    [SerializeField] private float maxUpwardSpeed = 0.35f;
    [SerializeField] private float colliderFriction = 0.18f;
    [SerializeField] private bool preferMeshCollider = true;

    [Header("Impact")]
    [SerializeField] private float extraPushForce = 0.18f;
    [SerializeField] private float sustainedPushForce = 9.6f;
    [SerializeField] private float pushAssistSpeed = 1.5f;
    [SerializeField] private float speedToPushMultiplier = 0.55f;
    [SerializeField] private float upwardScatterForce = 0f;
    [SerializeField] private float torqueForce = 0.75f;
    [SerializeField] private float maxAppliedImpactSpeed = 4f;

    private Rigidbody rb;
    private PhysicsMaterial lowFrictionMaterial;
    private bool activated;

    private void Awake()
    {
        ConfigurePhysics();
    }

    public void ConfigurePhysics()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.collisionDetectionMode = collisionDetection;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
        rb.isKinematic = lockUntilPlayerImpact;
        activated = true;

        EnsureCollider();
        ConfigureColliderFriction();
    }

    private void EnsureCollider()
    {
        if (preferMeshCollider && TryConfigureMeshCollider())
        {
            return;
        }

        if (GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        Bounds bounds = CalculateRenderBounds();
        box.center = transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(
            Mathf.Max(0.25f, bounds.size.x / Mathf.Max(0.001f, transform.lossyScale.x)),
            Mathf.Max(0.25f, bounds.size.y / Mathf.Max(0.001f, transform.lossyScale.y)),
            Mathf.Max(0.25f, bounds.size.z / Mathf.Max(0.001f, transform.lossyScale.z))
        );
    }

    private bool TryConfigureMeshCollider()
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
        meshCollider.isTrigger = false;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(boxCollider);
            }
            else
            {
                DestroyImmediate(boxCollider);
            }
        }

        MeshCollider rootMeshCollider = GetComponent<MeshCollider>();
        if (rootMeshCollider != null && rootMeshCollider != meshCollider)
        {
            if (Application.isPlaying)
            {
                Destroy(rootMeshCollider);
            }
            else
            {
                DestroyImmediate(rootMeshCollider);
            }
        }

        return true;
    }

    private Bounds CalculateRenderBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void OnCollisionEnter(Collision collision)
    {
        ApplyPlayerImpact(collision, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        ApplyPlayerImpact(collision, false);
    }

    private void FixedUpdate()
    {
        if (!activated || rb == null || rb.isKinematic)
        {
            return;
        }

        rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        ClampVelocity();
    }

    private void ApplyPlayerImpact(Collision collision, bool initialImpact)
    {
        PlayerRigidbodyMovement player = collision.collider.GetComponentInParent<PlayerRigidbodyMovement>();
        if (player == null || rb == null || collision.contactCount == 0)
        {
            return;
        }

        ActivateLinkedPieces();

        Vector3 awayFromPlayer = (transform.position - player.transform.position).normalized;
        if (awayFromPlayer.sqrMagnitude < 0.01f)
        {
            awayFromPlayer = collision.GetContact(0).normal;
        }

        float impactSpeed = Mathf.Min(maxAppliedImpactSpeed, Mathf.Max(player.CurrentSpeed, collision.relativeVelocity.magnitude));
        Vector3 pushDirection = new Vector3(awayFromPlayer.x, 0f, awayFromPlayer.z);
        if (pushDirection.sqrMagnitude < 0.01f)
        {
            pushDirection = new Vector3(transform.position.x - player.transform.position.x, 0f, transform.position.z - player.transform.position.z);
        }

        pushDirection = (pushDirection.normalized + Vector3.up * upwardScatterForce).normalized;

        if (initialImpact)
        {
            rb.AddForce(pushDirection * impactSpeed * extraPushForce, ForceMode.Impulse);
            rb.AddTorque(Vector3.Cross(Vector3.up, pushDirection) * impactSpeed * torqueForce, ForceMode.Impulse);
        }
        else
        {
            rb.AddForce(pushDirection * impactSpeed * sustainedPushForce, ForceMode.Force);
            ApplyPushAssist(pushDirection, player.CurrentSpeed);
        }
    }

    private void ApplyPushAssist(Vector3 pushDirection, float playerSpeed)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float targetSpeed = Mathf.Min(maxHorizontalSpeed, Mathf.Max(pushAssistSpeed, playerSpeed * speedToPushMultiplier));
        Vector3 assistedVelocity = pushDirection * targetSpeed;

        if (Vector3.Dot(horizontalVelocity, pushDirection) < targetSpeed)
        {
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, assistedVelocity, sustainedPushForce * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horizontalVelocity.x, Mathf.Min(velocity.y, maxUpwardSpeed), horizontalVelocity.z);
        }
    }

    private void ClampVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        if (horizontal.magnitude > maxHorizontalSpeed)
        {
            horizontal = horizontal.normalized * maxHorizontalSpeed;
        }

        float vertical = Mathf.Min(velocity.y, maxUpwardSpeed);
        rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
    }

    private void ActivateLinkedPieces()
    {
        if (activated)
        {
            return;
        }

        PhysicsObstaclePiece[] pieces = FindObjectsByType<PhysicsObstaclePiece>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (PhysicsObstaclePiece piece in pieces)
        {
            if (Vector3.Distance(transform.position, piece.transform.position) <= linkedActivationRadius)
            {
                piece.ActivatePhysics();
            }
        }
    }

    private void ActivatePhysics()
    {
        if (activated)
        {
            return;
        }

        activated = true;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.WakeUp();
    }

    private void ConfigureColliderFriction()
    {
        if (lowFrictionMaterial == null)
        {
            lowFrictionMaterial = new PhysicsMaterial("Obstacle Low Friction")
            {
                dynamicFriction = colliderFriction,
                staticFriction = colliderFriction,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider obstacleCollider in colliders)
        {
            obstacleCollider.material = lowFrictionMaterial;
        }
    }
}
