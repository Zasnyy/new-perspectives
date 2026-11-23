using System;
using UnityEngine;

/// <summary>
/// Manages the player's currently equipped weapon.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private WeaponData startingWeapon;

    [Header("References")]
    [SerializeField] private Transform weaponSocket;

    private PlayerCombat playerCombat;
    private GameObject equippedWeaponVisual;

    /// <summary>
    /// Gets the currently equipped weapon.
    /// </summary>
    public WeaponData CurrentWeapon { get; private set; }

    /// <summary>
    /// Raised whenever the equipped weapon changes.
    /// </summary>
    public event Action<WeaponData> WeaponChanged;

    private void Awake()
    {
        playerCombat = GetComponent<PlayerCombat>();

        if (startingWeapon != null)
        {
            EquipWeapon(startingWeapon);
        }
    }

    /// <summary>
    /// Equips the specified weapon.
    /// </summary>
    /// <param name="weapon">The weapon data to equip.</param>
    /// <returns>True if the weapon was successfully equipped.</returns>
    public bool EquipWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            Debug.LogWarning("Cannot equip a null weapon.", this);
            return false;
        }

        if (playerCombat != null && playerCombat.IsAttacking)
        {
            Debug.LogWarning(
                "Cannot change weapons while an attack is in progress.",
                this);

            return false;
        }

        if (CurrentWeapon == weapon)
        {
            return true;
        }

        DestroyEquippedVisual();

        CurrentWeapon = weapon;

        CreateEquippedVisual();

        WeaponChanged?.Invoke(CurrentWeapon);

        Debug.Log("Equipped weapon: " + weapon.WeaponName);

        return true;
    }

    /// <summary>
    /// Removes the currently equipped weapon.
    /// </summary>
    /// <returns>True if the weapon was successfully removed.</returns>
    public bool UnequipWeapon()
    {
        if (playerCombat != null && playerCombat.IsAttacking)
        {
            return false;
        }

        if (CurrentWeapon == null)
        {
            return true;
        }

        DestroyEquippedVisual();

        CurrentWeapon = null;

        WeaponChanged?.Invoke(null);

        return true;
    }

    /// <summary>
    /// Creates the visual model for the currently equipped weapon.
    /// </summary>
    private void CreateEquippedVisual()
    {
        if (CurrentWeapon == null ||
            CurrentWeapon.WeaponPrefab == null)
        {
            return;
        }

        if (weaponSocket == null)
        {
            Debug.LogWarning(
                "Weapon Socket is not assigned. Weapon will have no visual.",
                this);

            return;
        }

        equippedWeaponVisual = Instantiate(
            CurrentWeapon.WeaponPrefab,
            weaponSocket);

        Transform weaponTransform = equippedWeaponVisual.transform;

        weaponTransform.localPosition = CurrentWeapon.GripPosition;

        weaponTransform.localRotation = Quaternion.Euler(
            CurrentWeapon.GripRotation);

        weaponTransform.localScale = CurrentWeapon.GripScale;
    }

    /// <summary>
    /// Destroys the visual model of the previously equipped weapon.
    /// </summary>
    private void DestroyEquippedVisual()
    {
        if (equippedWeaponVisual != null)
        {
            Destroy(equippedWeaponVisual);
            equippedWeaponVisual = null;
        }
    }
}