using UnityEngine;
using UnityEngine.InputSystem;

public class JoinDebug : MonoBehaviour
{
    PlayerInputManager mgr;

    void Awake()
    {
        mgr = GetComponent<PlayerInputManager>();
        Debug.Log($"[JoinDebug] mgr={(mgr ? "OK" : "NULL")}");
    }

    void Start()
    {
        Debug.Log($"[JoinDebug] PlayerPrefab={(mgr.playerPrefab ? mgr.playerPrefab.name : "NULL")}");
        mgr.JoinPlayer();
        Debug.Log("[JoinDebug] JoinPlayer() called");
    }
}
