using System.Collections.Generic;
using AssemblyLogging;
using AssemblyUtils;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace AssemblyTester
{
    public class GameResultsAnalyzer
    {
        // Current distance.
        private readonly Dictionary<int, float> distancesBetweenKills = new Dictionary<int, float>();

        // Total distance.
        private readonly Dictionary<int, float> totalDistances = new Dictionary<int, float>();

        // Total shots.
        private readonly Dictionary<int, int> shotCounts = new Dictionary<int, int>();

        // Total hits.
        private readonly Dictionary<int, int> hitsTaken = new Dictionary<int, int>();

        // Total destroyed targets.
        private readonly Dictionary<int, int> killCounts = new Dictionary<int, int>();

        // Position of the players.
        private readonly Dictionary<int, Vector2> lastPositions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, Vector2> initialPositions = new Dictionary<int, Vector2>();

        private readonly Dictionary<int, float> lastFightStartTime = new Dictionary<int, float>();

        /// <summary>
        /// Represents the last time when a fight ended or the last time the entity respawned.
        /// </summary>
        private readonly Dictionary<int, float> lastFightEndTime = new Dictionary<int, float>();

        private readonly Dictionary<int, float> totalTimeInFight = new Dictionary<int, float>();
        private readonly Dictionary<int, float> totalTimeToEngage = new Dictionary<int, float>();

        // /// <summary>
        // /// Last time when the enemy entered sight and the entity could react to that
        // /// </summary>
        // private readonly Dictionary<int, float> lastEnemyInSightTime = new Dictionary<int, float>();

        /// <summary>
        /// Last time when the enemy got out of sight and the entity could react to that
        /// </summary>
        private readonly Dictionary<int, float> lastEnemyOutOfSightTime = new Dictionary<int, float>();

        private readonly Dictionary<int, float> totalTimeBetweenSights = new Dictionary<int, float>();

        /// <summary>
        /// The number of time an entity sees the enemy
        /// </summary>
        private readonly Dictionary<int, int> numberOfDetections = new Dictionary<int, int>();

        /// <summary>
        /// Last time when we started searching for an enemy lost during fight
        /// </summary>
        private readonly Dictionary<int, float> lastStartSearchTime = new Dictionary<int, float>();

        // /// <summary>
        // /// Last time when we stopped searching for an enemy lost during fight
        // /// </summary>
        // private readonly Dictionary<int, float> lastStopSearchTime = new Dictionary<int, float>();

        /// <summary>
        /// Total time we spent from the start of a search until we stopped searching
        /// </summary>
        private readonly Dictionary<int, float> totalTimeToSurrender = new Dictionary<int, float>();
        
        // Logs info about the maps.
        public void LogMapInfo(MapInfo info) { }

        // Logs info about the maps.
        public void LogGameInfo(GameInfo info) { }

        // Logs reload.
        public void LogReload(ReloadInfo info) { }

        // Logs the shot.
        public void LogShot(ShotInfo info) { }

        // Logs the position (x and z respectively correspond to row and column in matrix notation).
        public void LogPosition(PositionInfo info) { }

        // Logs spawn.
        public void LogSpawn(SpawnInfo info)
        {
            lastFightEndTime[info.entityId] = Time.time;
        }

        // Logs a kill.
        public void LogKill(KillInfo info)
        {
            HandleEndOfFight(info.killedEntityID);
            HandleEnemyOufOfSight(info.killedEntityID);
        }

        // Logs a hit.
        public void LogHit(HitInfo info) { }


        // Logs statistics about the game.
        public void LogGameStatistics() { }

        public void CompileResults() { }

        public void Reset() { }

        public void LogEnemyInSight(int entityID)
        {
            HandleEnemyInSight(entityID);
        }

        public void LogEnemyOutOfSight(int entityID)
        {
            HandleEnemyOufOfSight(entityID);
        }

        public void LogEnterFight(EnterFightInfo obj)
        {
            HandleStartOfFight(obj.entityId);
        }

        public void LogExitFight(ExitFightInfo obj)
        {
            HandleEndOfFight(obj.entityId);
        }

        public void LogStartSearch(int entityID)
        {
            HandleStartSearch(entityID);
        }

        public void LogStopSearch(int entityID)
        {
            HandleStopSearch(entityID);
        }

        private void HandleStartSearch(int entityID)
        {
            var startSearchTime = Time.time;
            lastStartSearchTime[entityID] = startSearchTime;
            // lastStopSearchTime.Remove(entityID);
            // Nothing else?
        }

        private void HandleStopSearch(int entityID)
        {
            var stopSearchTime = Time.time;
            // lastStopSearchTime[entityID] = stopSearchTime;
            if (lastStartSearchTime.TryGetValue(entityID, out var startSearchTime))
            {
                var surrenderTime = stopSearchTime - startSearchTime;
                totalTimeToSurrender.TryGetValue(entityID, out var previousTimeToSurrender);
                totalTimeToSurrender[entityID] = previousTimeToSurrender + surrenderTime;
            }

            lastStartSearchTime.Remove(entityID);
        }

        private void HandleEnemyOufOfSight(int entityID)
        {
            numberOfDetections.AddToKey(entityID, 1);
            lastEnemyOutOfSightTime[entityID] = Time.time;
            // lastEnemyInSightTime.Remove(infoKilledEntityID);
            // Nothing else?
        }

        private void HandleEnemyInSight(int entityID)
        {
            var foundTime = Time.time;
            // lastEnemyInSightTime[entityID] = foundTime;
            if (lastEnemyOutOfSightTime.TryGetValue(entityID, out var lastLostTime))
            {
                var timeBetweenSights = foundTime - lastLostTime;
                totalTimeBetweenSights.AddToKey(entityID, timeBetweenSights);
            }

            lastEnemyOutOfSightTime.Remove(entityID);
        }

        private void HandleEndOfFight(int entityID)
        {
            var endFightTime = Time.time;
            lastFightEndTime[entityID] = endFightTime;
            if (lastFightStartTime.TryGetValue(entityID, out var startTime))
            {
                var timeInFight = endFightTime - startTime;
                totalTimeInFight.AddToKey(entityID, timeInFight);
            }

            lastFightStartTime.Remove(entityID);
        }

        private void HandleStartOfFight(int entityID)
        {
            var startFightTime = Time.time;
            lastFightStartTime[entityID] = startFightTime;
            if (lastFightEndTime.TryGetValue(entityID, out var endTime))
            {
                var timeToEngage = startFightTime - endTime;
                totalTimeToEngage.AddToKey(entityID, timeToEngage);
            }

            lastFightEndTime.Remove(entityID);
        }
    }
}