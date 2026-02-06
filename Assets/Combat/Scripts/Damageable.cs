using UnityEngine;

public class Damageable : MonoBehaviour
{
    public FighterController owner;
    public Rigidbody rb;
    public int hp = 100;

    public void TakeHit(FighterController attacker, AttackData atk)
    {
        bool guarding = owner.IsGuarding;
        bool facingAttacker = Vector3.Dot(owner.Forward, (attacker.transform.position - owner.transform.position).normalized) > 0.2f;

        bool blocked = guarding && facingAttacker && atk.canBeBlocked;

        if (atk.isGrab)
        {
            ApplyDamage(atk.damage);
            ApplyKnockback(attacker, atk.knockback);
            owner.EnterHitstun(atk.hitstun);
            return;
        }

        if (blocked)
        {
            // ガード成功
            ApplyDamage(0);
            ApplyKnockback(attacker, atk.knockback * 0.3f);
            owner.EnterBlockstun(atk.blockstun);
        }
        else
        {
            // 通常ヒット
            ApplyDamage(atk.damage);
            ApplyKnockback(attacker, atk.knockback);
            owner.EnterHitstun(atk.hitstun);
        }
    }

    void ApplyDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0) owner.Die();
    }

    void ApplyKnockback(FighterController attacker, float power)
    {
        Vector3 dir = (transform.position - attacker.transform.position);
        dir.y = 0f;
        dir = dir.sqrMagnitude < 0.0001f ? attacker.Forward : dir.normalized;

        // 横だけ飛ばす（格ゲーっぽく安定）
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.AddForce(dir * power, ForceMode.VelocityChange);
    }
}

