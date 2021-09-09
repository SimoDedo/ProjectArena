using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.UnityGameObject;
using UnityEditor.VersionControl;

namespace AI.State
{
    public interface IState
    {
        public void Enter();
        public void Update();
        public void Exit();
    }


    public class Idle : IState
    {
        public Idle()
        {
        }

        public void Enter()
        {
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}