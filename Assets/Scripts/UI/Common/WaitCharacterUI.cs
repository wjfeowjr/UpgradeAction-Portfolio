using System;
using UnityEngine;
using UnityEngine.UI;

public class WaitCharacterUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;

    private void OnEnable()
    {
        if(GameManager.Instance.PlayerList.Count <= 1)
            return;
        
        if(GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.PlayerList[0])
            characterImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.PlayerList[1]}_{ConstValues.Face}");
        else if (GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.PlayerList[1])
            characterImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.PlayerList[0]}_{ConstValues.Face}");
    }
}
