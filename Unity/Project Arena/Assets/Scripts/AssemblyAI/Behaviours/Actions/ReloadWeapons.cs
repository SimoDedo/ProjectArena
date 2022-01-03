using AssemblyEntity.Component;
using BehaviorDesigner.Runtime.Tasks;

namespace AssemblyAI.Behaviours.Actions
{
    // TODO Associate skill to prevent n00b players from reloading like this
    // TODO Should probably add activation chance so that we don't just start reloading every other second
    public class ReloadWeapons : Action
    {
        private GunManager gunManager;

        public override void OnAwake()
        {
            gunManager = GetComponent<AIEntity>().GunManager;
        }

        public override void OnStart()
        {
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
            {
                gunManager.TryEquipGun(mostUnchargedGun);
                var currentGun = gunManager.CurrentGunIndex;
                if (gunManager.CanGunReload(currentGun))
                {
                    gunManager.ReloadCurrentGun();
                }
            }
        }

        public override TaskStatus OnUpdate()
        {
            return gunManager.IsCurrentGunReloading() ? TaskStatus.Running : TaskStatus.Success;
        }
    }
}