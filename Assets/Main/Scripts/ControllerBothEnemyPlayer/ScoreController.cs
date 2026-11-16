using System;
using TMPro;
using UnityEngine;

public class ScoreController : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    public event Action<ScoreController> ScoreChanged;

    public int CurrentScore { get; private set; }
    public int CurrentPoint => CurrentScore;

    // Refresh the UI once when the scene starts.
    private void Awake()
    {
        CurrentScore = Mathf.Max(0, CurrentScore);
        NotifyDataChanged();
    }

    // Add raw score and update any linked score text.
    public void AddScore(int amount)
    {
        CurrentScore = Mathf.Max(0, CurrentScore + amount);
        NotifyDataChanged();
    }

    // Reset the current score back to zero.
    public void ResetScore()
    {
        CurrentScore = 0;
        NotifyDataChanged();
    }

    public void RefreshDataBindings()
    {
        NotifyDataChanged();
    }

    // Push the current score value to the UI.
    private void RefreshUI()
    {
        if (scoreText != null)
            scoreText.text = CurrentScore.ToString();
    }

    private void NotifyDataChanged()
    {
        RefreshUI();
        ScoreChanged?.Invoke(this);
    }
}
