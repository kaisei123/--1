using UnityEngine;

[CreateAssetMenu(menuName = "Combat/AttackData")]
public class AttackData : ScriptableObject
{
    public string attackName;

    public float startup = 0.08f;
    public float active = 0.05f;
    public float recovery = 0.12f;

    public int damage = 5;
    public float knockback = 6f;
    public float hitstun = 0.15f;
    public float blockstun = 0.10f;

    public bool canBeBlocked = true;
    public bool isGrab = false;

    public float range = 1.2f;
    public float radius = 0.35f;
}
