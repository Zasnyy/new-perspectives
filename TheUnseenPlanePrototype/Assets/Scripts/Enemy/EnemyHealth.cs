
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth = Mathf.Max(
            0f,
            currentHealth - damage);

        Debug.Log(
            $"{gameObject.name} received {damage} damage. " +
            $"Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log(gameObject.name + " has been defeated.");

        EnemyAIController ai = GetComponent<EnemyAIController>();

        if (ai != null)
            ai.enabled = false;

        EnemyMovement movement = GetComponent<EnemyMovement>();

        if (movement != null)
            movement.SetMovementEnabled(false);

        EnemyCombat combat = GetComponent<EnemyCombat>();

        if (combat != null)
            combat.enabled = false;

        CharacterController controller =
            GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        // Death animation and ragdoll can be added later.
    }
}
