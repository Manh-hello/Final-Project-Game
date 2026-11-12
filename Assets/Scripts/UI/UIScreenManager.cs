using System.Collections.Generic;
using UnityEngine;

public enum ScreenId // Định nghĩa danh sách các màn hình có trong game.
{
    Hub, CharacterSelect, PlayerManagement, Profile,
    Inventory, Quest, Achievement, Bestiary, Settings, LoadingMap1
}

public class UIScreenManager : MonoBehaviour // Quản lý toàn bộ màn hình
{
    public static UIScreenManager Instance { get; private set; }

    [System.Serializable]
    public struct Entry { public ScreenId id; public UIScreen screen; }

    [SerializeField] List<Entry> screens;
    [SerializeField] ScreenId startScreen = ScreenId.Hub;

    readonly Dictionary<ScreenId, UIScreen> map = new(); // Dictionary giúp tìm màn hình nhanh chóng thông qua ID
    UIScreen current;

    void Awake()
    {
        Instance = this; // Gán Instance để các script khác có thể truy cập UIScreenManager
        foreach (var e in screens) // Duyệt qua danh sách screens được khai báo trong Inspector 
        {
            map[e.id] = e.screen; // Lưu từng màn hình vào Dictionary
            e.screen.Hide(true); // Ẩn tất cả màn hình ngay lập tức
        }
    }

    void Start() => Show(startScreen); // Khi game bắt đầu, hiển thị màn hình mặc định

    public void Show(ScreenId id) // Tìm màn hình cần hiển thị trong Dictionary
    {
        if (!map.TryGetValue(id, out var next)) { Debug.LogWarning($"Chưa gán màn hình {id}"); return; } // Nếu không tìm thấy, báo lỗi
        if (current == next) return; // Nếu màn hình đang mở chính là màn hình cần mở thì không làm gì
        if (current != null) current.Hide(); // Ẩn màn hình hiện tại
        current = next; // Cập nhật current
        current.Show(); // Hiển thị màn hình mới
    }
}