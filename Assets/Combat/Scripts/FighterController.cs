using System.Collections;
using UnityEngine;

public enum FighterState { Idle, Move, Guard, Attack, Hitstun, Blockstun, Dead }
public enum PlayerId { P1, P2 }

public class FighterController : MonoBehaviour
{
    [Header("Player")]
    public PlayerId playerId = PlayerId.P1;

    [Header("Refs")]
    public Transform opponent;
    public Rigidbody rb;
    public Hitbox hitbox;

    [Header("Animation")]
    public Animator animator;   // ★追加

    [Header("Ground")]
    public Transform groundCheck;              // 足元のEmpty（無ければ自動作成はしないので手動推奨）
    public float groundCheckRadius = 0.18f;
    public LayerMask groundLayers = ~0;
    public float jumpVelocity = 6.5f;

    [Header("Grab / Hurtbox")]
    public LayerMask hurtboxLayers = ~0; // とりあえず全部（後でHurtboxレイヤーに絞ると良い）

    [Header("Moves (AttackData)")]
    public AttackData jab;
    public AttackData poke;
    public AttackData heavy;
    public AttackData grab;

    [Header("Tuning")]
    public float moveSpeed = 5f;

    [Header("Debug")]
    public bool debugLogs = true;

    public FighterState state = FighterState.Idle;
    public bool IsGuarding => state == FighterState.Guard;
    public Vector3 Forward { get; private set; } // 相手方向（地面投影）

    // --- cached ---
    Transform hitboxAnchor;     // Hitboxの親（Empty）
    SphereCollider hitboxCol;   // Hitboxに付いてるSphereCollider

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!hitbox) hitbox = GetComponentInChildren<Hitbox>(true);
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true); // ★追加


        if (hitbox != null)
        {
            hitboxCol = hitbox.GetComponent<SphereCollider>();
            hitboxAnchor = hitbox.transform.parent; // HitboxAnchor想定
        }

        Forward = transform.forward;

        if (debugLogs) Debug.Log($"[{name}] FighterController Awake playerId={playerId} rb={(rb != null)} hitbox={(hitbox != null)}");
    }

    void Update()
    {

        if (state == FighterState.Dead) return;

        UpdateFacingAxis();

        // ★アニメパラメータは常に更新（returnより前）
        if (animator != null)
        {
            float mx = ReadMoveX();               // 入力を先に読む
            animator.SetFloat("Speed", Mathf.Abs(mx)); // ←パラメータ名が speed ならこれ
            animator.SetBool("IsGrounded", IsGrounded()); // 任意（あるなら）
        }

        // 硬直中は入力を受けない
        if (state == FighterState.Hitstun || state == FighterState.Blockstun || state == FighterState.Attack)
            return;

        // ※ここ以降は今まで通り

        // Update動作確認（うるさければdebugLogsをOFF）
        if (debugLogs && Time.frameCount % 60 == 0)
            Debug.Log($"[{name}] Update running state={state}");

        if (state == FighterState.Dead) return;

        UpdateFacingAxis();

        // 硬直中は入力を受けない
        if (state == FighterState.Hitstun || state == FighterState.Blockstun || state == FighterState.Attack)
            return;

        // 入力取得（P1/P2で分離）
        float moveX = ReadMoveX();
        bool jumpDown = ReadJumpDown();
        bool guardHeld = ReadGuardHeld();

        bool atk1 = ReadAtk1Down();
        bool atk2 = ReadAtk2Down();
        bool atk3 = ReadAtk3Down();
        bool grabDown = ReadGrabDown();

        // ガード状態
        if (guardHeld)
        {
            state = FighterState.Guard;
        }
        else if (state == FighterState.Guard)
        {
            state = FighterState.Idle;
        }

        // ジャンプ（ガード中でもジャンプさせないなら if (state!=Guard) で囲ってね）
        if (jumpDown)
        {
            TryJump();
        }

        // 攻撃（ガード中は出さない）
        if (state != FighterState.Guard)
        {
            if (atk1) StartCoroutine(DoAttack(jab, "Jab"));
            if (atk2) StartCoroutine(DoAttack(poke, "Poke"));
            if (atk3) StartCoroutine(DoAttack(heavy, "Heavy"));
            if (grabDown) StartCoroutine(DoGrab(grab, "Grab"));
        }

        // 移動（攻撃・硬直中は上でreturn済み）
        MoveSideways(moveX);
    }

    // ========= Input (テンキー無し版) =========

    float ReadMoveX()
    {
        float x = 0f;

        if (playerId == PlayerId.P1)
        {
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
        }

        return x;
    }

    bool ReadJumpDown()
    {
        if (playerId == PlayerId.P1) return Input.GetKeyDown(KeyCode.W);
        return Input.GetKeyDown(KeyCode.UpArrow);
    }

    bool ReadGuardHeld()
    {
        if (playerId == PlayerId.P1) return Input.GetKey(KeyCode.LeftShift);
        return Input.GetKey(KeyCode.RightControl); // P2ガード
    }

    bool ReadAtk1Down()
    {
        if (playerId == PlayerId.P1) return Input.GetKeyDown(KeyCode.F);
        return Input.GetKeyDown(KeyCode.Semicolon); // ;
    }

    bool ReadAtk2Down()
    {
        if (playerId == PlayerId.P1) return Input.GetKeyDown(KeyCode.G);
        return Input.GetKeyDown(KeyCode.Quote); // '
    }

    bool ReadAtk3Down()
    {
        if (playerId == PlayerId.P1) return Input.GetKeyDown(KeyCode.H);
        return Input.GetKeyDown(KeyCode.Return); // Enter
    }

    bool ReadGrabDown()
    {
        if (playerId == PlayerId.P1) return Input.GetKeyDown(KeyCode.R);
        return Input.GetKeyDown(KeyCode.Slash); // /
    }


    // ========= Facing / Movement =========

    void UpdateFacingAxis()
    {
        if (!opponent)
        {
            if (Forward == Vector3.zero) Forward = transform.forward;
            return;
        }

        Vector3 axis = opponent.position - transform.position;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.0001f) axis = transform.forward;

        Forward = axis.normalized;

        // 見た目は相手方向を向く（不要なら外してOK）
        transform.forward = Forward;
    }

    void MoveSideways(float input)
    {
        if (rb == null) return;

        // 画面/世界の左右を固定軸にする（おすすめ）
        Vector3 axis = Vector3.right;   // 2.5Dならこれが一番安定

        Vector3 delta = axis * (input * moveSpeed * Time.deltaTime);
        rb.MovePosition(rb.position + delta);

        if (state == FighterState.Idle && Mathf.Abs(input) > 0.01f) state = FighterState.Move;
        if (state == FighterState.Move && Mathf.Abs(input) <= 0.01f) state = FighterState.Idle;
    }


    bool IsGrounded()
    {
        if (!groundCheck) return true; // groundCheck未設定なら仮でtrue（後で必ず入れる推奨）
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    void TryJump()
    {
        if (rb == null) return;
        if (!IsGrounded()) return;

        // Unity6: linearVelocity 推奨（velocityでも動く）
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);

        if (debugLogs) Debug.Log($"[{name}] Jump!");
    }

    // ========= Attacks =========

    IEnumerator DoAttack(AttackData atk, string label)
    {
        if (atk == null)
        {
            Debug.LogWarning($"[{name}] {label} AttackData is NULL (InspectorのMovesに入れて！)");
            yield break;
        }
        if (hitbox == null)
        {
            Debug.LogWarning($"[{name}] hitbox is NULL (子にHitboxある？)");
            yield break;
        }

        if (debugLogs) Debug.Log($"[{name}] DoAttack start: {atk.name}");

        state = FighterState.Attack;

        if (animator != null)
        {
            animator.ResetTrigger("Attack"); // 念のため
            animator.SetTrigger("Attack");   // ★攻撃アニメ開始
        }

        // startup
        yield return new WaitForSeconds(atk.startup);

        // active
        PositionHitbox(atk);

        // ★ここ：毎回新しい攻撃として初期化するためON
        hitbox.gameObject.SetActive(true);

        if (debugLogs) Debug.Log($"[{name}] Hitbox ON ({atk.name})");
        hitbox.BeginAttack(atk);

        yield return new WaitForSeconds(atk.active);

        hitbox.EndAttack();

        // ★ここ：確実にOFF（次の攻撃でOnEnableが走る）
        hitbox.gameObject.SetActive(false);

        if (debugLogs) Debug.Log($"[{name}] Hitbox OFF ({atk.name})");

        // recovery
        yield return new WaitForSeconds(atk.recovery);

        if (state == FighterState.Attack) state = FighterState.Idle;
    }

    IEnumerator DoGrab(AttackData atk, string label)
    {
        if (atk == null)
        {
            Debug.LogWarning($"[{name}] {label} AttackData is NULL (InspectorのMovesに入れて！)");
            yield break;
        }

        state = FighterState.Attack;

        yield return new WaitForSeconds(atk.startup);

        TryGrab(atk);

        yield return new WaitForSeconds(atk.recovery);

        if (state == FighterState.Attack) state = FighterState.Idle;
    }

    void PositionHitbox(AttackData atk)
    {
        if (hitboxAnchor == null || hitboxCol == null) return;

        Vector3 f = (Forward == Vector3.zero) ? transform.forward : Forward;

        // 高さは胸〜腹付近（VRMにしたら微調整）
        Vector3 pos = transform.position + Vector3.up * 1.0f + f * atk.range;

        hitboxAnchor.position = pos;
        hitboxCol.radius = atk.radius;
        hitboxCol.center = Vector3.zero;
        hitboxCol.isTrigger = true;
    }

    void TryGrab(AttackData atk)
    {
        Vector3 f = (Forward == Vector3.zero) ? transform.forward : Forward;
        Vector3 center = transform.position + Vector3.up * 1.0f + f * atk.range;

        Collider[] hits = Physics.OverlapSphere(center, atk.radius, hurtboxLayers, QueryTriggerInteraction.Collide);

        foreach (var h in hits)
        {
            var hb = h.GetComponentInParent<Hurtbox>();
            if (hb == null) continue;
            if (hb.controller == this) continue;

            ApplyHitToHurtbox(hb, atk);
            break;
        }
    }

    void ApplyHitToHurtbox(Hurtbox target, AttackData atk)
    {
        if (target == null) return;

        // ダメージ
        target.health?.TakeDamage(atk.damage);

        // ===== ノックバック方向を「画面左右（世界X）」で決める =====
        Vector3 axis = Vector3.right; // ← 画面右方向が正

        Vector3 ownerToTarget = target.transform.position - transform.position;

        // どっち側に相手がいるか
        float sign = Mathf.Sign(Vector3.Dot(ownerToTarget, axis));
        if (sign == 0) sign = 1f;

        Vector3 dir = axis * sign;

        // ノックバック適用
        target.controller?.ApplyKnockback(dir * atk.knockback, atk.hitstun);
    }


    // ========= Stun / Knockback =========

    public void ApplyKnockback(Vector3 velocity, float stunSeconds)
    {
        StopAllCoroutines();
        hitbox?.EndAttack();

        state = FighterState.Hitstun;

        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);

        CancelInvoke(nameof(EndStun));
        Invoke(nameof(EndStun), stunSeconds);

        if (debugLogs) Debug.Log($"[{name}] Knockback applied v={velocity} stun={stunSeconds}");
    }

    public void EnterHitstun(float t)
    {
        StopAllCoroutines();
        hitbox?.EndAttack();
        state = FighterState.Hitstun;

        CancelInvoke(nameof(EndStun));
        Invoke(nameof(EndStun), t);
    }

    public void EnterBlockstun(float t)
    {
        StopAllCoroutines();
        hitbox?.EndAttack();
        state = FighterState.Blockstun;

        CancelInvoke(nameof(EndStun));
        Invoke(nameof(EndStun), t);
    }

    void EndStun()
    {
        if (state == FighterState.Hitstun || state == FighterState.Blockstun)
            state = FighterState.Idle;
    }

    public void Die()
    {
        StopAllCoroutines();
        hitbox?.EndAttack();
        state = FighterState.Dead;
    }
}
