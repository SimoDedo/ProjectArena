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
    public class KillDistanceAnalyzer
    {
        private readonly int bot1ID;
        private readonly int bot2ID;
        private readonly Dictionary<int, List<float>> killDistances = new Dictionary<int, List<float>>();
        
        public KillDistanceAnalyzer(int bot1ID, int bot2ID)
        {
            this.bot1ID = bot1ID;
            this.bot2ID = bot2ID;
            killDistances.Add(bot1ID, new List<float>());
            killDistances.Add(bot2ID, new List<float>());
        }

        public void Setup()
        {
            KillInfoGameEvent.Instance.AddListener(LogKillDistance);
        }

        public void TearDown()
        {
            KillInfoGameEvent.Instance.RemoveListener(LogKillDistance);
        }

        public void Reset()
        {    
            killDistances.Clear();
            killDistances.Add(bot1ID, new List<float>());
            killDistances.Add(bot2ID, new List<float>());
        }

        private void LogKillDistance(KillInfo receivedInfo)
        {
            if (receivedInfo.killedEntityID == receivedInfo.killerEntityID)
            {
                // Suicide!
                return;
            }

            var distance = Mathf.Sqrt(Mathf.Pow(receivedInfo.killedX - receivedInfo.killerX, 2) +
                                      Mathf.Pow(receivedInfo.killedZ - receivedInfo.killerZ, 2)
            );
            killDistances[receivedInfo.killerEntityID].Add(distance);
        }

        public Tuple<string,string> CompileResultsAsCSV()
        {
            var csvBuilder = new StringBuilder();
            
            foreach (var position in killDistances[bot1ID])
            {
                csvBuilder.Append(position);
                csvBuilder.AppendLine();
            }

            var pos1 = csvBuilder.ToString();
            csvBuilder.Clear();
            foreach (var position in killDistances[bot2ID])
            {
                csvBuilder.Append(position);
                csvBuilder.AppendLine();
            }
            var pos2 = csvBuilder.ToString();

            return new Tuple<string, string>(pos1, pos2);
        }
    }
}