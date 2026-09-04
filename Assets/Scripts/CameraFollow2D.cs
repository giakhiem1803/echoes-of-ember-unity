using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothness = 5f;
    [SerializeField] private float minX = -7f;
    [SerializeField] private float maxX = 58f;
    public void SetTarget(Transform value) => target = value;
    private void LateUpdate()
    {
        if (target == null) return;
        float x = Mathf.Clamp(target.position.x + 2.2f, minX, maxX);
        transform.position = Vector3.Lerp(transform.position, new Vector3(x, 0f, -10f), smoothness * Time.deltaTime);
    }
}
