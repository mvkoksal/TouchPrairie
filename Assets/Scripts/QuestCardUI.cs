using TMPro;
using UnityEngine;

public class QuestCardUI : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text difficultyText;
    public TMP_Text rewardText;

    public void Setup(Quest quest)
    {
        titleText.text = quest.title;
        descriptionText.text = quest.description;
        difficultyText.text = quest.difficulty;
        rewardText.text = "+" + quest.xp_reward + " XP";
    }
}