using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KeyboardP2Controller : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 6f;
    public float acceleration = 30f;

    [Header("2.5D lane")]
    public bool lockZ = true;
    public float fixedZ = 0f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;

        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
    }

    void FixedUpdate()
    {
        // P2: 矢印キー
        float x = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) x += 1f;

        // カメラ右方向を水平化して左右移動
        Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 targetVel = camRight * (x * moveSpeed);

        Vector3 vel = rb.linearVelocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);
        Vector3 newVelXZ = Vector3.MoveTowards(velXZ, targetVel, acceleration * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newVelXZ.x, vel.y, newVelXZ.z);

        if (lockZ)
        {
            Vector3 p = rb.position; p.z = fixedZ; rb.position = p;
            Vector3 v = rb.linearVelocity; v.z = 0f; rb.linearVelocity = v;
        }
    }
}
