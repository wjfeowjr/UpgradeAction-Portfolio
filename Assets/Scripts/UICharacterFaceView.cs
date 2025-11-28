using UnityEngine;
using UnityEngine.UI;

public interface ICharacterFace
{
    void SetFace(string characterId);
}

public class UICharacterFaceModel
{
    public string firstCharacter;
    public string secondCharacter;
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

    public void SetFirstFace()
    {
        _characterFace.SetFace(_model.firstCharacter);
    }
    
    public void SetSecondFace()
    {
        _characterFace.SetFace(_model.secondCharacter);
    }
}

public class UICharacterFaceView : MonoBehaviour, ICharacterFace
{
    [SerializeField] private Image faceImage;

    public void SetFace(string characterId)
    {
        faceImage.sprite = GameManager.Instance.GetAtlasSprite($"{characterId}_{ConstValues.Face}");
    }
}
