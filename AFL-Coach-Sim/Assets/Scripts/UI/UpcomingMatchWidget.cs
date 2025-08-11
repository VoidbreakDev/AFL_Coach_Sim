// Assets/Scripts/UI/UpcomingMatchWidget.cs
using UnityEngine;
using TMPro;
using AFLManager.Models;
using System;

public class UpcomingMatchWidget : MonoBehaviour
{
    [SerializeField] TMP_Text homeName;
    [SerializeField] TMP_Text awayName;
    [SerializeField] TMP_Text dateText;
    [SerializeField] UnityEngine.UI.Button playButton;

    public System.Action OnPlay;

    void Awake() { if (playButton) playButton.onClick.AddListener(() => OnPlay?.Invoke()); }

    public void Bind(Match m, DateTime date)
    {
        homeName.text = m.HomeTeamId;
        awayName.text = m.AwayTeamId;
        dateText.text = date.ToString("d MMM yyyy");
        playButton.interactable = true;
    }

    public void BindNoMatch()
    {
        homeName.text = "-";
        awayName.text = "-";
        dateText.text = "No upcoming match";
        playButton.interactable = false;
    }
}
