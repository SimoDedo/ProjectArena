using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.State
{
    // TODO This for now is exactly like SearchForLostEnemy, but with different transition
    public class SearchForDamageSource : IState
    {
        public SearchForDamageSource(AIEntity entity)
        {
            this.entity = entity;
        }
    
        private AIEntity entity;

        public float CalculateTransitionScore()
        {
            if (entity.GetEnemy().isAlive && entity.HasTakenDamageRecently())
            {
                return 0.7f;
            }
            return 0f;
        }
        public void Enter()
        {
            entity.SetNewState(new SearchForLostEnemy(entity));
        }
    
        public void Update()
        {
        }
    
        public void Exit()
        {
        }
    }
}