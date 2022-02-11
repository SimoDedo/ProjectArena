using System.Collections.Generic;
using System.Linq;
using AssemblyAI.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.KnowledgeBase
{
    public class PickupKnowledgeBase
    {
        private readonly AIEntity me;
        private AISightSensor sightSensor;

        private readonly Dictionary<Pickable, float> estimatedActivationTime = new Dictionary<Pickable, float>();

        public PickupKnowledgeBase(AIEntity me)
        {
            this.me = me;
            DetectPickups();
        }

        public void Prepare()
        {
            sightSensor = me.SightSensor;
        }

        private void DetectPickups()
        {
            var spawners = Object.FindObjectsOfType<Pickable>();
            foreach (var spawner in spawners)
            {
                estimatedActivationTime.Add(spawner, Time.time);
            }
        }

        public void Update()
        {
            var keyList = new List<Pickable>(estimatedActivationTime.Keys);
            foreach (var pickable in keyList)
            {
                var position = pickable.gameObject.transform.position;
                // Let us give the bot a slightly unfair advantage: if they are close to the powerup, they
                // know its status. This is because it would be very easy for a human to turn around and check, 
                // but complex (to implement) in the AI of the bot.
                if ((position - me.transform.position).sqrMagnitude > 4 && !sightSensor.CanSeeObject(pickable.transform, Physics.AllLayers)) continue;
                if (pickable.IsActive)
                {
                    // if(estimatedActivationTime[pickable] > Time.time)
                    // {
                    estimatedActivationTime[pickable] = Time.time;
                    // }  
                }
                // If we believed that the pickup was already active, then update the value to the average
                // possible remaining time (aka cooldown / 2)
                // Otherwise we already have an estimate on when the object will respawn, so do not make any new
                // assumption
                else if (estimatedActivationTime[pickable] < Time.time)
                {
                    // TODO IF I CAN SEE THE OBJECT BEING PICKED UP, COOLDOWN IS NOT HALVED
                    //  HOW TO PROPERLY DETECT PICKUP IS ENEMY IS IN FRONT?
                    estimatedActivationTime[pickable] = Time.time + pickable.Cooldown / 2;
                }
            }
        }

        public List<Pickable> GetPickups()
        {
            return estimatedActivationTime.Keys.ToList();
        }
        
        public Dictionary<Pickable, float> GetPickupsEstimatedActivationTimes()
        {
            // TODO Is this a copy or a reference of the dictionary?
            return estimatedActivationTime
                // .ToDictionary(pair => pair.Key, pair => pair.Value)
                ;
        }

        public void MarkConsumed(Pickable pickable)
        {
            estimatedActivationTime[pickable] = Time.time + pickable.Cooldown;
        }
    }
}