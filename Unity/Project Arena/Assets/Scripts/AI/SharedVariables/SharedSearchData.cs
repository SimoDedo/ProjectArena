using BehaviorDesigner.Runtime;

namespace AI.SharedVariables
{
    [System.Serializable]
    public struct SearchData
    {
        public bool shouldSearch;
        public float timeSetFlag;
    }

    [System.Serializable]
    public class SharedSearchData : SharedVariable<SearchData>
    {
        public static implicit operator SharedSearchData(SearchData value)
        {
            return new SharedSearchData {Value = value};
        }
    }
}