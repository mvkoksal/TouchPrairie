using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePanel : MonoBehaviour
{
    [SerializeField] TMP_Text playerName, playerLevel;
    [SerializeField] Slider xpBar;

    void OnEnable() { PlayerService.Changed += Refresh; Refresh(PlayerService.Current); }
    void OnDisable() { PlayerService.Changed -= Refresh; }

    void Refresh(PlayerData p)
    {
        playerName.text = p.name;
        playerLevel.text = $"{p.level}";
        xpBar.value = p.Progress;   // Slider min 0, max 1
    }
}