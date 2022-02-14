using System.Collections.Generic;
using System.Linq;
using AI.AI.Layer1;
using Pickables;
using UnityEngine;

namespace AI.AI.Layer2
{
    /// <summary>
    /// This component deals with keeping track of the activation times of all the pickups in the map.
    /// </summary>
    public class PickupKnowledgeBase
    {
        private readonly Dictionary<Pickable, float> estimatedActivationTime = new Dictionary<Pickable, float>();
        private readonly AIEntity me;
        private SightSensor sightSensor;

        public PickupKnowledgeBase(AIEntity me)
        {
            this.me = me;
            DetectPickups();
        }

        /// <summary>
        /// Finish setting up the entity.
        /// </summary>
        public void Prepare()
        {
            sightSensor = me.SightSensor;
        }

        // Detects all the pickups in the scene.
        private void DetectPickups()
        {
            var spawners = Object.FindObjectsOfType<Pickable>();
            foreach (var spawner in spawners) estimatedActivationTime.Add(spawner, Time.time);
        }

        /// <summary>
        /// Updates the knowledge base.
        /// </summary>
        public void Update()
        {
            var keyList = new List<Pickable>(estimatedActivationTime.Keys);
            foreach (var pickable in keyList)
            {
                var position = pickable.gameObject.transform.position;
                // Let us give the bot a slightly unfair advantage: if they are close to the powerup, they
                // know its status. This is because it would be very easy for a human to turn around and check, 
                // but complex (to implement) in the AI of the bot.
                if ((position - me.transform.position).sqrMagnitude > 4 &&
                    !sightSensor.CanSeeObject(pickable.transform, Physics.AllLayers)) continue;
                if (pickable.IsActive)
                    // if(estimatedActivationTime[pickable] > Time.time)
                    // {
                    estimatedActivationTime[pickable] = Time.time;
                // }  
                // If we believed that the pickup was already active, then update the value to the average
                // possible remaining time (aka cooldown / 2)
                // Otherwise we already have an estimate on when the object will respawn, so do not make any new
                // assumption
                else if (estimatedActivationTime[pickable] < Time.time)
                    // TODO IF I CAN SEE THE OBJECT BEING PICKED UP, COOLDOWN IS NOT HALVED
                    //  HOW TO PROPERLY DETECT PICKUP IS ENEMY IS IN FRONT?
                    //  What if I simply store the pickups I can see each turn an use that to understand if a state
                    //  changed in front of me? Ignore the entity layer however. 
                    estimatedActivationTime[pickable] = Time.time + pickable.Cooldown / 2;
            }
        }

        /// <summary>
        /// Get the list of pickups saved in the knowledge base.
        /// </summary>
        public List<Pickable> GetPickups()
        {
            return estimatedActivationTime.Keys.ToList();
        }


        /// <summary>
        /// Returns the estimated activation times for each power up handled by the knowledge base.
        /// </summary>
        public Dictionary<Pickable, float> GetPickupsEstimatedActivationTimes()
        {
            // TODO Is this a copy or a reference of the dictionary?
            return estimatedActivationTime;
        }

        /// <summary>
        /// Marks a specific pickup as consumed. The knowledge base can use this information to better estimate
        /// the next activation time.
        /// </summary>
        public void MarkConsumed(Pickable pickable)
        {
            estimatedActivationTime[pickable] = Time.time + pickable.Cooldown;
        }
    }
}