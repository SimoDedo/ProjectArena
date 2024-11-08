using Bonsai;
using Entity.Component;
using UnityEngine;
using Random = UnityEngine.Random;
using Task = Bonsai.Core.Task;

namespace AI.BonsaiBehaviours.Actions
{
    // TODO Associate skill to prevent n00b players from reloading like this
    /// <summary>
    /// Reloads weapons if needed, but not necessarily every time.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class ReloadWeapons : Task
    {
        private const float TIMEOUT = 1.5f;
        private const float RELOAD_PROBABILITY = 0.8f;
        private GunManager gunManager;
        private float nextWeaponChoiceTime = float.MinValue;
        private bool shouldReload;

        public override void OnStart()
        {
            gunManager = Actor.GetComponent<AIEntity>().GunManager;
        }

        public override Status Run()
        {
            if (nextWeaponChoiceTime < Time.time)
            {
                shouldReload = EquipGunToReload();
                nextWeaponChoiceTime = Time.time + TIMEOUT;
            }

            if (shouldReload)
            {
                var currentGun = gunManager.CurrentGunIndex;
                if (gunManager.CanGunReload(currentGun)) gunManager.ReloadCurrentGun();
            }

            return Status.Running;
        }

        private bool EquipGunToReload()
        {
            if (Random.value > RELOAD_PROBABILITY)
                // Do not reload for this timeout
                return false;

            var gunCount = gunManager.NumberOfGuns;
            var mostUnchargedGun = GunManager.NO_GUN;
            var worstPercentage = 1f;
            for (var i = 0; i < gunCount; i++)
            {
                var ammoInCharger = gunManager.GetAmmoInChargerForGun(i);
                var chargerSize = gunManager.GetChargerSizeForGun(i);
                var percentage = ammoInCharger / (float) chargerSize;
                if (percentage < worstPercentage)
                {
                    mostUnchargedGun = i;
                    worstPercentage = percentage;
                }
            }

            if (mostUnchargedGun != GunManager.NO_GUN)
                // Reload, but only if you manage to switch gun
                return gunManager.TryEquipGun(mostUnchargedGun);

            return false; // No weapon to reload
        }
    }
}