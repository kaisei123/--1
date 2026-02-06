using UnityEngine;

public class Hurtbox : MonoBehaviour
{
    public FighterController controller;
    public FighterHealth health;

    private void Awake()
    {
        if (!controller) controller = GetComponentInParent<FighterController>();
        if (!health) health = GetComponentInParent<FighterHealth>();
    }

    private void OnTriggerEnter(Collider other)
    {
        var hitbox = other.GetComponentInParent<Hitbox>();
        if (hitbox == null) return;

        // デバッグ（当たってるか）
        Debug.Log($"HURTBOX HIT by {hitbox.name}");

        hitbox.TryHit(this);
    }
}
