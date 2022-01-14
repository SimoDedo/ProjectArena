using System;
using System.Collections.Generic;
using Accord.Statistics.Kernels;
using AssemblyLogging;
using AssemblyUtils;
using Newtonsoft.Json;
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
        }

        public void TearDown()
        {
            SearchInfoGameEvent.Instance.RemoveListener(LogSearchInfo);
            FightingStatusGameEvent.Instance.RemoveListener(LogFightStatus);
            EnemyAwarenessStatusGameEvent.Instance.RemoveListener(LogAwareness);
            SpawnInfoGameEvent.Instance.RemoveListener(LogSpawn);
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

        public void CompileResults()
        {
            // Just show the results?
            Debug.Log("TimeToEngage: " + JsonConvert.SerializeObject(timeToEngage));
            Debug.Log("TimeInFight: " + JsonConvert.SerializeObject(timeInFight));
            Debug.Log("NumberOfFights: " + JsonConvert.SerializeObject(numberOfFights));
            Debug.Log("TimeBetweenSights: " + JsonConvert.SerializeObject(timeBetweenSights));
            Debug.Log("NumberOfSights: " + JsonConvert.SerializeObject(numberOfSights));
            Debug.Log("TimeToSurrender: " + JsonConvert.SerializeObject(timeToSurrender));
            Debug.Log("NumberOfRetreats: " + JsonConvert.SerializeObject(numberOfRetreats));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(respawnTime));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(latestAwarenessStatus));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(startFightingTime));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(endFightingTime));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(endDetectEnemyTime));
            // Debug.Log("Results: " + JsonConvert.SerializeObject(latestFightInfo));
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
        }
    }
}