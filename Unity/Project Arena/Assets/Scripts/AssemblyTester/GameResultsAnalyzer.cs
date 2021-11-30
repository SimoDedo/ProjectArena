using System.Collections.Generic;
using System.Numerics;
using AssemblyLogging;

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

        
        
        // Logs info about the maps.
        public void LogMapInfo(MapInfo info)
        {
        
        }

        // Logs info about the maps.
        public void LogGameInfo(GameInfo info)
        {
        }

        // Logs reload.
        public void LogReload(ReloadInfo info)
        {
        }

        // Logs the shot.
        public void LogShot(ShotInfo info)
        {
        }

        // Logs the position (x and z respectively correspond to row and column in matrix notation).
        public void LogPosition(PositionInfo info)
        {
        }

        // Logs spawn.
        public void LogSpawn(SpawnInfo info)
        {
        }

        // Logs a kill.
        public void LogKill(KillInfo info)
        {
        }

        // Logs a hit.
        public void LogHit(HitInfo info)
        {
        }


        // Logs statistics about the game.
        public void LogGameStatistics()
        {
        }

        public void CompileResults()
        {
            
        }

        public void Reset()
        {
            
        }
    }
}