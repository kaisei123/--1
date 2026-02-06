using UnityEngine;

public class FaceOpponentSimple : MonoBehaviour
{
    public Transform opponent;

    [Header("Auto-assign if empty")]
    public Transform model;            // 空なら自動で探す
    public Transform cameraTransform;  // 空なら Camera.main を拾う

    void OnValidate()
    {
        AutoAssign();
    }

    void Awake()
    {
        AutoAssign();
    }

    void AutoAssign()
    {
        // model が未設定なら、子の "Model" を探す
        if (!model)
        {
            var found = transform.Find("Model");
            if (found) model = found;
        }

        // cameraTransform が未設定なら MainCamera を拾う
        if (!cameraTransform && Camera.main)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (!opponent || !model || !cameraTransform) return;

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 toOpp = opponent.position - transform.position;
        toOpp.y = 0f;

        float side = Vector3.Dot(toOpp, camRight);
        if (Mathf.Abs(side) < 0.001f) return;

        model.forward = camRight * Mathf.Sign(side);
    }
}
