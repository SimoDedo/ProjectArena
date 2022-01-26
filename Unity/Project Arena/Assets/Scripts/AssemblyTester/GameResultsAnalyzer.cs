using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyLogging;
using AssemblyUtils;
using UnityEngine;

namespace AssemblyTester
{
    public class GameResultsAnalyzer
    {
        // DONE!
        private readonly Dictionary<int, float> timeToEngage = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, float> timeInFight = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, float> timeBetweenSights = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, float> timeToSurrender = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, int> numberOfRetreats = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, int> numberOfFights = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, int> numberOfFrags = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, int> numberOfShots = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, int> numberOfHits = new Dictionary<int, int>();

        private readonly Dictionary<int, int> nonZeroKillStreaksCount = new Dictionary<int, int>();
        private readonly Dictionary<int, int> killStreaksSum = new Dictionary<int, int>();
        private readonly Dictionary<int, int> currentKillStreak = new Dictionary<int, int>();
        private readonly Dictionary<int, int> killStreakMax = new Dictionary<int, int>();

        public void Setup()
        {
            EntityGameMetricsGameEvent.Instance.AddListener(LogMetrics);
            KillInfoGameEvent.Instance.AddListener(LogKill);
            ShotInfoGameEvent.Instance.AddListener(LogShot);
            HitInfoGameEvent.Instance.AddListener(LogHit);
        }

        public void TearDown()
        {
            EntityGameMetricsGameEvent.Instance.RemoveListener(LogMetrics);
            KillInfoGameEvent.Instance.RemoveListener(LogKill);
            ShotInfoGameEvent.Instance.RemoveListener(LogShot);
            HitInfoGameEvent.Instance.RemoveListener(LogHit);
        }

        public void Reset()
        {
            timeToEngage.Clear();
            numberOfFights.Clear();
            timeInFight.Clear();
            numberOfRetreats.Clear();
            timeToSurrender.Clear();
            timeBetweenSights.Clear();
            
            numberOfFrags.Clear();
            numberOfShots.Clear();
            numberOfHits.Clear();
            killStreakMax.Clear();
            killStreaksSum.Clear();
            currentKillStreak.Clear();
            nonZeroKillStreaksCount.Clear();
        }

        private void LogMetrics(GameMetrics receivedInfo)
        {
            timeToEngage.Add(receivedInfo.entityId, receivedInfo.timeToEngage);
            numberOfFights.Add(receivedInfo.entityId, receivedInfo.numberOfFights);
            timeInFight.Add(receivedInfo.entityId, receivedInfo.timeInFights);
            numberOfRetreats.Add(receivedInfo.entityId, receivedInfo.numberOfRetreats);
            timeToSurrender.Add(receivedInfo.entityId, receivedInfo.timeToSurrender);
            timeBetweenSights.Add(receivedInfo.entityId, receivedInfo.timeBetweenSights);
        }

        private void LogHit(HitInfo receivedInfo)
        {
            numberOfHits.AddToKey(receivedInfo.hitterEntityID, 1);
        }

        private void LogShot(ShotInfo receivedInfo)
        {
            numberOfShots.AddToKey(receivedInfo.ownerId, 1);
        }

        private void LogKill(KillInfo receivedInfo)
        {
            numberOfFrags.AddToKey(receivedInfo.killerEntityID, 1);
            // TODO This is slightly wrong, what if I have a reciprocal kill? 
            // One of the entities would get the point now, and the other only after death.

            // Update killer streak info
            if (currentKillStreak.TryGetValue(receivedInfo.killerEntityID, out var currentStreak))
            {
                // Already started a streak, update value!
                currentKillStreak[receivedInfo.killerEntityID] = currentStreak + 1;
            }
            else
            {
                // No streak! Start a new one
                currentKillStreak[receivedInfo.killerEntityID] = 1;
                nonZeroKillStreaksCount.AddToKey(receivedInfo.killerEntityID, 1);
            }

            CloseKillStreak(receivedInfo.killedEntityID);
        }

        private void CloseKillStreak(int entityId)
        {
            // Update killed streak info
            if (currentKillStreak.TryGetValue(entityId, out var streak))
            {
                // The entity had a streak! Interrupt it now.
                killStreaksSum.AddToKey(entityId, streak);
                currentKillStreak.Remove(entityId);

                // Update max kill streak if needed
                var hadMax = killStreakMax.TryGetValue(entityId, out var max);
                if (!hadMax || streak > max)
                {
                    killStreakMax[entityId] = streak;
                }
            }
        }

        public Dictionary<string, object> CompileResults(float gameLength)
        {
            // TODO Is everything done by the end of the game?
            // e.g. killstreaks are closed, searches are over, ...

            // Compile accuracy data
            var totalKeys = new HashSet<int>(numberOfShots.Keys);
            totalKeys.UnionWith(numberOfHits.Keys);

            var accuracy = new Dictionary<int, float>();
            foreach (var key in totalKeys)
            {
                numberOfShots.TryGetValue(key, out var shots);
                numberOfHits.TryGetValue(key, out var hits);
                if (hits == 0)
                {
                    accuracy.Add(key, 0);
                }
                else
                {
                    accuracy.Add(key, hits / (float) shots);
                }
            }

            var killStreakAverage = new Dictionary<int, float>();
            foreach (var key in nonZeroKillStreaksCount.Keys)
            {
                CloseKillStreak(key);
                var count = nonZeroKillStreaksCount[key];
                killStreaksSum.TryGetValue(key, out var sum);
                killStreakAverage.Add(key, sum / (float) count);
            }

            var totalFrags = numberOfFrags.Sum(entry => entry.Value);
            var entropy = numberOfFrags.Sum(
                entry => -entry.Value / (float) totalFrags * Mathf.Log(entry.Value / (float) totalFrags, 2)
            );

            var numberOfFightsSum = this.numberOfFights.Sum(entry => entry.Value);
            var timeToEngageSum = timeToEngage.Sum(entry => entry.Value);
            var pace = 2 * 1 / (1 + Math.Exp(-3 * numberOfFightsSum / timeToEngageSum)) - 1;

            var timeInFightSum = timeInFight.Sum(entry => entry.Value);
            var timeBetweenSightsSum = timeBetweenSights.Sum(entry => entry.Value);
            var timeToSurrenderSum = timeToSurrender.Sum(entry => entry.Value);
            var numberOfRetreatsSum = numberOfRetreats.Sum(entry => entry.Value);
            var pursueTime = timeInFightSum / (2 * gameLength);
            var fightTime = (timeInFightSum - timeBetweenSightsSum - timeToSurrenderSum) / (2 * gameLength);

            var sightLossRate = timeBetweenSightsSum / timeInFightSum;
            var targetLossRate = numberOfRetreatsSum / (float) numberOfFightsSum;

            return new Dictionary<string, object>
            {
                {"timeInFight1", timeInFight.First().Value},
                {"timeInFight2", timeInFight.Last().Value},
                {"timeToEngage1", timeToEngage.First().Value},
                {"timeToEngage2", timeToEngage.Last().Value},
                {"numberOfFights1", numberOfFights.First().Value},
                {"numberOfFights2", numberOfFights.Last().Value},
                {"timeBetweenSights1", timeBetweenSights.First().Value},
                {"timeBetweenSights2", timeBetweenSights.Last().Value},
                {"timeToSurrender1", timeToSurrender.First().Value},
                {"timeToSurrender2", timeToSurrender.Last().Value},
                {"numberOfRetreats1", numberOfRetreats.First().Value},
                {"numberOfRetreats2", numberOfRetreats.Last().Value},
                {"numberOfFrags1", numberOfFrags.First().Value},
                {"numberOfFrags2", numberOfFrags.Last().Value},
                {"numberOfShots1", numberOfShots.First().Value},
                {"numberOfShots2", numberOfShots.Last().Value},
                {"numberOfHits1", numberOfHits.First().Value},
                {"numberOfHits2", numberOfHits.Last().Value},
                {"accuracy1", accuracy.First().Value},
                {"accuracy2", accuracy.Last().Value},
                {"killStreakAverage1", killStreakAverage.First().Value},
                {"killStreakAverage2", killStreakAverage.Last().Value},
                {"killStreakMax1", killStreakMax.First().Value},
                {"killStreakMax2", killStreakMax.Last().Value},
                {"entropy", entropy},
                {"pace", pace},
                {"fightTime", fightTime},
                {"pursueTime", pursueTime},
                {"sightLossRate", sightLossRate},
                {"targetLossRate", targetLossRate},
            };
        }
    }
}