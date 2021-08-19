// using BehaviorDesigner.Runtime;
// using BehaviorDesigner.Runtime.Tasks;

// TODO

// namespace AI
// {
//     public class ShouldLookForAmmo
//     {
//     using BehaviorDesigner.Runtime;
//     using BehaviorDesigner.Runtime.Tasks;
//
//     namespace AI
//     {
//         public class ShouldLookForAmmo: Conditional
//         {
//             public SharedInt thresholdLowAmmo;
//             private Entity entity;
//             public override void OnAwake()
//             {
//                 entity = GetComponent<Entity>();
//             }
//
//             public override TaskStatus OnUpdate()
//             {
//                 return entity.amm < thresholdLowAmmo.Value ? TaskStatus.Success : TaskStatus.Failure;
//             }
//         }
//     }
//     
// }