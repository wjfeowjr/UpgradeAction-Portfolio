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

    public void SetData(string fileName, int idx)
    {
        var isNewGame = string.IsNullOrWhiteSpace(fileName);
        newGameObject.SetActive(isNewGame);
        saveGameObject.SetActive(!isNewGame);
        numText.text = $"세이브{idx}_";
        
        if (isNewGame)
        {
            newGameText.text = "새 게임_";
        }
        else
        {
            var saveData = GameManager.Instance.LoadGame(fileName);

            placeText.text = "귀신_";
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
