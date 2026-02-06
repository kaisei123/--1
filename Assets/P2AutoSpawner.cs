using UnityEngine;

public class P2AutoSpawner : MonoBehaviour
{
    public Transform p2Spawn;
    public GameObject p2Prefab;

    void Start()
    {
        if (!p2Prefab || !p2Spawn)
        {
            Debug.LogError("[P2AutoSpawner] p2Prefab or p2Spawn is missing.");
            return;
        }

        Instantiate(p2Prefab, p2Spawn.position, p2Spawn.rotation);
        Debug.Log("[P2AutoSpawner] Spawned P2.");
    }
}
