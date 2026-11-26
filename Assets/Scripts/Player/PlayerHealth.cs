using System;
using UnityEngine;

namespace PinkApocalypse.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        public float MaximumHealth => maximumHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedHealth => maximumHealth > 0f ? currentHealth / maximumHealth : 0f;
        public event Action<float, float> HealthChanged;

        private void Awake()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maximumHealth);
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || currentHealth <= 0f) return;
            SetHealth(currentHealth - amount);
        }

        public void RestoreHealth(float amount)
        {
            if (amount <= 0f || currentHealth >= maximumHealth) return;
            SetHealth(currentHealth + amount);
        }

        public void SetHealth(float value)
        {
            float updated = Mathf.Clamp(value, 0f, maximumHealth);
            if (Mathf.Approximately(currentHealth, updated)) return;
            currentHealth = updated;
            HealthChanged?.Invoke(currentHealth, maximumHealth);
        }
    }
}
