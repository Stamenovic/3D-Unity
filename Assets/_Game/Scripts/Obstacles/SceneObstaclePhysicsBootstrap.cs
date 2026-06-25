using UnityEngine;

public class SceneObstaclePhysicsBootstrap : MonoBehaviour
{
    private const string RootName = "Runtime Obstacle Physics";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<SceneObstaclePhysicsBootstrap>() != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        root.AddComponent<SceneObstaclePhysicsBootstrap>();
    }

    private void Start()
    {
        ConfigureExistingStones();
    }

    private void ConfigureExistingStones()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Transform item in transforms)
        {
            if (!IsSceneStone(item))
            {
                continue;
            }

            if (item.GetComponent<Rigidbody>() == null)
            {
                item.gameObject.AddComponent<Rigidbody>();
            }

            PhysicsObstaclePiece obstacle = item.GetComponent<PhysicsObstaclePiece>();
            if (obstacle == null)
            {
                obstacle = item.gameObject.AddComponent<PhysicsObstaclePiece>();
            }

            obstacle.ConfigurePhysics();
        }
    }

    private bool IsSceneStone(Transform item)
    {
        string objectName = item.name.ToLowerInvariant();
        if (!objectName.StartsWith("stone "))
        {
            return false;
        }

        Transform parent = item.parent;
        while (parent != null)
        {
            if (parent.name.ToLowerInvariant().Contains("obstacle"))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }
}
