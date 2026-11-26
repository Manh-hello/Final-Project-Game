using UnityEngine;

/// <summary>Assign a player when gameplay is added. Without a target, preserves the opening view.</summary>
[RequireComponent(typeof(Camera))]
public sealed class ForestCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 mapMin = new Vector2(-5, -3);
    public Vector2 mapMax = new Vector2(106, 12);
    public Vector2 offset = new Vector2(2, 2.3f);
    public float smoothTime = 0.25f;
    Vector3 velocity;
    Camera sceneCamera;

    void Awake() { sceneCamera = GetComponent<Camera>(); }

    public void SnapToTarget()
    {
        if (!target) return;
        if (!sceneCamera) sceneCamera = GetComponent<Camera>();
        float halfHeight = sceneCamera.orthographicSize;
        float halfWidth = halfHeight * sceneCamera.aspect;
        transform.position = new Vector3(
            ClampToMap(target.position.x + offset.x, mapMin.x, mapMax.x, halfWidth),
            ClampToMap(target.position.y + offset.y, mapMin.y, mapMax.y, halfHeight),
            transform.position.z);
        velocity = Vector3.zero;
    }

    void LateUpdate()
    {
        if (!target) return;
        float halfHeight = sceneCamera.orthographicSize;
        float halfWidth = halfHeight * sceneCamera.aspect;
        Vector3 desired = new Vector3(
            ClampToMap(target.position.x + offset.x, mapMin.x, mapMax.x, halfWidth),
            ClampToMap(target.position.y + offset.y, mapMin.y, mapMax.y, halfHeight),
            transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    static float ClampToMap(float value, float min, float max, float halfView)
    {
        return max - min <= halfView * 2 ? (min + max) * 0.5f : Mathf.Clamp(value, min + halfView, max - halfView);
    }
}
