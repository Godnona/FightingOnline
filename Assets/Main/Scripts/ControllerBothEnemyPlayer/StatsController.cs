using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsController : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    [Header("Ripp")]
    [SerializeField] private int maxRipp = 100;
    [SerializeField] private int currentRipp = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text rippText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider rippSlider;

    [Header("Respawn")]
    [SerializeField] private Transform transformSpawn;
    [SerializeField] private GameObject vfxRespawn;

    public event Action<StatsController> StatsChanged;
    public event Action<int, int> HealthChanged;
    public event Action<int, int> RippChanged;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public int MaxRipp => maxRipp;
    public int CurrentRipp => currentRipp;
    public bool IsDead => currentHealth <= 0;

    // Clamp startup values and refresh linked UI.
    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        currentRipp = Mathf.Clamp(currentRipp, 0, maxRipp);
        NotifyDataChanged();
    }

    // Apply incoming damage and keep health inside its valid range.
    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - Mathf.Abs(amount), 0, maxHealth);
        NotifyDataChanged();
    }

    // Restore health without going above the maximum value.
    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + Mathf.Abs(amount), 0, maxHealth);
        NotifyDataChanged();
    }

    // Check whether this fighter has enough ripp to use an attack.
    public bool CanUseRipp(int amount)
    {
        return currentRipp >= Mathf.Max(0, amount);
    }

    // Spend ripp only when enough resource is available.
    public bool SpendRipp(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (currentRipp < safeAmount)
            return false;
        currentRipp -= safeAmount;
        NotifyDataChanged();
        return true;
    }

    // Add or remove ripp and clamp the final value.
    public void ChangeRipp(int amount)
    {
        currentRipp = Mathf.Clamp(currentRipp + amount, 0, maxRipp);
        NotifyDataChanged();
    }

    // Restore the full health and ripp values.
    public void RestoreFullStats()
    {
        currentHealth = maxHealth;
        currentRipp = maxRipp;
        NotifyDataChanged();
    }

    public void RefreshDataBindings()
    {
        NotifyDataChanged();
    }

    // Move the owner back to the configured spawn point without resetting combat progress.
    public void RespawnAtSpawn()
    {
        if (transformSpawn == null)
            return;

        transform.position = transformSpawn.position;

        if (vfxRespawn != null)
            Instantiate(vfxRespawn, transformSpawn.position, Quaternion.identity);
    }

    // Push the latest health and ripp values to the assigned UI.
    private void RefreshUI()
    {
        if (healthText != null)
            healthText.text = currentHealth.ToString();
        if (rippText != null)
            rippText.text = currentRipp.ToString();

        RefreshSlider(healthSlider, currentHealth, maxHealth);
        RefreshSlider(rippSlider, currentRipp, maxRipp);
    }

    private static void RefreshSlider(Slider slider, int currentValue, int maxValue)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = Mathf.Max(1, maxValue);
        slider.SetValueWithoutNotify(Mathf.Clamp(currentValue, 0, maxValue));
    }

    private void NotifyDataChanged()
    {
        RefreshUI();
        HealthChanged?.Invoke(currentHealth, maxHealth);
        RippChanged?.Invoke(currentRipp, maxRipp);
        StatsChanged?.Invoke(this);
    }
}
