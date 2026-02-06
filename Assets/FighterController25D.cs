using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2.5D格闘ゲーム用の最小コントローラ
/// - 2.5D化: Z固定 (任意)
/// - 移動: カメラの right を水平化した方向 (画面左右)
/// - 入力:
///    A) PlayerInput (Send Messages) で OnMove/OnJump/OnAttack を受ける (使ってもOK)
///    B) 同キーボード2人用: KeyboardFighterInput から
///       SetMove / QueueJump / QueueAttack を呼ぶ (推奨)
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class FighterController25D : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;   // 未設定ならCamera.mainを拾う
    public Transform model;             // 見た目（子のモデル）
    public Transform groundCheck;       // 足元のEmpty
    public LayerMask groundLayers;      // Groundレイヤー

    [Header("2.5D Lane")]
    public bool lockZ = true;
    public float fixedZ = 0f;

    [Header("Move")]
    public float moveSpeed = 6f;
    public float acceleration = 30f;

    [Header("Jump")]
    public float jumpForce = 6.5f;
    public float groundCheckRadius = 0.18f;

    [Header("Attack (debug)")]
    public Transform hitBoxCenter;
    public Vector3 hitBoxSize = new Vector3(1.5f, 1.5f, 1.0f);
    public LayerMask hittableLayers;
    public float attackCooldown = 0.25f;

    [Header("Opponent")]
    public Transform opponent;

    [Header("Runtime")]
    public bool CanMove = true;         // 戦闘側からロックしたいとき用（任意）

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool jumpQueued;
    private bool attackQueued;
    private bool isGrounded;
    private float lastAttackTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        // 2.5Dの基本：倒れない
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        Debug.Log($"[FighterController25D] Awake rb={rb != null} cam={cameraTransform != null}");
    }

    void Update()
    {
        // 接地判定
        isGrounded = groundCheck
            ? Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore)
            : false;

        if (jumpQueued && isGrounded)
        {
            Jump();
            jumpQueued = false;
        }

        if (attackQueued)
        {
            TryAttack();
            attackQueued = false;
        }

        FaceOpponent();
    }

    void FixedUpdate()
    {
        if (lockZ)
        {
            // Z固定（物理のズレ防止）
            Vector3 p = rb.position;
            p.z = fixedZ;
            rb.position = p;

            Vector3 v = rb.linearVelocity;
            v.z = 0f;
            rb.linearVelocity = v;
        }

        Move();
    }

    // =========================
    // 入力の受け口（推奨）
    // KeyboardFighterInput から呼ぶ
    // =========================
    public void SetMove(Vector2 v) => moveInput = v;
    public void QueueJump() => jumpQueued = true;
    public void QueueAttack() => attackQueued = true;

    // =========================
    // PlayerInput (Send Messages)
    // 使う場合は PlayerInput を同じGameObjectに付ける
    // =========================
    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        // Debug.Log($"[{name}] OnMove called: {moveInput}");
    }

    void OnJump(InputValue value)
    {
        if (value.isPressed) jumpQueued = true;
    }

    void OnAttack(InputValue value)
    {
        if (value.isPressed) attackQueued = true;
    }

    // =========================

    public void SetOpponent(Transform t) => opponent = t;

    void Move()
    {
        if (!CanMove) return;
        if (!cameraTransform) return;

        // カメラ右方向を水平化（y=0）
        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude < 0.0001f) return;
        camRight.Normalize();

        // 左右移動（moveInput.x）
        Vector3 targetVel = camRight * (moveInput.x * moveSpeed);

        Vector3 vel = rb.linearVelocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

        Vector3 newVelXZ = Vector3.MoveTowards(velXZ, targetVel, acceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newVelXZ.x, vel.y, newVelXZ.z);
    }

    void Jump()
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
    }

    void FaceOpponent()
    {
        if (!model || !opponent || !cameraTransform) return;

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude < 0.0001f) return;
        camRight.Normalize();

        Vector3 toOpp = opponent.position - transform.position;
        toOpp.y = 0f;

        float side = Vector3.Dot(toOpp, camRight); // +なら右、-なら左
        if (Mathf.Abs(side) < 0.001f) return;

        Vector3 faceDir = camRight * Mathf.Sign(side);
        model.forward = faceDir;
    }


    void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        if (!hitBoxCenter)
        {
            Debug.LogWarning($"[{name}] hitBoxCenter is not set");
            return;
        }

        // まずは全レイヤー・Trigger含むで確実に拾う（原因切り分けが終わるまでこれで固定）
        Collider[] hits = Physics.OverlapBox(
            hitBoxCenter.position,
            hitBoxSize * 0.5f,
            hitBoxCenter.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        Debug.Log($"[ATTACK] {name} hits={hits.Length}");

        foreach (var h in hits)
        {
            // 自分自身は除外（ここ重要）
            if (h.transform.root == transform) continue;

            Debug.Log($"[HIT] {name} -> {h.name}");

            // ここで「相手」にだけ反応させる
            var health = h.GetComponentInParent<FighterHealth>();
            if (health != null)
            {
                // 仮：HP減らすだけ（ノックバックは後で）
                health.TakeHit(10, Vector3.zero);
                break;
            }
        }
    }



    void OnDrawGizmosSelected()
    {
        // 元の行列を保存
        Matrix4x4 oldMatrix = Gizmos.matrix;

        if (groundCheck)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (hitBoxCenter)
        {
            Gizmos.matrix = Matrix4x4.TRS(
                hitBoxCenter.position,
                hitBoxCenter.rotation,
                Vector3.one
            );
            Gizmos.DrawWireCube(Vector3.zero, hitBoxSize);
        }

        // ★必ず元に戻す
        Gizmos.matrix = oldMatrix;
    }

}
