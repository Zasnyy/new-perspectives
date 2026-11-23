using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles weapon-based player combat in both gameplay perspectives.
/// Targets must implement IDamageable to receive damage.
/// </summary>
[RequireComponent(typeof(WeaponController))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerMovementController playerMovement;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Transform twoDPlane;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask targetLayers = ~0;

    private readonly HashSet<int> hitTargets = new HashSet<int>();

    private float nextAttackTime;
    private bool combatLocked;
    private bool isAttacking;

    private Coroutine attackRoutine;

    /// <summary>
    /// Gets whether the player is currently performing an attack.
    /// </summary>
    public bool IsAttacking => isAttacking;

    /// <summary>
    /// Gets the currently equipped weapon.
    /// </summary>
    public WeaponData CurrentWeapon =>
        weaponController != null
            ? weaponController.CurrentWeapon
            : null;

    /// <summary>
    /// Gets whether the player can currently attack.
    /// </summary>
    public bool CanAttack =>
        !combatLocked &&
        !isAttacking &&
        CurrentWeapon != null &&
        (playerMovement == null ||
         !playerMovement.IsMovementLocked) &&
        (playerHealth == null ||
         !playerHealth.IsDead) &&
        Time.time >= nextAttackTime;

    /// <summary>
    /// Raised when the player begins an attack.
    /// </summary>
    public event Action AttackPerformed;

    /// <summary>
    /// Raised with the weapon and perspective used for an attack.
    /// </summary>
    public event Action<WeaponData, PerspectiveMode> AttackStarted;

    /// <summary>
    /// Raised when an attack finishes its recovery stage.
    /// </summary>
    public event Action<WeaponData> AttackFinished;

    /// <summary>
    /// Raised if an attack is interrupted.
    /// </summary>
    public event Action AttackCancelled;

    /// <summary>
    /// Raised when an attack damages a target.
    /// </summary>
    public event Action<MonoBehaviour> TargetHit;

    private void Awake()
    {
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovementController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (weaponController == null)
        {
            weaponController = GetComponent<WeaponController>();
        }
    }

    private void Start()
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "PlayerCombat requires a WeaponController.",
                this);

            enabled = false;
            return;
        }

        if (weaponController.CurrentWeapon == null)
        {
            Debug.LogWarning(
                "No starting weapon is equipped.",
                this);
        }
    }

    private void Update()
    {
        if (inputReader != null &&
            inputReader.AttackPressedThisFrame)
        {
            TryAttack();
        }
    }

    /// <summary>
    /// Attempts to begin an attack using the currently equipped weapon.
    /// </summary>
    /// <returns>True if an attack was successfully started.</returns>
    public bool TryAttack()
    {
        if (!CanAttack)
        {
            return false;
        }

        WeaponData weapon = CurrentWeapon;

        PerspectiveMode attackPerspective =
            playerMovement != null
                ? playerMovement.CurrentPerspective
                : PerspectiveMode.ThreeD;

        if (attackPerspective == PerspectiveMode.TwoD &&
            twoDPlane == null)
        {
            Debug.LogError(
                "PlayerCombat requires the 2D Plane in 2D perspective.",
                this);

            return false;
        }

        Vector3 attackDirection = GetFacingDirection(
            attackPerspective);

        if (attackDirection.sqrMagnitude < 0.001f)
        {
            return false;
        }

        hitTargets.Clear();

        isAttacking = true;

        float attackDuration =
            weapon.AttackStartupDuration +
            weapon.AttackActiveDuration +
            weapon.AttackRecoveryDuration;

        nextAttackTime = Time.time + Mathf.Max(
            weapon.AttackCooldown,
            attackDuration);

        AttackPerformed?.Invoke();

        AttackStarted?.Invoke(weapon, attackPerspective);

        attackRoutine = StartCoroutine(
            ExecuteAttack(
                weapon,
                attackPerspective,
                attackDirection));

        return true;
    }

    /// <summary>
    /// Executes the startup, active, and recovery stages of an attack.
    /// </summary>
    /// <param name="weapon">The weapon used for this attack.</param>
    /// <param name="perspective">The perspective at attack initiation.</param>
    /// <param name="attackDirection">The direction the attack faces.</param>
    /// <returns>The attack coroutine.</returns>
    private IEnumerator ExecuteAttack(
        WeaponData weapon,
        PerspectiveMode perspective,
        Vector3 attackDirection)
    {
        // Startup: delay the hit detection until the weapon is ready.
        if (weapon.AttackStartupDuration > 0f)
        {
            yield return new WaitForSeconds(
                weapon.AttackStartupDuration);
        }

        // Active: continuously check for targets during the hit window.
        float activeEndTime = Time.time +
            Mathf.Max(0.01f, weapon.AttackActiveDuration);

        do
        {
            if (perspective == PerspectiveMode.TwoD)
            {
                Perform2DAttack(weapon, attackDirection);
            }
            else
            {
                Perform3DAttack(weapon, attackDirection);
            }

            yield return null;
        }
        while (Time.time < activeEndTime && !combatLocked);

        // Recovery: prevent another attack until recovery is complete.
        if (weapon.AttackRecoveryDuration > 0f)
        {
            yield return new WaitForSeconds(
                weapon.AttackRecoveryDuration);
        }

        isAttacking = false;
        attackRoutine = null;

        AttackFinished?.Invoke(weapon);
    }

    /// <summary>
    /// Performs an attack using the weapon's 2D attack properties.
    /// </summary>
    /// <param name="weapon">The weapon performing the attack.</param>
    /// <param name="facingDirection">The direction the attack faces.</param>
    private void Perform2DAttack(
        WeaponData weapon,
        Vector3 facingDirection)
    {
        Vector3 planeNormal = twoDPlane.forward.normalized;

        Quaternion boxRotation = twoDPlane.rotation;

        Vector3 attackCenter = transform.position +
            facingDirection * (weapon.AttackRange2D * 0.5f) +
            twoDPlane.up * weapon.AttackHeightOffset2D;

        Vector3 halfExtents = new Vector3(
            weapon.AttackRange2D * 0.5f,
            weapon.AttackHeight2D * 0.5f,
            weapon.AttackDepth2D * 0.5f);

        Collider[] hits = Physics.OverlapBox(
            attackCenter,
            halfExtents,
            boxRotation,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        ApplyDamageToHits(
            hits,
            weapon,
            facingDirection,
            true);
    }

    /// <summary>
    /// Performs an attack using the weapon's 3D attack properties.
    /// </summary>
    /// <param name="weapon">The weapon performing the attack.</param>
    /// <param name="facingDirection">The direction the attack faces.</param>
    private void Perform3DAttack(
        WeaponData weapon,
        Vector3 facingDirection)
    {
        Vector3 boxSize = weapon.AttackBoxSize3D;

        Quaternion boxRotation = Quaternion.LookRotation(
            facingDirection,
            Vector3.up);

        Vector3 attackCenter = transform.position +
            facingDirection * (boxSize.z * 0.5f) +
            Vector3.up * weapon.AttackHeightOffset3D;

        Collider[] hits = Physics.OverlapBox(
            attackCenter,
            boxSize * 0.5f,
            boxRotation,
            targetLayers,
            QueryTriggerInteraction.Ignore);

        ApplyDamageToHits(
            hits,
            weapon,
            facingDirection,
            false);
    }

    /// <summary>
    /// Applies weapon damage to each valid target only once per attack.
    /// </summary>
    /// <param name="hits">The colliders found by the attack.</param>
    /// <param name="weapon">The weapon used to determine damage.</param>
    /// <param name="facingDirection">The attack's facing direction.</param>
    /// <param name="isTwoD">Whether this is a 2D attack.</param>
    private void ApplyDamageToHits(
        Collider[] hits,
        WeaponData weapon,
        Vector3 facingDirection,
        bool isTwoD)
    {
        foreach (Collider hit in hits)
        {
            if (!TryGetDamageable(
                hit,
                out MonoBehaviour targetComponent,
                out IDamageable damageable))
            {
                continue;
            }

            int targetId = targetComponent.GetInstanceID();

            if (damageable.IsDead ||
                hitTargets.Contains(targetId))
            {
                continue;
            }

            Vector3 directionToTarget =
                targetComponent.transform.position - transform.position;

            float maximumAngle;

            if (isTwoD)
            {
                Vector3 planeNormal = twoDPlane.forward.normalized;

                float depthDifference = Mathf.Abs(Vector3.Dot(
                    directionToTarget,
                    planeNormal));

                if (depthDifference > weapon.AttackDepth2D * 0.5f)
                {
                    continue;
                }

                directionToTarget = Vector3.ProjectOnPlane(
                    directionToTarget,
                    planeNormal);

                maximumAngle = weapon.AttackAngle2D;
            }
            else
            {
                directionToTarget = Vector3.ProjectOnPlane(
                    directionToTarget,
                    Vector3.up);

                maximumAngle = weapon.AttackAngle3D;
            }

            if (directionToTarget.sqrMagnitude < 0.001f)
            {
                continue;
            }

            float angle = Vector3.Angle(
                facingDirection,
                directionToTarget.normalized);

            if (angle > maximumAngle * 0.5f)
            {
                continue;
            }

            hitTargets.Add(targetId);

            damageable.TakeDamage(weapon.AttackDamage);

            TargetHit?.Invoke(targetComponent);
        }
    }

    /// <summary>
    /// Finds a damage receiver on the hit collider or one of its parents.
    /// </summary>
    /// <param name="hit">The collider hit by the attack.</param>
    /// <param name="targetComponent">The component implementing IDamageable.</param>
    /// <param name="damageable">The damage receiver interface.</param>
    /// <returns>True if a valid target was found.</returns>
    private bool TryGetDamageable(
        Collider hit,
        out MonoBehaviour targetComponent,
        out IDamageable damageable)
    {
        targetComponent = null;
        damageable = null;

        MonoBehaviour[] components =
            hit.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component == null ||
                !component.isActiveAndEnabled ||
                component.transform == transform ||
                component.transform.IsChildOf(transform))
            {
                continue;
            }

            if (component is IDamageable receiver)
            {
                targetComponent = component;
                damageable = receiver;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates the player's facing direction for the selected perspective.
    /// </summary>
    /// <param name="perspective">The active gameplay perspective.</param>
    /// <returns>A normalized world-space facing direction.</returns>
    private Vector3 GetFacingDirection(PerspectiveMode perspective)
    {
        Vector3 direction;

        if (perspective == PerspectiveMode.TwoD)
        {
            Vector3 planeNormal = twoDPlane.forward.normalized;

            direction = Vector3.ProjectOnPlane(
                transform.forward,
                planeNormal);

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = twoDPlane.right;
            }
        }
        else
        {
            direction = Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up);

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector3.forward;
            }
        }

        return direction.normalized;
    }

    /// <summary>
    /// Enables or disables player combat.
    /// Interrupts an active attack when combat is locked.
    /// </summary>
    /// <param name="locked">Whether combat should be locked.</param>
    public void SetCombatLocked(bool locked)
    {
        combatLocked = locked;

        if (!locked || attackRoutine == null)
        {
            return;
        }

        StopCoroutine(attackRoutine);

        attackRoutine = null;
        isAttacking = false;

        AttackCancelled?.Invoke();
    }

    private void OnDisable()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);

            attackRoutine = null;
            isAttacking = false;

            AttackCancelled?.Invoke();
        }
    }

    /// <summary>
    /// Draws the currently equipped weapon's attack area in the editor.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        WeaponController controller = weaponController;

        if (controller == null)
        {
            controller = GetComponent<WeaponController>();
        }

        if (controller == null || controller.CurrentWeapon == null)
        {
            return;
        }

        WeaponData weapon = controller.CurrentWeapon;

        PerspectiveMode perspective = playerMovement != null
            ? playerMovement.CurrentPerspective
            : PerspectiveMode.ThreeD;

        if (perspective == PerspectiveMode.TwoD)
        {
            if (twoDPlane == null)
            {
                return;
            }

            Vector3 facing = GetFacingDirection(perspective);

            Vector3 center = transform.position +
                facing * (weapon.AttackRange2D * 0.5f) +
                twoDPlane.up * weapon.AttackHeightOffset2D;

            Gizmos.color = Color.yellow;

            Gizmos.matrix = Matrix4x4.TRS(
                center,
                twoDPlane.rotation,
                Vector3.one);

            Gizmos.DrawWireCube(
                Vector3.zero,
                new Vector3(
                    weapon.AttackRange2D,
                    weapon.AttackHeight2D,
                    weapon.AttackDepth2D));
        }
        else
        {
            Vector3 facing = GetFacingDirection(perspective);
            Vector3 boxSize = weapon.AttackBoxSize3D;

            Vector3 center = transform.position +
                facing * (boxSize.z * 0.5f) +
                Vector3.up * weapon.AttackHeightOffset3D;

            Gizmos.color = Color.red;

            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.LookRotation(facing, Vector3.up),
                Vector3.one);

            Gizmos.DrawWireCube(Vector3.zero, boxSize);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}