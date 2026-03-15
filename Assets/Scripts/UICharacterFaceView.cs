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
    [SerializeField] private Image faceImage;

    public void SetFace(string characterId)
    {
        faceImage.sprite = GameManager.Instance.GetAtlasSprite($"{characterId}_{ConstValues.Face}");
    }
}
