using UnityEngine;

/// <summary>Small visual motion for magical forest decorations; never moves collision geometry.</summary>
public sealed class ForestAmbient : MonoBehaviour
{
    public float amplitude = 0.06f;
    public float speed = 1.2f;
    public float phase;
    public bool floatVertically = true;
    Vector3 origin;
    SpriteRenderer spriteRenderer;
    Color originalColor;

    void Awake()
    {
        origin = transform.localPosition;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        float wave = Mathf.Sin(Time.time * speed + phase);
        if (floatVertically) transform.localPosition = origin + Vector3.up * (wave * amplitude);
        if (spriteRenderer)
        {
            Color color = originalColor;
            color.a *= 0.78f + 0.22f * wave;
            spriteRenderer.color = color;
        }
    }
}
