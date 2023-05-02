using AI.Layers.KnowledgeBase;
using Bonsai;
    using Bonsai.Core;

    namespace AI.BonsaiBehaviours.Conditions
    {
        /// <summary>
        /// Returns Success if the entity possesses Area knowledge, Failure otherwise.
        /// </summary>
        [BonsaiNode("Conditional/")]
        public class HasAreaKnowledge : ConditionalAbort
        {
            private MapWanderPlanner wanderPlanner;

            public override void OnStart()
            {
                wanderPlanner = Actor.GetComponent<AIEntity>().MapWanderPlanner;
            }

            public override bool Condition()
            {
                return wanderPlanner.CanBeUsed;
            }

            public override Status Run()
            {
                return Condition() ? Status.Success : Status.Failure;
            }
        }
    }