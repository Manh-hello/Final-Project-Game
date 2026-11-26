using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SkillEffect : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 3f;
    public int damage = 10;
    public LayerMask hitLayers;
    public bool pierceEnemy = false;

    [HideInInspector] public Vector2 direction = Vector2.right;

    // Gọi ngay sau Instantiate: đặt hướng bay + góc xoay hình
    public void Launch(Vector2 dir)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    void Start() => Destroy(gameObject, lifeTime);

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        if (other.CompareTag("Enemy"))
        {
            // other.GetComponent<EnemyHealth>()?.TakeDamage(damage);
            if (pierceEnemy) return;
        }

        if (speed > 0f) Destroy(gameObject);
    }
}