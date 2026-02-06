using UnityEngine;
using UnityEngine.InputSystem;

public class AutoJoinAtStart : MonoBehaviour
{
    public PlayerInputManager manager;
    public int playersToSpawn = 1; // とりあえず1でOK

    void Awake()
    {
        if (!manager) manager = GetComponent<PlayerInputManager>();
    }

    void Start()
    {
        for (int i = 0; i < playersToSpawn; i++)
        {
            manager.JoinPlayer();
        }
        Debug.Log($"Auto-joined {playersToSpawn} player(s).");
    }
}
