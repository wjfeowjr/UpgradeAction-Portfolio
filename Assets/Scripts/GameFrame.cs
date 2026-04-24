using DG.Tweening;
using TMPro;
using UnityEngine;

public class GameFrame : ExpansionUiObject
{
    [SerializeField] private TMP_Text  settingValue;
    [SerializeField] private Transform leftArrow;
    [SerializeField] private Transform rightArrow;
    [SerializeField] private float     arrowOffset   = 8f;
    [SerializeField] private float     arrowDuration = 0.4f;

    private Vector3 _leftArrowOrigin;
    private Vector3 _rightArrowOrigin;

    private void Awake()
    {
        _leftArrowOrigin  = leftArrow.localPosition;
        _rightArrowOrigin = rightArrow.localPosition;
    }

    private void OnEnable()
    {
        leftArrow.localPosition  = _leftArrowOrigin;
        rightArrow.localPosition = _rightArrowOrigin;

        leftArrow.DOLocalMoveX(_leftArrowOrigin.x - arrowOffset, arrowDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);

        rightArrow.DOLocalMoveX(_rightArrowOrigin.x + arrowOffset, arrowDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        leftArrow.DOKill();
        rightArrow.DOKill();
        leftArrow.localPosition  = _leftArrowOrigin;
        rightArrow.localPosition = _rightArrowOrigin;
    }

    public void SetData(string value)
    {
        settingValue.text = value;
    }
}
