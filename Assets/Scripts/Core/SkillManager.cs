using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[System.Serializable]
public class SkillSlot
{
    public string id;
    public GameObject effectPrefab;
    public Transform spawnPoint;
    public float cooldown = 2f;
    public string animTrigger = "Attack";
    public Image cooldownImage;
    public Key hotkey = Key.None;

    [Header("Di chuyển")]
    public bool moveForward = true;                     // false = đứng yên tại chỗ
    public float speed = 8f;                            // chỉ dùng khi Move Forward
    public float lifeTime = 1f;                         // 0 = dùng giá trị của prefab

    [Header("Hướng bay (x, y đều tịnh tiến)")]
    [FormerlySerializedAs("aimDirection")]
    public Vector2 aimDirectionRight = new Vector2(1f, -1f);   // khi Player nhìn phải
    public Vector2 aimDirectionLeft  = new Vector2(-1f, -1f);  // khi Player nhìn trái

    [Header("Kích thước (nhân với scale gốc của prefab)")]
    public bool overrideSize = false;
    public Vector2 size = Vector2.one;                  // X = dài, Y = rộng

    [Header("Góc xoay hình")]
    public bool overrideRotation = true;
    public Vector3 rotationFacingRight = new Vector3(0f, 0f, -110f);
    public Vector3 rotationFacingLeft  = new Vector3(180f, 0f, 60f);

    [HideInInspector] public float nextReadyTime;
}

public class SkillManager : MonoBehaviour
{
    public SkillSlot[] skills;
    [SerializeField] SpriteRenderer sprite;

    Animator anim;
    readonly Dictionary<string, SkillSlot> byId = new();

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (!sprite) sprite = GetComponent<SpriteRenderer>();

        foreach (var s in skills)
        {
            if (string.IsNullOrEmpty(s.id)) continue;
            if (byId.ContainsKey(s.id)) Debug.LogWarning($"Trùng id skill: {s.id}");
            else byId[s.id] = s;
        }
    }

    void Update()
    {
        var k = Keyboard.current;

        foreach (var s in skills)
        {
            if (s.cooldownImage != null)
            {
                float remain = s.nextReadyTime - Time.time;
                s.cooldownImage.fillAmount = s.cooldown > 0f ? Mathf.Clamp01(remain / s.cooldown) : 0f;
            }

            if (k != null && s.hotkey != Key.None && k[s.hotkey].wasPressedThisFrame)
                Cast(s);
        }
    }

    public void UseById(string id)
    {
        if (byId.TryGetValue(id, out var s)) Cast(s);
        else Debug.LogWarning($"Không có skill id: {id}");
    }

    public void UseByIndex(int index)
    {
        if (index >= 0 && index < skills.Length) Cast(skills[index]);
    }

    void Cast(SkillSlot s)
    {
        if (s.effectPrefab == null || s.spawnPoint == null) return;
        if (Time.time < s.nextReadyTime) return;
        s.nextReadyTime = Time.time + s.cooldown;

        if (anim != null && !string.IsNullOrEmpty(s.animTrigger)) anim.SetTrigger(s.animTrigger);

        bool facingRight = !(sprite != null && sprite.flipX);
        float facing = facingRight ? 1f : -1f;

        // Vị trí ra chiêu: lật x của spawnPoint theo hướng nhìn
        Vector3 offset = s.spawnPoint.position - transform.position;
        offset.x = Mathf.Abs(offset.x) * facing;
        Vector3 pos = transform.position + offset;
        pos.z = 0f;

        var fx = Instantiate(s.effectPrefab, pos, Quaternion.identity);

        var sr = fx.GetComponent<SpriteRenderer>();
        if (sr != null && sprite != null)
        {
            sr.sortingLayerID = sprite.sortingLayerID;
            sr.sortingOrder = sprite.sortingOrder + 5;
            sr.color = Color.white;
        }

        var rb2 = fx.GetComponent<Rigidbody2D>();
        if (rb2 != null) rb2.bodyType = RigidbodyType2D.Kinematic;

        if (s.overrideSize)
        {
            var baseScale = s.effectPrefab.transform.localScale;
            fx.transform.localScale = new Vector3(
                Mathf.Abs(baseScale.x) * s.size.x,
                Mathf.Abs(baseScale.y) * s.size.y,
                baseScale.z);
        }

        var effect = fx.GetComponent<SkillEffect>();
        if (effect == null) return;

        effect.speed = s.moveForward ? s.speed : 0f;
        if (s.lifeTime > 0f) effect.lifeTime = s.lifeTime;

        // Hướng bay: lấy trực tiếp theo bên trái / phải
        Vector2 dir = facingRight ? s.aimDirectionRight : s.aimDirectionLeft;
        effect.Launch(dir);

        if (s.overrideRotation)
        {
            fx.transform.rotation = Quaternion.Euler(
                facingRight ? s.rotationFacingRight : s.rotationFacingLeft);
        }
    }
}