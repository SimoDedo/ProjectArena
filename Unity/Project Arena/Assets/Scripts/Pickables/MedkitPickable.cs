﻿using UnityEngine;

namespace Pickables
{
    /// <summary>
    ///     Medkit Pickable is an implementation of Pickable that heals the player.
    /// </summary>
    public class MedkitPickable : Pickable
    {
        [SerializeField] private int restoredHealth = 50;

        public int RestoredHealth => restoredHealth;

        protected override bool CanBePicked(GameObject entity)
        {
            return entity.GetComponent<Entity.Entity>().CanBeHealed();
        }

        protected override void PickUp(GameObject entity)
        {
            entity.GetComponent<Entity.Entity>().HealFromMedkit(this);
        }
    }
}