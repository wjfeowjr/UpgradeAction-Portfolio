using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveFrame : ExpansionUiObject
{
    [SerializeField] private GameObject newGameObject;
    [SerializeField] private GameObject saveGameObject;
    
    [SerializeField] private TMP_Text newGameText; 
    [SerializeField] private TMP_Text placeText; 
    [SerializeField] private TMP_Text goldText; 
    [SerializeField] private TMP_Text numText;
    [SerializeField] private Image[] characterImages;

    // ReSharper disable Unity.PerformanceAnalysis
    public void SetData(string fileName, int idx)
    {
        var isNewGame = string.IsNullOrWhiteSpace(fileName);
        newGameObject.SetActive(isNewGame);
        saveGameObject.SetActive(!isNewGame);
        numText.text = string.Format(GameManager.Instance.GetTalk(30051), idx);
        
        if (isNewGame)
        {
            newGameText.text = GameManager.Instance.GetTalk(30052);
        }
        else
        {
            var saveData = GameManager.Instance.LoadGame(fileName);

            if (string.IsNullOrWhiteSpace(saveData.savePoint))
            {
                placeText.text = GameManager.Instance.GetTalk(130000);
            }
            else
            {
                var roomTableData = TableManager.Instance.GetRoom(saveData.savePoint);
                placeText.text = GameManager.Instance.GetPlaceName(TableParse.Enum<ePlace>(roomTableData.place));
            }
            
            goldText.text = GameManager.Instance.GetThousandCommaText(saveData.gold);

            foreach (var characterImage in characterImages)
                characterImage.gameObject.SetActive(false);

            for (int i = 0; i < saveData.playerList.Count; i++)
            {
                characterImages[i].sprite = GameManager.Instance.GetAtlasSprite($"{saveData.playerList[i]}_{ConstValues.Face}");
                characterImages[i].gameObject.SetActive(true);
            }
        }
    }
}
