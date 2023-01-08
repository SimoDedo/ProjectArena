using System;
using System.Collections.Generic;
using System.Text;
using Logging;
using UnityEngine;

namespace Tester
{
    /// <summary>
    /// Collects game statistics events and compiles them into their final results.
    /// </summary>
    public class DeathPositionAnalyzer
    {
        private readonly int bot1ID;
        private readonly int bot2ID;
        private readonly Dictionary<int, List<Vector2>> deathPositions = new Dictionary<int, List<Vector2>>();
        
        public DeathPositionAnalyzer(int bot1ID, int bot2ID)
        {
            this.bot1ID = bot1ID;
            this.bot2ID = bot2ID;
            deathPositions.Add(bot1ID, new List<Vector2>());
            deathPositions.Add(bot2ID, new List<Vector2>());
        }

        public void Setup()
        {
            KillInfoGameEvent.Instance.AddListener(LogDeathPosition);
        }

        public void TearDown()
        {
            KillInfoGameEvent.Instance.RemoveListener(LogDeathPosition);
        }

        public void Reset()
        {    
            deathPositions.Clear();
            deathPositions.Add(bot1ID, new List<Vector2>());
            deathPositions.Add(bot2ID, new List<Vector2>());
        }

        private void LogDeathPosition(KillInfo receivedInfo)
        {
            if (receivedInfo.killedEntityID == receivedInfo.killerEntityID)
            {
                // Suicide!
                return;
            }

            deathPositions[receivedInfo.killedEntityID].Add(new Vector2(receivedInfo.killedX, receivedInfo.killedZ));
        }

        public Tuple<string,string> CompileResultsAsCSV()
        {
            var positions1 = deathPositions[bot1ID];
            var positions2 = deathPositions[bot2ID];
            
            return new Tuple<string, string>(GetCSV(positions1), GetCSV(positions2));
        }

        private string GetCSV(List<Vector2> positions)
        {
            var csvBuilder = new StringBuilder();
            foreach (var position in positions)
            {
                csvBuilder.Append(position.x);
                csvBuilder.Append(',');
                csvBuilder.Append(position.y);
                csvBuilder.AppendLine();
            }

            return csvBuilder.ToString();
        }
    }
}