using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public AttackData attackData;
    public FighterController owner;

    private readonly HashSet<Hurtbox> alreadyHit = new HashSet<Hurtbox>();

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<FighterController>();
        GetComponent<Collider>().enabled = false;
    }

    public void BeginAttack(AttackData data)
    {
        attackData = data;
        alreadyHit.Clear();
        GetComponent<Collider>().enabled = true;
    }

    public void EndAttack()
    {
        GetComponent<Collider>().enabled = false;
    }

    public void TryHit(Hurtbox target)
    {
        if (attackData == null) return;
        if (target == null) return;

        // 自分自身に当てない
        if (target.controller == owner) return;

        // 多段ヒット防止
        if (alreadyHit.Contains(target)) return;
        alreadyHit.Add(target);

        // ダメージ
        target.health?.TakeDamage(attackData.damage);

        // ノックバック方向（攻撃者→被弾者）
        Vector3 dir = (target.transform.position - owner.transform.position);
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : owner.Forward;

        // ノックバック（FighterController側に ApplyKnockback が必要）
        target.controller?.ApplyKnockback(dir * attackData.knockback, attackData.hitstun);

        Debug.Log($"HIT! {owner.name} -> {target.name} dmg={attackData.damage}");

    }
}
