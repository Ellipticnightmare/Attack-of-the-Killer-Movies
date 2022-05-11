using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    ItemSlotManager itemSlotManager;

    public WeaponItem rightWeapon;

    private void Awake()
    {
        itemSlotManager = GetComponentInChildren<ItemSlotManager>();
    }

    private void Start()
    {
      itemSlotManager.LoadItemOnSlot(rightWeapon, true);
    }
}
