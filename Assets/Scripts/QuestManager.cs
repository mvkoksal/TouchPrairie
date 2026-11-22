
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class QuestManager : MonoBehaviour
{
    [Header("Quest UI")]
    public Transform questContainer;
    public QuestCardUI questCardTemplate;

    private List<QuestData> displayedQuests = new List<QuestData>();

    void Start()
    {
        RefreshQuestBoard();
    }

    void OnEnable()
    {
        QuestService.QuestsChanged += RefreshQuestBoard;
    }

    void OnDisable()
    {
        QuestService.QuestsChanged -= RefreshQuestBoard;
    }

    public List<QuestData> GetActiveQuests()
    {
        return displayedQuests
            .Where(q => q.status != QuestStatus.Completed)
            .ToList();
    }

    public void RefreshQuestBoard()
    {
        if (questCardTemplate == null || questContainer == null)
        {
            Debug.LogError("Quest UI references are missing!");
            return;
        }

        foreach (Transform child in questContainer)
        {
            if (child.gameObject != questCardTemplate.gameObject)
                Destroy(child.gameObject);
        }

        questCardTemplate.gameObject.SetActive(false);
        displayedQuests.Clear();

        List<QuestData> quests = QuestService.GetQuests()
            .Where(q => q.status != QuestStatus.Completed)
            .ToList();

        foreach (QuestData quest in quests)
        {
            QuestCardUI card = Instantiate(
                questCardTemplate,
                questContainer,
                false
            );

            card.gameObject.SetActive(true);
            card.transform.localScale = Vector3.one;

            // Populate the card using the existing UI method.
            card.Setup(new Quest
            {
                id = quest.id,
                title = quest.title,
                description = quest.description,
                difficulty = quest.difficulty,
                xp_reward = quest.xpReward,
                collectible_reward = quest.collectibleName
            });

            displayedQuests.Add(quest);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            questContainer as RectTransform
        );
    }

    public QuestResult CompleteQuest(string questId)
    {
        QuestResult result = QuestService.Complete(questId);

        if (result != null)
            Debug.Log("Completed quest: " + result.quest.title);

        return result;
    }
}
