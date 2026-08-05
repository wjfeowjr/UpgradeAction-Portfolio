using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 표시할 값 묶음. 로직은 없다.
public class UICharacterFaceModel
{
    public List<string> playerList = new List<string>();
}

// 받은 값을 그리기만 한다.
// 무엇을 그릴지 판단하는 부분이 없어 Presenter 를 두지 않았다.
public class UICharacterFaceView : MonoBehaviour
{
    [SerializeField] private Image faceImage;

    public void SetChangeFace(UICharacterFaceModel model)
    {
        SetFace(model.playerList[0]);
    }

    private void SetFace(string characterId)
    {
        faceImage.sprite = GameManager.Instance.GetAtlasSprite($"{characterId}_{ConstValues.Face}");
    }
}
