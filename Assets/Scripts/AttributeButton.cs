using DG.Tweening;
using UnityEngine;

public class AttributeButton : MonoBehaviour
{
    [SerializeField] private GameObject selectImage;   // 선택됨 이미지
    
    private Tween selectTween;

    public void Select()
    {
        if (selectImage != null)
            selectImage.SetActive(true);

        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void UnSelect()
    {
        if (selectImage != null)
            selectImage.SetActive(false);

        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
