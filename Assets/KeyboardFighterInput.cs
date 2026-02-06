using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardFighterInput : MonoBehaviour
{
    public InputActionAsset actions;
    public string actionMapName = "Gameplay_P1"; // P1/P2で変える
    public FighterController25D controller;

    InputAction move;
    InputAction jump;
    InputAction attack;

    void Awake()
    {
        controller = GetComponent<FighterController25D>();
    }

    void OnEnable()
    {
        var map = actions.FindActionMap(actionMapName, throwIfNotFound: true);
        map.Enable();

        move = map.FindAction("Move", throwIfNotFound: true);
        jump = map.FindAction("Jump", throwIfNotFound: true);
        attack = map.FindAction("Attack", throwIfNotFound: true);
    }

    void OnDisable()
    {
        var map = actions.FindActionMap(actionMapName, false);
        map?.Disable();
    }

    void Update()
    {
        controller.SetMove(move.ReadValue<Vector2>());

        if (jump.WasPressedThisFrame()) controller.QueueJump();
        if (attack.WasPressedThisFrame()) controller.QueueAttack();
    }
}
