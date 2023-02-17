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
        private readonly Dictionary<int, List<Vector4>> deathAndKillPositions = new();
        
        public DeathPositionAnalyzer(int bot1ID, int bot2ID)
        {
            this.bot1ID = bot1ID;
            this.bot2ID = bot2ID;
            deathAndKillPositions.Add(bot1ID, new List<Vector4>());
            deathAndKillPositions.Add(bot2ID, new List<Vector4>());
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
            deathAndKillPositions.Clear();
            deathAndKillPositions.Add(bot1ID, new List<Vector4>());
            deathAndKillPositions.Add(bot2ID, new List<Vector4>());
        }

        private void LogDeathPosition(KillInfo receivedInfo)
        {
            if (receivedInfo.killedEntityID == receivedInfo.killerEntityID)
            {
                // Suicide!
                return;
            }

            deathAndKillPositions[receivedInfo.killedEntityID].Add(
                new Vector4(receivedInfo.killedX, receivedInfo.killedZ, receivedInfo.killerX, receivedInfo.killerZ)
                );
        }

        public Tuple<string,string> CompileResultsAsCSV()
        {
            var positions1 = deathAndKillPositions[bot1ID];
            var positions2 = deathAndKillPositions[bot2ID];
            
            return new Tuple<string, string>(GetCSV(positions1), GetCSV(positions2));
        }

        private string GetCSV(List<Vector4> positions)
        {
            var csvBuilder = new StringBuilder();
            foreach (var position in positions)
            {
                csvBuilder.Append(position.x);
                csvBuilder.Append(',');
                csvBuilder.Append(position.y);
                csvBuilder.Append(',');
                csvBuilder.Append(position.z);
                csvBuilder.Append(',');
                csvBuilder.Append(position.w);
                csvBuilder.AppendLine();
            }

            return csvBuilder.ToString();
        }
    }
}