using System.Collections.Generic;
using AI.KnowledgeBase;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    public class ResumeFight : IState
    {
        public ResumeFight(AIEntity entity)
        {
            this.entity = entity;
            targetKB = entity.GetComponent<TargetKnowledgeBase>();
        }

        public float CalculateTransitionScore()
        {
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKB.HasSeenTarget(true);
            return canSee ? 0.95f : 0.0f;
        }

        private AIEntity entity;
        private TargetKnowledgeBase targetKB;

        public void Enter()
        {
            entity.SetNewState(new Fight(entity));
        }
    
        public void Update()
        {
        }
    
        public void Exit()
        {
        }
    }
}