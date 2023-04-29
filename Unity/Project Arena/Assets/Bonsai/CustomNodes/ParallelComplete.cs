using Bonsai.Standard;

namespace Bonsai.CustomNodes
{
  [BonsaiNode("Composites/")]
  public class ParallelComplete : Parallel
  {
    public override Status Run()
    {
      if (IsAnyChildWithStatus(Status.Success))
      {
        return Status.Success;
      }

      if (IsAnyChildWithStatus(Status.Failure))
      {
        return Status.Failure;
      }

      // Process the sub-iterators.
      RunChildBranches();

      // Parallel iterators still running.
      return Status.Running;
    }
  }
}