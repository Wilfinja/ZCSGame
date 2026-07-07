using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    [Header("Bar References")]
    [Tooltip("The main health slider.")]
    public Slider healthSlider;

    [Tooltip("A second slider behind the first for the ghost effect.")]
    public Slider damageSlider;

    [Header("Boss Reference")]
    public RobotCEOBoss boss;

    [Header("Damage Ghost Settings")]
    public float ghostDelay = 0.6f;
    public float ghostSpeed = 0.4f;

    private float ghostValue;
    private float ghostTimer;

    private void Start()
    {
        if (boss == null)
            boss = FindFirstObjectByType<RobotCEOBoss>();

        float max = boss != null ? boss.maxHealth : 100f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = max;
            healthSlider.value = max;
        }

        if (damageSlider != null)
        {
            damageSlider.minValue = 0f;
            damageSlider.maxValue = max;
            damageSlider.value = max;
        }

        ghostValue = max;
        ghostTimer = 0f;
    }

    private void Update()
    {
        // Re-find boss if our reference was destroyed
        if (boss == null)
            boss = FindFirstObjectByType<RobotCEOBoss>();

        // Still null — no boss in scene yet, drain ghost and wait
        if (boss == null)
        {
            ghostTimer = 0f;
            if (damageSlider != null)
                damageSlider.value = Mathf.MoveTowards(
                    damageSlider.value, 0f, ghostSpeed * damageSlider.maxValue * Time.deltaTime);
            return;
        }

        // Boss found — sync max values in case this is a fresh instance
        if (healthSlider != null && healthSlider.maxValue != boss.maxHealth)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = boss.maxHealth;
            damageSlider.minValue = 0f;
            damageSlider.maxValue = boss.maxHealth;
            healthSlider.value = boss.maxHealth;
            damageSlider.value = boss.maxHealth;
            ghostValue = boss.maxHealth;
        }

        float targetValue = boss.CurrentHealthNormalized * boss.maxHealth;

        if (healthSlider != null)
            healthSlider.value = targetValue;

        if (targetValue < ghostValue)
            ghostTimer = ghostDelay;

        if (ghostTimer > 0f)
        {
            ghostTimer -= Time.deltaTime;
        }
        else
        {
            ghostValue = Mathf.MoveTowards(
                ghostValue, targetValue, ghostSpeed * boss.maxHealth * Time.deltaTime);
            if (damageSlider != null)
                damageSlider.value = ghostValue;
        }
    }
}
