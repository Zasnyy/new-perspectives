using UnityEngine;

/// <summary>
/// Stores the combat properties and visual settings for an individual weapon.
/// Each weapon is created as its own ScriptableObject asset.
/// </summary>
[CreateAssetMenu(
    fileName = "NewWeapon",
    menuName = "The Unseen Plane/Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Weapon")]
    [SerializeField] private string weaponName = "Starter Weapon";
    [SerializeField] private GameObject weaponPrefab;

    [Header("Damage and Timing")]
    [SerializeField, Min(0f)] private float attackDamage = 25f;
    [SerializeField, Min(0f)] private float attackCooldown = 0.8f;
    [SerializeField, Min(0f)] private float attackStartupDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float attackActiveDuration = 0.12f;
    [SerializeField, Min(0f)] private float attackRecoveryDuration = 0.35f;

    [Header("2D Attack")]
    [SerializeField, Min(0.01f)] private float attackRange2D = 1.6f;
    [SerializeField, Min(0.01f)] private float attackHeight2D = 1.4f;
    [SerializeField, Min(0.01f)] private float attackDepth2D = 0.8f;
    [SerializeField] private float attackHeightOffset2D = 0.7f;
    [SerializeField, Range(0f, 180f)] private float attackAngle2D = 110f;

    [Header("3D Attack")]
    [SerializeField]
    private Vector3 attackBoxSize3D =
        new Vector3(1.2f, 1.4f, 1.8f);
    [SerializeField] private float attackHeightOffset3D = 0.7f;
    [SerializeField, Range(0f, 180f)] private float attackAngle3D = 100f;

    [Header("Weapon Visual Position")]
    [SerializeField] private Vector3 gripPosition;
    [SerializeField] private Vector3 gripRotation;
    [SerializeField] private Vector3 gripScale = Vector3.one;

    /// <summary>
    /// Gets the weapon's display name.
    /// </summary>
    public string WeaponName => weaponName;

    /// <summary>
    /// Gets the optional weapon model prefab.
    /// </summary>
    public GameObject WeaponPrefab => weaponPrefab;

    /// <summary>
    /// Gets the weapon's damage value.
    /// </summary>
    public float AttackDamage => attackDamage;

    /// <summary>
    /// Gets the minimum delay between attacks.
    /// </summary>
    public float AttackCooldown => attackCooldown;

    /// <summary>
    /// Gets the attack's startup duration.
    /// </summary>
    public float AttackStartupDuration => attackStartupDuration;

    /// <summary>
    /// Gets the attack's active duration.
    /// </summary>
    public float AttackActiveDuration => attackActiveDuration;

    /// <summary>
    /// Gets the attack's recovery duration.
    /// </summary>
    public float AttackRecoveryDuration => attackRecoveryDuration;

    public float AttackRange2D => attackRange2D;
    public float AttackHeight2D => attackHeight2D;
    public float AttackDepth2D => attackDepth2D;
    public float AttackHeightOffset2D => attackHeightOffset2D;
    public float AttackAngle2D => attackAngle2D;

    public Vector3 AttackBoxSize3D => attackBoxSize3D;
    public float AttackHeightOffset3D => attackHeightOffset3D;
    public float AttackAngle3D => attackAngle3D;

    public Vector3 GripPosition => gripPosition;
    public Vector3 GripRotation => gripRotation;
    public Vector3 GripScale => gripScale;
}