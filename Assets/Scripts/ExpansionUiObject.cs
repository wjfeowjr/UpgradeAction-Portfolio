using DG.Tweening;
using TMPro;
using UnityEngine;

public class ExpansionUiObject : TextUiObject
{
    private Tween selectTween;
    
    [SerializeField] protected GameObject[] selectObjects;
    [SerializeField] protected GameObject[] unSelectObjects;

    public virtual void SelectObjectActive(bool active)
    {
        foreach (var selectObject in selectObjects)
            selectObject.SetActive(active);

        foreach (var unSelectObject in unSelectObjects)
            unSelectObject.SetActive(!active);
    }
    
    public void Expansion(float scale)
    {
        selectTween?.Kill(false);
        selectTween = transform.DOScale(scale, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void Reduction()
    {
        selectTween?.Kill(false);
        selectTween = transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuad).SetUpdate(true);
    }
    
    protected void OnDisable()
    {
        selectTween?.Kill(false);
        selectTween = null;
    }
}
