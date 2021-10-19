using System;
using System.Collections.Generic;
using System.Linq;
using AI.Guns;
using UnityEngine;

namespace AssemblyEntity.Component
{
    public class GunManager : MonoBehaviour
    {
        private List<Gun> guns;
        private List<GunScorer> gunScorers;
        private List<bool> ActiveGuns
        {
            get { return guns.Select(it => it.isActiveAndEnabled).ToList(); }
            set
            {
                if (value.Count != guns.Count)
                    throw new Exception("Error, ActiveGuns value must have same lenght of gun!");
                for (var i = 0; i < value.Count; i++)
                {
                    guns[i].enabled = value[i];
                }
            }
        }
        
        public int CurrentGunIndex { get; private set; } = NO_GUN;
        public int NumberOfGuns => guns.Count;

        public int FindLowestActiveGun()
        {
            return ActiveGuns.FindIndex(it => it);
        }
        
        // TODO RENAMING
        public bool TryEquipGun(int index)
        {
            if (index == NO_GUN)
                return TrySwitchGuns(CurrentGunIndex, index);
            
            if (index < 0 || index > guns.Count)
                return false;
            return ActiveGuns[index] && TrySwitchGuns(CurrentGunIndex, index);
        }

        // TODO Do not allow direct usage of Gun, create interface or new GunHandlingComponent

        public void Prepare()
        {
            guns = gameObject.GetComponentsInChildren<Gun>().ToList();
            gunScorers = guns.Select(it => it.GetComponent<GunScorer>()).ToList();
        }


        // Variables to slow down the gun switching.
        private float lastSwitched = float.MinValue;
        private const float switchWait = 0.05f;

        /// <returns>True if the gun currently active is the one requested</returns>
        public bool TrySwitchGuns(int toDeactivate, int toActivate)
        {
            if (Time.time > lastSwitched + switchWait)
            {
                if (toDeactivate != toActivate)
                {
                    lastSwitched = Time.time;
                    CurrentGunIndex = toActivate;
                    if (toDeactivate != NO_GUN)
                        guns[toDeactivate].Stow();
                    if (toActivate != NO_GUN)
                        guns[toActivate].Wield();
                }

                return true;
            }

            return toDeactivate == toActivate;
        }

        public void SetupGuns(GameManager gms, Entity parent, PlayerUIManager pms, bool[] ag)
        {
            for (var i = 0; i < ag.Length; i++)
            {
                // Setup the gun.
                var gun = guns[i];
                gun.SetupGun(gms, parent, pms, i + 1);
                ActiveGuns[i] = ag[i];
            }
        }

        public bool CanBeSupplied(bool[] suppliedGuns)
        {
            for (var i = 0; i < Math.Min(suppliedGuns.Length, guns.Count); i++)
            {
                if (suppliedGuns[i] && ActiveGuns[i] && !guns[i].IsFull())
                {
                    return true;
                }
            }

            return false;
        }

        public void SupplyGuns(bool[] suppliedGuns, int[] ammoAmounts)
        {
            for (var i = 0; i < suppliedGuns.Length; i++)
            {
                if (suppliedGuns[i] && ActiveGuns[i])
                {
                    guns[i].AddAmmo(ammoAmounts[i]);
                }
            }
        }
        
        public int GetMaxAmmoForGun(int index)
        {
            return guns[index].GetMaxAmmo();
        }
        public int GetCurrentAmmoForGun(int index)
        {
            return guns[index].GetCurrentAmmo();
        }
        
        public int GetCurrentChargerSize(int index)
        {
            return guns[index].GetAmmoClipSize();
        }
        
        public int GetCurrentAmmoInCharger(int index)
        {
            return guns[index].GetLoadedAmmo();
        }
        

        public void ResetAmmo()
        {
            for (var i = 0; i < guns.Count; i++)
            {
                if (ActiveGuns[i])
                {
                    guns[i].ResetAmmo();
                }
            }
        }
        
        public bool IsGunActive(int index)
        {
            return ActiveGuns[index];
        }

        public bool CanGunAim(int index)
        {
            return guns[index].CanAim();
        }
        
        public bool CanCurrentGunAim()
        {
            return guns[CurrentGunIndex].CanAim();
        }

        public void SetCurrentGunAim(bool aim)
        {
            guns[CurrentGunIndex].Aim(aim);
        }

        public bool CanGunShoot(int index)
        {
            return guns[index].CanShoot();
        }

        public bool CanCurrentGunShoot()
        {
            return guns[CurrentGunIndex].CanShoot();
        }
        public void ShootCurrentGun()
        {
            guns[CurrentGunIndex].Shoot();
        }

        public bool CanGunReload(int index)
        {
            return guns[index].CanReload();
        }

        public bool CanCurrentGunReload()
        {
            return guns[CurrentGunIndex].CanReload();
        }

        public void ReloadCurrentGun()
        {
            guns[CurrentGunIndex].Reload();
        }
        public float GetGunScore(int index, float distance)
        {
            return gunScorers[index].GetGunScore(distance);
        }

        public float GetGunProjectileSpeed(int index)
        {
            return guns[index].GetProjectileSpeed();
        }
        
        public float GetCurrentGunProjectileSpeed()
        {
            return guns[CurrentGunIndex].GetProjectileSpeed();
        }

        public bool IsCurrentGunReloading()
        {
            return guns[CurrentGunIndex].IsReloading;
        }

        public Tuple<float,float> GetCurrentAmmoOptimalRange()
        {
            return gunScorers[CurrentGunIndex].GetOptimalRange();
        }
        
        public const int NO_GUN = -1;

    }
}