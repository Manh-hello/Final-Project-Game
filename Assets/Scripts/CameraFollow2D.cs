using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] SpriteRenderer bounds; // kéo Background vào
    [SerializeField] float smooth = 8f;
    [SerializeField] float yOffset = 1f;

    Camera cam;
    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (!target || !bounds) return;

        float h = cam.orthographicSize;
        float w = h * cam.aspect;
        Bounds b = bounds.bounds;

        float x = Mathf.Clamp(target.position.x, b.min.x + w, b.max.x - w);
        float y = Mathf.Clamp(target.position.y + yOffset, b.min.y + h, b.max.y - h);

        Vector3 goal = new Vector3(x, y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }
}