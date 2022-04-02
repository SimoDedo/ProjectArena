using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logging;
using UnityEngine;
using Utils;

namespace Tester
{
    /// <summary>
    /// Collects game statistics events and compiles them into their final results.
    /// </summary>
    public class PositionAnalyzer
    {
        private readonly int bot1ID;
        private readonly int bot2ID;

        private readonly Dictionary<int, List<Vector2>> positions = new Dictionary<int, List<Vector2>>();
        
        public PositionAnalyzer(int bot1ID, int bot2ID)
        {
            this.bot1ID = bot1ID;
            this.bot2ID = bot2ID;
            positions[bot1ID] = new List<Vector2>();
            positions[bot2ID] = new List<Vector2>();
        }

        public void Setup()
        {
            PositionInfoGameEvent.Instance.AddListener(LogPosition);
        }

        private void LogPosition(PositionInfo obj)
        {
            positions[obj.entityID].Add(new Vector2(Mathf.RoundToInt(obj.x), Mathf.RoundToInt(obj.z)));
        }

        public void TearDown()
        {
            PositionInfoGameEvent.Instance.RemoveListener(LogPosition);
        }

        public void Reset()
        {    
            positions[bot1ID].Clear();
            positions[bot2ID].Clear();
        }
        
        public Tuple<string,string> CompileResultsAsCSV()
        {
            var positions1 = positions[bot1ID];
            var positions2 = positions[bot2ID];
            
            return new Tuple<string, string>(GetCSV(positions1), GetCSV(positions2));
        }

        private string GetCSV(List<Vector2> positions)
        {
            var csvBuilder = new StringBuilder();
            foreach (var position in positions)
            {
                csvBuilder.Append(position.x);
                csvBuilder.Append(';');
                csvBuilder.Append(position.y);
                csvBuilder.AppendLine();
            }

            return csvBuilder.ToString();
        }
    }
}