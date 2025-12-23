using System;
using UnityEngine;
using UnityEngine.UI;

public class WaitCharacterUI : MonoBehaviour
{
    [SerializeField] private Image characterImage;

    private void OnEnable()
    {
        if(GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.FirstPlayer)
            characterImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.SecondPlayer}_{ConstValues.Face}");
        else if (GameManager.Instance.CurPlayer.BasicStat.id == GameManager.Instance.SecondPlayer)
            characterImage.sprite = GameManager.Instance.GetAtlasSprite($"{GameManager.Instance.FirstPlayer}_{ConstValues.Face}");
    }
}
