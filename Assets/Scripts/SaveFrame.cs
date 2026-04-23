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

            if (string.IsNullOrWhiteSpace(saveData.savePoint))
            {
                placeText.text = "태양의 언덕_";
            }
            else
            {
                var roomTableData = TableManager.Instance.roomsTable.Rooms.Find(x => x.id == saveData.savePoint);
                var place = roomTableData.place;
                switch (place)
                {
                    case ConstValues.SunHill:
                        placeText.text = "태양의 언덕_";
                        break;
                    
                    case ConstValues.BaseCamp:
                        placeText.text = "베이스 캠프_";
                        break;
                    
                    case ConstValues.Forest:
                        placeText.text = "위험한 숲_";
                        break;
                    
                    case ConstValues.Mine:
                        placeText.text = "금광_";
                        break;
                }
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
