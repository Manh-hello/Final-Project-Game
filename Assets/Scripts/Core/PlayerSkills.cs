using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[System.Serializable]
public class Skill
{
    public string name;
    public GameObject effectPrefab;
    public Transform spawnPoint;     // vị trí ra chiêu (miệng rồng)
    public float cooldown = 3f;
    public string animTrigger = "Attack";
    public Image cooldownImage;      // ảnh phủ hiển thị hồi chiêu
    public Vector2 aimDirection = new Vector2(1f, -1f); // hướng bắn khi nhìn sang phải (x, y)
    [HideInInspector] public float nextReadyTime;
}

public class PlayerSkills : MonoBehaviour
{
    public Skill[] skills;
    [SerializeField] SpriteRenderer sprite;
    Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        foreach (var s in skills)
        {
            if (s.cooldownImage == null) continue;
            float remain = s.nextReadyTime - Time.time;
            s.cooldownImage.fillAmount = Mathf.Clamp01(remain / s.cooldown);
        }

        var k = Keyboard.current;
        if (k == null) return;
        if (k.jKey.wasPressedThisFrame) UseSkill(0);
        if (k.kKey.wasPressedThisFrame) UseSkill(1);
    }

    public void UseSkill(int index)
    {
        if (index < 0 || index >= skills.Length) return;
        var s = skills[index];
        if (s.effectPrefab == null || s.spawnPoint == null) return;
        if (Time.time < s.nextReadyTime) return;
        s.nextReadyTime = Time.time + s.cooldown;

        if (anim != null) anim.SetTrigger(s.animTrigger);

        // Hướng nhìn: sprite gốc nhìn sang phải, flipX = true là nhìn trái
        float facing = (sprite != null && sprite.flipX) ? -1f : 1f;

        // Vị trí ra chiêu: lấy offset của spawnPoint so với Player, lật x theo hướng nhìn
        Vector3 offset = s.spawnPoint.position - transform.position;
        offset.x = Mathf.Abs(offset.x) * facing;
        Vector3 pos = transform.position + offset;
        pos.z = 0f;

        var fx = Instantiate(s.effectPrefab, pos, Quaternion.identity);

        // Ép hiệu ứng vẽ lên trên Player
        var sr = fx.GetComponent<SpriteRenderer>();
        var playerSr = GetComponent<SpriteRenderer>();
        if (sr != null && playerSr != null)
        {
            sr.sortingLayerID = playerSr.sortingLayerID;
            sr.sortingOrder = playerSr.sortingOrder + 5;
            sr.color = Color.red;
        }

        var rb2 = fx.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.bodyType = RigidbodyType2D.Kinematic;

        // Đặt hướng bay + xoay hình theo hướng bay
        var effect = fx.GetComponent<SkillEffect>();
        if (effect != null) effect.Launch(GetAimDirection(s, facing, pos));
    }

    Vector2 GetAimDirection(Skill s, float facing, Vector3 from)
    {
        // HIỆN TẠI: hướng cố định, lật x theo hướng nhìn của player
        return new Vector2(s.aimDirection.x * facing, s.aimDirection.y);

        // SAU NÀY: nhắm enemy gần nhất. Bỏ dòng return ở trên và dùng:
        // var target = FindNearestEnemy(from);
        // if (target != null) return (Vector2)(target.position - from);
        // return new Vector2(s.aimDirection.x * facing, s.aimDirection.y);
    }

    Transform FindNearestEnemy(Vector3 from, float range = 10f)
    {
        Transform best = null;
        float bestDist = range;
        foreach (var e in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float d = Vector2.Distance(from, e.transform.position);
            if (d < bestDist) { bestDist = d; best = e.transform; }
        }
        return best;
    }
}