using TMPro;
using UnityEngine;

public class QuestCompletePopup : MonoBehaviour
{
    [SerializeField] TMP_Text questTitleText, xpText, collectibleText, levelUpText;

    public void Show(QuestResult r)
    {
        questTitleText.text = r.quest.title;
        xpText.text = $"+{r.xpGained} XP";
        collectibleText.text = $"Unlocked: {r.collectibleName} ({r.collectibleRarity})";
        levelUpText.gameObject.SetActive(r.leveledUp);
        levelUpText.text = $"LEVEL UP! Level {r.newLevel}";
        gameObject.SetActive(true);
    }

    public void Close() => gameObject.SetActive(false);
}