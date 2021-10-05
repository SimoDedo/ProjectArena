using System;
using System.Linq;
using AI.Behaviours.NewBehaviours.Variables;
using AI.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class ChooseHealthPack: Action
    {
        [SerializeField] private SharedSelectedPickupInfo pickupChosen;
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private PickupKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            knowledgeBase = GetComponent<PickupKnowledgeBase>();
            navSystem = GetComponent<NavigationSystem>();
        }

        // TODO What if there are no health pickups? Should I handle this case here or in the FSM?
        public override TaskStatus OnUpdate()
        {
            var pickups = knowledgeBase.GetPickupKnowledgeForType(Pickable.PickupType.MEDKIT);
            var bestPath = new NavMeshPath();
            var bestTime = float.MaxValue;
            var bestMedkit = pickups.First();
            foreach (var medkit in pickups)
            {
                var position = medkit.Key.transform.position;
                var path = navSystem.CalculatePath(position);
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    var estimatedArrivalTime = navSystem.GetEstimatedPathDuration(path);
                    var timeRemaining = medkit.Value - Time.time;
                    var worstTime = Math.Max(timeRemaining, estimatedArrivalTime);
                    if (worstTime < bestTime)
                    {
                        bestTime = worstTime;
                        bestPath = path;
                        bestMedkit = medkit;
                    }
                }
            }

            pickupChosen.Value = new SelectedPickupInfo
            {
                pickup = bestMedkit.Key,
                estimatedActivationTime = bestMedkit.Value
            };
            pathChosen.Value = bestPath;
            
            return TaskStatus.Success;
        }
    }
}