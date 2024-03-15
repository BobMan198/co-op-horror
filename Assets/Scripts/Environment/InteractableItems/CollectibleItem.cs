using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleItem : InteractableItem
{
    public string collectibleName;
    public float moneyWorth;
    public override void Interact()
    {
        GameRunner.itemsCollected.Add(this);
        Destroy(this.gameObject);
    }
}
