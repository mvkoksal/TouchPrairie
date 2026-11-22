
using UnityEngine;
using System.Linq;

public class QuestBoardView : MonoBehaviour
{
    [SerializeField] Transform content;
    [SerializeField] QuestRowView rowPrefab;
    [SerializeField] QuestCompletePopup popup;

    void OnEnable()
    {
        QuestService.QuestsChanged += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        QuestService.QuestsChanged -= Rebuild;
    }

    void Rebuild()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var quest in QuestService.GetQuests())
        {
            Instantiate(rowPrefab, content)
                .Bind(quest, QuestService.Start);
        }
    }

    public void OnTestCompleteClicked()
    {
        var active = QuestService.ActiveQuests.FirstOrDefault();

        if (active == null)
        {
            Debug.Log("No available quests to complete.");
            return;
        }

        QuestResult result = QuestService.Complete(active.id);

        if (popup != null && result != null)
            popup.Show(result);
    }
}
