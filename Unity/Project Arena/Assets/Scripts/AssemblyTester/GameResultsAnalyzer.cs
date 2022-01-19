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
        private readonly Dictionary<int, int> numberOfFights = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, float> timeBetweenSights = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, int> numberOfSights = new Dictionary<int, int>();

        // DONE!
        private readonly Dictionary<int, float> timeToSurrender = new Dictionary<int, float>();

        // DONE!
        private readonly Dictionary<int, int> numberOfRetreats = new Dictionary<int, int>();

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

        private readonly Dictionary<int, float> respawnTime = new Dictionary<int, float>();
        private readonly Dictionary<int, float> startFightingTime = new Dictionary<int, float>();
        private readonly Dictionary<int, float> endFightingTime = new Dictionary<int, float>();

        private readonly Dictionary<int, float> endDetectEnemyTime = new Dictionary<int, float>();

        private readonly Dictionary<int, EnemyAwarenessStatus> latestAwarenessStatus =
            new Dictionary<int, EnemyAwarenessStatus>();

        private readonly Dictionary<int, FightingStatus> latestFightInfo =
            new Dictionary<int, FightingStatus>();

        public void Setup()
        {
            SearchInfoGameEvent.Instance.AddListener(LogSearchInfo);
            FightingStatusGameEvent.Instance.AddListener(LogFightStatus);
            EnemyAwarenessStatusGameEvent.Instance.AddListener(LogAwareness);
            SpawnInfoGameEvent.Instance.AddListener(LogSpawn);
            KillInfoGameEvent.Instance.AddListener(LogKill);
            ShotInfoGameEvent.Instance.AddListener(LogShot);
            HitInfoGameEvent.Instance.AddListener(LogHit);
        }


        public void TearDown()
        {
            SearchInfoGameEvent.Instance.RemoveListener(LogSearchInfo);
            FightingStatusGameEvent.Instance.RemoveListener(LogFightStatus);
            EnemyAwarenessStatusGameEvent.Instance.RemoveListener(LogAwareness);
            SpawnInfoGameEvent.Instance.RemoveListener(LogSpawn);
            KillInfoGameEvent.Instance.RemoveListener(LogKill);
            ShotInfoGameEvent.Instance.RemoveListener(LogShot);
            HitInfoGameEvent.Instance.RemoveListener(LogHit);
        }

        public void Reset()
        {
            timeToEngage.Clear();
            timeInFight.Clear();
            numberOfFights.Clear();
            timeBetweenSights.Clear();
            numberOfSights.Clear();
            timeToSurrender.Clear();
            numberOfRetreats.Clear();
            respawnTime.Clear();
            latestAwarenessStatus.Clear();
            startFightingTime.Clear();
            endFightingTime.Clear();
            endDetectEnemyTime.Clear();
            latestFightInfo.Clear();
            numberOfFrags.Clear();
            numberOfShots.Clear();
            numberOfHits.Clear();
            killStreakMax.Clear();
            killStreaksSum.Clear();
            currentKillStreak.Clear();
            nonZeroKillStreaksCount.Clear();
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

        private void LogSpawn(SpawnInfo receivedInfo)
        {
            HandleRespawn(receivedInfo.entityId);
        }

        private void HandleRespawn(int entityId)
        {
            // Time to engage stats
            respawnTime[entityId] = Time.time;
            // No other relevant stat
        }

        private void LogAwareness(EnemyAwarenessStatus receivedInfo)
        {
            var hasValue = latestAwarenessStatus.TryGetValue(receivedInfo.observerID, out var storedInfo);
            if (!hasValue || storedInfo.isEnemyDetected != receivedInfo.isEnemyDetected)
            {
                latestAwarenessStatus[receivedInfo.observerID] = receivedInfo;
                if (receivedInfo.isEnemyDetected)
                {
                    ProcessAcquireEnemyDetection(receivedInfo.observerID);
                }
                else
                {
                    ProcessLostEnemyDetection(receivedInfo.observerID);
                }
            }
        }

        private void ProcessLostEnemyDetection(int entityId)
        {
            endDetectEnemyTime[entityId] = Time.time;
        }


        private void ProcessAcquireEnemyDetection(int entityId)
        {
            UpdateTimeBetweenSights(entityId);
        }

        private void UpdateTimeBetweenSights(int entityId)
        {
            if (endDetectEnemyTime.TryGetValue(entityId, out var endTime))
            {
                timeBetweenSights.AddToKey(entityId, Time.time - endTime);
            }

            numberOfSights.AddToKey(entityId, 1);
        }

        private void LogFightStatus(FightingStatus receivedInfo)
        {
            var hasValue = latestFightInfo.TryGetValue(receivedInfo.entityId, out var storedInfo);
            if (hasValue && storedInfo.isActivelyFighting != receivedInfo.isActivelyFighting)
            {
                // There was a change in fighting status now!
                if (receivedInfo.isActivelyFighting)
                {
                    ProcessEnteredFight(receivedInfo.entityId);
                }
                else
                {
                    ProcessExitedFight(receivedInfo.entityId);
                }
            }

            latestFightInfo[receivedInfo.entityId] = receivedInfo;
        }

        private void ProcessEnteredFight(int entityId)
        {
            startFightingTime[entityId] = Time.time;
            numberOfFights.AddToKey(entityId, 1);
            UpdateTimeToEngage(entityId);
        }

        private void UpdateTimeToEngage(int entityId)
        {
            respawnTime.TryGetValue(entityId, out var respawn);
            endFightingTime.TryGetValue(entityId, out var endFight);
            var startEngage = Mathf.Max(respawn, endFight);
            timeToEngage.AddToKey(entityId, Time.time - startEngage);
        }

        private void ProcessExitedFight(int entityId)
        {
            endFightingTime[entityId] = Time.time;
            // Time in fight stats
            UpdateTimeInFight(entityId);
        }

        private void UpdateTimeInFight(int entityId)
        {
            if (startFightingTime.TryGetValue(entityId, out var startFightTime))
            {
                timeInFight.AddToKey(entityId, Time.time - startFightTime);
            }
        }

        private readonly Dictionary<int, Tuple<float, SearchInfo>> latestSearchInfo =
            new Dictionary<int, Tuple<float, SearchInfo>>();

        // This method should keep track of the previous search info reported by info.searcherID and update
        // if required.
        // If the new info has the same search start time, update the previous time used, otherwise close the 
        // previous search (if any) and open a new one.
        // When opening a new search, update the number of searches performed by the entity
        private void LogSearchInfo(SearchInfo receivedInfo)
        {
            var hasValue = latestSearchInfo.TryGetValue(receivedInfo.searcherId, out var storedInfo);
            if (!hasValue || storedInfo.Item2.timeLastSight < receivedInfo.timeLastSight)
            {
                timeToSurrender.TryGetValue(receivedInfo.searcherId, out var previousTime);
                timeToSurrender[receivedInfo.searcherId] =
                    previousTime + (Time.time - receivedInfo.timeLastSight);

                numberOfRetreats.AddToKey(receivedInfo.searcherId, 1);
            }
            else
            {
                // The value refers to the same search, update previous!
                timeToSurrender.TryGetValue(receivedInfo.searcherId, out var previousTime);
                timeToSurrender[receivedInfo.searcherId] =
                    previousTime + (Time.time - receivedInfo.timeLastSight) -
                    (storedInfo.Item1 - storedInfo.Item2.timeLastSight);
            }

            latestSearchInfo[receivedInfo.searcherId] = new Tuple<float, SearchInfo>(Time.time, receivedInfo);
        }

        public Dictionary<string, object> CompileResults()
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
                {"killStreakMax2", killStreakMax.Last().Value}
            };
        }
    }
}