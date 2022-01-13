using System;
using System.Collections.Generic;
using AssemblyLogging;
using AssemblyUtils;
using UnityEngine;

namespace AssemblyTester
{
    public class GameResultsAnalyzer
    {
        public void Setup()
        {
            // PositionInfoGameEvent.Instance.AddListener(LogPosition);
            // GameInfoGameEvent.Instance.AddListener(LogGameInfo);
            // MapInfoGameEvent.Instance.AddListener(LogMapInfo);
            // ReloadInfoGameEvent.Instance.AddListener(LogReload);
            // ShotInfoGameEvent.Instance.AddListener(LogShot);
            // KillInfoGameEvent.Instance.AddListener(LogKill);
            // HitInfoGameEvent.Instance.AddListener(LogHit);
            //
            // EnemyInSightGameEvent.Instance.AddListener(LogEnemyInSight);
            // EnemyOutOfSightGameEvent.Instance.AddListener(LogEnemyOutOfSight);

            SearchInfoGameEvent.Instance.AddListener(LogSearchInfo);
            FightingStatusGameEvent.Instance.AddListener(LogFightStatus);
            EnemyAwarenessStatusGameEvent.Instance.AddListener(LogAwareness);
            SpawnInfoGameEvent.Instance.AddListener(LogSpawn);
        }
        
        public void TearDown()
        {
            // PositionInfoGameEvent.Instance.RemoveListener(LogPosition);
            // GameInfoGameEvent.Instance.RemoveListener(LogGameInfo);
            // MapInfoGameEvent.Instance.RemoveListener(LogMapInfo);
            // ReloadInfoGameEvent.Instance.RemoveListener(LogReload);
            // ShotInfoGameEvent.Instance.RemoveListener(LogShot);
            // SpawnInfoGameEvent.Instance.RemoveListener(LogSpawn);
            // KillInfoGameEvent.Instance.RemoveListener(LogKill);
            // HitInfoGameEvent.Instance.RemoveListener(LogHit);
            //
            // EnemyInSightGameEvent.Instance.RemoveListener(LogEnemyInSight);
            // EnemyOutOfSightGameEvent.Instance.RemoveListener(LogEnemyOutOfSight);
            SearchInfoGameEvent.Instance.RemoveListener(LogSearchInfo);
            FightingStatusGameEvent.Instance.RemoveListener(LogFightStatus);
            EnemyAwarenessStatusGameEvent.Instance.RemoveListener(LogAwareness);
            SpawnInfoGameEvent.Instance.RemoveListener(LogSpawn);

        }
        
        private void LogSpawn(SpawnInfo receivedInfo)
        {
            HandleRespawn(receivedInfo.entityId);
        }
        
        // TODO Handle death!
        // In case of death:
        // - I stop fights, if any (TimeInFight). Done by resetting the goal machine (if I was fighting, I exit)
        // - I stop searches, if any (TimeToSurrender). Done by resetting the goal machine
        // - I stop being aware of the enemy. (Done by the TargetKnowledgeBase.Reset())

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

        
        private readonly Dictionary<int, float> respawnTime = new Dictionary<int, float>();
        private readonly Dictionary<int, float> startFightingTime = new Dictionary<int, float>();
        private readonly Dictionary<int, float> endFightingTime = new Dictionary<int, float>();

        private readonly Dictionary<int, float> endDetectEnemyTime = new Dictionary<int, float>();

        private void HandleRespawn(int entityId)
        {
            // Time to engage stats
            respawnTime[entityId] = Time.time;
            // No other relevant stat
        }

        private readonly Dictionary<int, EnemyAwarenessStatus> latestAwarenessStatus =
            new Dictionary<int, EnemyAwarenessStatus>();

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

        private readonly Dictionary<int, Tuple<float, FightingStatus>> latestFightInfo =
            new Dictionary<int, Tuple<float, FightingStatus>>();

        private void LogFightStatus(FightingStatus receivedInfo)
        {
            var hasValue = latestFightInfo.TryGetValue(receivedInfo.entityId, out var storedInfo);
            if (!hasValue || storedInfo.Item1 < Time.time &&
                storedInfo.Item2.isActivelyFighting != receivedInfo.isActivelyFighting)
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

            latestFightInfo[receivedInfo.entityId] = new Tuple<float, FightingStatus>(Time.time, receivedInfo);
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
            var startFightTime = startFightingTime[entityId];
            timeInFight.AddToKey(entityId,Time.time - startFightTime);
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

        public void CompileResults() { }

        public void Reset() { }
    }
}