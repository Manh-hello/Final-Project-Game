using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIScreen : MonoBehaviour // Hiệu ứng hiển thị và ẩn màn hình, Script này sử dụng CanvasGroup và DOTween để tạo hiệu ứng Fade In / Fade Out
{
    CanvasGroup _cg; // CanvasGroup cho phép điều khiển đồng thời các thành phần UI con: alpha: Độ trong suốt, interactable: Cho phép tương tác với UI, blocksRaycasts: Cho phép UI nhận Raycast.
    CanvasGroup cg => _cg ? _cg : (_cg = GetComponent<CanvasGroup>());

    public virtual void Show()
    {
        gameObject.SetActive(true); // Kích hoạt GameObject
        cg.DOKill(); // Hủy tween cũ nếu đang chạy
        cg.alpha = 0; // Đặt alpha = 0 (trong suốt)
        cg.interactable = cg.blocksRaycasts = true; // Cho phép tương tác và nhận Raycast
        cg.DOFade(1f, 0.25f).SetUpdate(true); // DOTween tăng alpha từ 0 lên 1 trong 0.25 giây
    }

    public virtual void Hide(bool instant = false)
    {
        cg.DOKill(); // Hủy các animation DOTween đang chạy trên CanvasGroup
        cg.interactable = cg.blocksRaycasts = false; // Không cho phép tương tác với UI, Không nhận sự kiện Raycast
        if (instant || !gameObject.activeInHierarchy) // instant = true: Ẩn ngay lập tức.
        {
            cg.alpha = 0; 
            gameObject.SetActive(false);
            return;
        }
        cg.DOFade(0f, 0.15f).SetUpdate(true).OnComplete(() => gameObject.SetActive(false)); //Giảm độ trong suốt về 0 trong 0.15 giây, Tắt GameObject sau khi hiệu ứng kết thúc
    }
}