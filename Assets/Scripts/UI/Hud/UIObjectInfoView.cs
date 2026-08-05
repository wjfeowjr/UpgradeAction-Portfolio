using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 표시할 값 묶음. 로직은 없다.
public class UIObjectInfoModel
{
    public string id;
    public string objectName;
    public int count;
}

// 받은 값을 그리기만 한다.
// 무엇을 그릴지 판단하는 부분이 없어 Presenter 를 두지 않았다.
public class UIObjectInfoView : MonoBehaviour
{
    private const float FadeDuration = 0.5f;
    private const float StayDuration = 1.5f;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image image;
    [SerializeField] private TMP_Text objectText;

    private Sequence _sequence;

    public void SetObjectText(UIObjectInfoModel model)
    {
        SetObjectText(model.id, model.objectName, model.count);
    }

    private void SetObjectText(string id, string objectName, int count)
    {
        _sequence?.Kill();

        gameObject.SetActive(true);

        image.sprite = GameManager.Instance.GetAtlasSprite(id);

        if(count <= 1)
            objectText.text = objectName;
        else
            objectText.text = $"{objectName} x {count}";

        canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence().Append(canvasGroup.DOFade(1f, FadeDuration)).AppendInterval(StayDuration).Append(canvasGroup.DOFade(0f, FadeDuration));
        SoundManager.Instance.PlaySound(ConstValues.Upgrade);
    }

    public void HideImmediate()
    {
        _sequence?.Kill();
        gameObject.SetActive(false);
    }
}
