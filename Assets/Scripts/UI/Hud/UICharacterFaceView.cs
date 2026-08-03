using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICharacterFace
{
    void SetFace(string characterId);
}

public class UICharacterFaceModel
{
    public List<string> playerList = new List<string>();
}

public class UICharacterFacePresenter
{
    private readonly ICharacterFace _characterFace;
    private UICharacterFaceModel _model;

    public UICharacterFacePresenter(ICharacterFace characterFace, UICharacterFaceModel model)
    {
        _characterFace = characterFace;
        _model = model;
    }

    public void SetChangeFace()
    {
        _characterFace.SetFace(_model.playerList[0]);
    }
}

public class UICharacterFaceView : MonoBehaviour, ICharacterFace
{
    // 이 View 가 자기 Presenter 를 직접 조립한다.
    // 호출부가 인터페이스 변환 -> Model 생성 -> Presenter 생성 -> 역주입을
    // 매번 반복하던 것을 한 줄로 줄인다.
    private UICharacterFacePresenter presenter;
    public UICharacterFacePresenter Presenter => presenter;

    public UICharacterFacePresenter Bind(UICharacterFaceModel model)
    {
        presenter = new UICharacterFacePresenter(this, model);
        return presenter;
    }

    [SerializeField] private Image faceImage;

    public void SetFace(string characterId)
    {
        faceImage.sprite = GameManager.Instance.GetAtlasSprite($"{characterId}_{ConstValues.Face}");
    }
}
