using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSlotManager : MonoBehaviour
{
    ItemHolderSlot rightHandSlot;

    private void Awake()
    {
      ItemHolderSlot[] itemHolderSlots = GetComponentsInChildren<ItemHolderSlot>();
      foreach (ItemHolderSlot itemSlot in itemHolderSlots)
      {
        if(itemSlot.isRightHandSlot)
        {
          rightHandSlot = itemSlot;
        }
      }
    }

      public void LoadItemOnSlot(WeaponItem weaponItem, bool isRight)
      {
        if(isRight)
        {
            rightHandSlot.LoadItemModel(weaponItem);
        }
      }

}
