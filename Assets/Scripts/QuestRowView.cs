using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestRowView : MonoBehaviour
{
    [SerializeField] TMP_Text questTitle, questDescription, questDifficulty, questXP;

    public void Bind(QuestData q, Action<string> onStart)
    {
        questTitle.text = q.title;
        questDescription.text = q.description;
        questDifficulty.text = q.difficulty;
        questXP.text = $"+{q.xpReward} XP";
    }
}