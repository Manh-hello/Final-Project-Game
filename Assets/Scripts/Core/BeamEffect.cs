
using UnityEngine;

public class BeamEffect : MonoBehaviour
{
    public LayerMask hitLayers;
    public float maxLength = 12f;
    public float growSpeed = 25f;

    private SpriteRenderer sr;
    private Vector2 origin;

    private float spriteLength;
    private float originalScaleX;
    private float originalScaleY;
    private float originalScaleZ;
    private float currentLength;

    private Vector3 originalChildPosition;

    void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();

        spriteLength = sr.sprite.bounds.size.x;

        originalScaleX = transform.localScale.x;
        originalScaleY = transform.localScale.y;
        originalScaleZ = transform.localScale.z;

        originalChildPosition = sr.transform.localPosition;

        origin = transform.position;

        currentLength = growSpeed > 0f ? 0f : maxLength;
    }

    void LateUpdate()
    {
        // Hướng tia theo trục X
        Vector2 dir = transform.right;

        RaycastHit2D hit = Physics2D.Raycast(
            origin, dir, maxLength, hitLayers
        );

        float limit = hit.collider != null
            ? hit.distance
            : maxLength;

        currentLength = growSpeed > 0f
            ? Mathf.Min(
                currentLength + growSpeed * Time.deltaTime,
                limit
            )
            : limit;

        // Scale X
        float scaleX = currentLength /
                       Mathf.Max(spriteLength, 0.0001f);

        transform.localScale = new Vector3(
            scaleX,
            originalScaleY,
            originalScaleZ
        );

        // Bù vị trí Sprite con nếu Pivot nằm giữa
        sr.transform.localPosition = new Vector3(
            originalChildPosition.x + spriteLength * 0.5f,
            originalChildPosition.y,
            originalChildPosition.z
        );
    }
}
