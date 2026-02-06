using UnityEngine;

public class FighterHealth : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP = 100;

    void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"{name} took {damage} damage");
    }

    public void TakeHit(int damage, Vector3 knockback)
    {
        currentHP -= damage;
        Debug.Log($"{name} HP = {currentHP}");

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(knockback, ForceMode.Impulse);
        }
    }
}
