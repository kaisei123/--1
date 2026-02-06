using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform p1Spawn;
    public Transform p2Spawn;

    [Header("P2 Auto Spawn (A案)")]
    public GameObject p2Prefab;     // PlayerP2.prefab

    [Header("Camera")]
    public Transform mainCamera;     // 未設定なら Camera.main

    // JoinしたPlayer（P1用）
    private readonly Dictionary<int, PlayerInput> players = new();

    // P2（仮プレイヤー）
    private Transform p2Transform;

    void Awake()
    {
        if (!mainCamera && Camera.main)
            mainCamera = Camera.main.transform;
    }

    void Start()
    {
        // --- A案：P2を開始時にスポーン ---
        if (p2Prefab && p2Spawn)
        {
            GameObject p2Obj = Instantiate(
                p2Prefab,
                p2Spawn.position,
                p2Spawn.rotation
            );

            p2Transform = p2Obj.transform;

            // P2側にカメラ参照を渡す（あれば）
            var p2Ctrl = p2Transform.GetComponent<KeyboardP2Controller>();
            if (p2Ctrl && mainCamera)
                p2Ctrl.cameraTransform = mainCamera;

            Debug.Log("[GameManager] P2 auto-spawned.");
        }
        else
        {
            Debug.LogWarning("[GameManager] P2Prefab or P2Spawn is not set.");
        }
    }

    // ★ PlayerInputManager の Player Joined Event から呼ばれる
    public void OnPlayerJoined(PlayerInput player)
    {
        if (player == null) return;

        players[player.playerIndex] = player;

        // --- P1は左スポーン ---
        if (player.playerIndex == 0 && p1Spawn)
        {
            player.transform.SetPositionAndRotation(
                p1Spawn.position,
                p1Spawn.rotation
            );
        }

        // カメラ参照をP1コントローラに渡す
        var p1Ctrl = player.GetComponent<FighterController25D>();
        if (p1Ctrl && mainCamera)
            p1Ctrl.cameraTransform = mainCamera;

        TryLinkOpponents();

        Debug.Log($"[GameManager] Player joined: index={player.playerIndex}");
    }

    public void OnPlayerLeft(PlayerInput player)
    {
        if (player == null) return;
        players.Remove(player.playerIndex);
    }

    // --- P1とP2を相互に結びつける ---
    void TryLinkOpponents()
    {
        if (!players.ContainsKey(0) || p2Transform == null)
            return;

        Transform p1Transform = players[0].transform;

        // P1（Input System側）
        var p1Ctrl = p1Transform.GetComponent<FighterController25D>();
        if (p1Ctrl)
            p1Ctrl.SetOpponent(p2Transform);

        // P2（仮キーボード側）
        var p2Face = p2Transform.GetComponent<FaceOpponentSimple>();
        if (p2Face)
            p2Face.opponent = p1Transform;

        Debug.Log("[GameManager] Opponents linked.");
    }
}
