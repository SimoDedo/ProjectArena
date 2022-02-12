using System;
using System.Collections.Generic;

namespace Logging
{
    namespace Logging
    {
        namespace Game
        {
            [Serializable]
            public class JsonGameLog
            {
                public int logPart;
                public string testID;
                public JsonMapInfo mapInfo;
                public JsonGameInfo gameInfo;
                public List<JsonSpawn> spawnLogs;
                public List<JsonPosition> positionLogs;
                public List<JsonShot> shotLogs;
                public List<JsonReload> reloadLogs;
                public List<JsonHit> hitLogs;
                public List<JsonKill> killLogs;

                public JsonGameLog(string testID)
                {
                    logPart = 0;
                    this.testID = testID;

                    spawnLogs = new List<JsonSpawn>();
                    positionLogs = new List<JsonPosition>();
                    shotLogs = new List<JsonShot>();
                    reloadLogs = new List<JsonReload>();
                    hitLogs = new List<JsonHit>();
                    killLogs = new List<JsonKill>();
                }

                public void Reset()
                {
                    spawnLogs.Clear();
                    positionLogs.Clear();
                    shotLogs.Clear();
                    reloadLogs.Clear();
                    hitLogs.Clear();
                    killLogs.Clear();
                }

                public void Split()
                {
                    logPart++;
                    Reset();
                }
            }

            [Serializable]
            public class JsonPosition
            {
                public double timestamp;
                public int entityID;
                public double x;
                public double y;
                public double direction;

                public JsonPosition(float timestamp, int entityID, float x, float y, float direction)
                {
                    this.entityID = entityID;
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
                    this.y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
                    this.direction = Math.Round(direction, 2, MidpointRounding.AwayFromZero);
                }
            }

            [Serializable]
            public class JsonShot
            {
                public double timestamp;
                public double x;
                public double y;
                public double direction;
                public int ownerID;
                public int weaponID;
                public int ammoInCharger;
                public int totalAmmo;

                public JsonShot(float timestamp, float x, float y, float direction, int ownerID, int weaponID,
                    int ammoInCharger,
                    int totalAmmo)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
                    this.y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
                    this.direction = Math.Round(direction, 2, MidpointRounding.AwayFromZero);
                    this.ownerID = ownerID;
                    this.weaponID = weaponID;
                    this.ammoInCharger = ammoInCharger;
                    this.totalAmmo = totalAmmo;
                }
            }

            [Serializable]
            public class JsonReload
            {
                public double timestamp;
                public int ownerId;
                public int weapon;
                public int ammoInCharger;
                public int totalAmmo;

                public JsonReload(float timestamp, int ownerId, int weapon, int ammoInCharger, int totalAmmo)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.ownerId = ownerId;
                    this.weapon = weapon;
                    this.ammoInCharger = ammoInCharger;
                    this.totalAmmo = totalAmmo;
                }
            }

            [Serializable]
            public class JsonKill
            {
                public double timestamp;
                public double x;
                public double y;
                public int killedEntityID;
                public string killedEntity;
                public int killerEntityID;
                public string killerEntity;

                public JsonKill(float timestamp, float x, float y, int killedEntityID, string killedEntity,
                    int killerEntityID, string killerEntity)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
                    this.y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
                    this.killedEntity = killedEntity;
                    this.killedEntityID = killedEntityID;
                    this.killerEntity = killerEntity;
                    this.killerEntityID = killerEntityID;
                }
            }

            [Serializable]
            public class JsonHit
            {
                public double timestamp;
                public double x;
                public double y;
                public int hitEntityID;
                public string hitEntity;
                public int hitterEntityID;
                public string hitterEntity;
                public int damage;

                public JsonHit(float timestamp, float x, float y, 
                    int hitEntityID, string hitEntity,
                    int hitterEntityID, string hitterEntity, int damage)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
                    this.y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
                    this.hitEntityID = hitEntityID;
                    this.hitEntity = hitEntity;
                    this.hitterEntityID = hitterEntityID;
                    this.hitterEntity = hitterEntity;
                    this.damage = damage;
                }
            }

            [Serializable]
            public class JsonSpawn
            {
                public double timestamp;
                public double x;
                public double y;
                public int entityID;
                public string spawnedEntity;

                public JsonSpawn(float timestamp, float x, float y, int entityID, string spawnedEntity)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.x = Math.Round(x, 2, MidpointRounding.AwayFromZero);
                    this.y = Math.Round(y, 2, MidpointRounding.AwayFromZero);
                    this.entityID = entityID;
                    this.spawnedEntity = spawnedEntity;
                }
            }
        }

        namespace Statistics
        {
            [Serializable]
            public class JsonStatisticsLog
            {
                public int logPart;
                public string testID;
                public JsonMapInfo mapInfo;
                public JsonGameInfo gameInfo;
                public List<JsonTargetStatistics> targetStatisticsLogs;
                public JsonFinalStatistics finalStatistics;

                public JsonStatisticsLog(string testID)
                {
                    this.testID = testID;

                    targetStatisticsLogs = new List<JsonTargetStatistics>();
                }

                public void Reset()
                {
                    targetStatisticsLogs.Clear();
                }

                public void Split()
                {
                    logPart++;
                    Reset();
                }
            }
            
            [Serializable]
            public class JsonAIStatisticsLog
            {
                public string testID;
                public JsonMapInfo mapInfo;
                public JsonGameInfo gameInfo;
                
                // TODO atm Unity JSONUtility doesn't handle dictionaries...
                public Dictionary<int, JsonFinalStatistics> finalStatistics;

                public JsonAIStatisticsLog(string testID)
                {
                    this.testID = testID;
                }
            }

            [Serializable]
            public class JsonTargetStatistics
            {
                public double timestamp;
                public double playerInitialX;
                public double playerInitialY;
                public double playerX;
                public double playerY;
                public double targetX;
                public double targetY;
                public float coveredTileDistance;
                public float time;
                public float speed;

                public JsonTargetStatistics(float timestamp, float playerInitialX,
                    float playerInitialY, float playerX, float playerY, float targetX,
                    float targetY, float coveredTileDistance, float time, float speed)
                {
                    this.timestamp = Math.Round(timestamp, 2, MidpointRounding.AwayFromZero);
                    this.playerInitialX = Math.Round(playerInitialX, 2,
                        MidpointRounding.AwayFromZero);
                    this.playerInitialY = Math.Round(playerInitialY, 2,
                        MidpointRounding.AwayFromZero);
                    this.playerX = Math.Round(playerX, 2, MidpointRounding.AwayFromZero);
                    this.playerY = Math.Round(playerY, 2, MidpointRounding.AwayFromZero);
                    this.targetX = Math.Round(targetX, 2, MidpointRounding.AwayFromZero);
                    this.targetY = Math.Round(targetY, 2, MidpointRounding.AwayFromZero);
                    this.coveredTileDistance = coveredTileDistance;
                    this.time = time;
                    this.speed = speed;
                }
            }

            [Serializable]
            public class JsonFinalStatistics
            {
                public int totalShots;
                public int totalHits;
                public float accuracy;
                public float coveredDistance;
                public float mediumKillTime;
                public float mediumKillDistance;

                public JsonFinalStatistics(int totalShots, int totalHits, float accuracy,
                    float coveredDistance, float mediumKillTime, float mediumKillDistance)
                {
                    this.totalShots = totalShots;
                    this.totalHits = totalHits;
                    this.accuracy = accuracy;
                    this.coveredDistance = coveredDistance;
                    this.mediumKillTime = mediumKillTime;
                    this.mediumKillDistance = mediumKillDistance;
                }
            }
        }

        namespace Survey
        {
            [Serializable]
            public class JsonSurvey
            {
                public List<JsonQuestion> questions;

                public JsonSurvey(List<JsonQuestion> questions)
                {
                    this.questions = questions;
                }
            }

            [Serializable]
            public class JsonQuestion
            {
                public int questionId;
                public string questionText;
                public List<JsonOption> options;

                public JsonQuestion(int questionId, string questionText, List<JsonOption> options)
                {
                    this.questionId = questionId;
                    this.questionText = questionText;
                    this.options = options;
                }
            }

            [Serializable]
            public class JsonOption
            {
                public int optionId;
                public string optionText;

                public JsonOption(int optionId, string optionText)
                {
                    this.optionId = optionId;
                    this.optionText = optionText;
                }
            }

            [Serializable]
            public class JsonAnswers
            {
                public string testID;
                public string[] playedMaps;
                public List<JsonAnswer> answers;

                public JsonAnswers(string testID, string[] playedMaps,
                    List<JsonAnswer> answers)
                {
                    this.testID = testID;
                    this.playedMaps = playedMaps;
                    this.answers = answers;
                }
            }

            [Serializable]
            public class JsonAnswer
            {
                public int questionId;
                public int[] answers;

                public JsonAnswer(int questionId, int[] answers)
                {
                    this.questionId = questionId;
                    this.answers = answers;
                }
            }
        }

        [Serializable]
        public class JsonMapInfo
        {
            public string name;
            public float height;
            public float width;
            public float tileSize;
            public bool flip;

            public JsonMapInfo(string name, float height, float width, float tileSize, bool flip)
            {
                this.name = name;
                this.height = height;
                this.width = width;
                this.tileSize = tileSize;
                this.flip = flip;
            }
        }

        [Serializable]
        public class JsonGameInfo
        {
            public string experiment;
            public string scene;
            public int duration;

            public JsonGameInfo(int duration, string scene, string experiment)
            {
                this.duration = duration;
                this.scene = scene;
                this.experiment = experiment;
            }
        }

        [Serializable]
        public class JsonCompletionTracker
        {
            public List<StudyCompletionTracker> studyCompletionTrackers;
            public int logsCount;

            public JsonCompletionTracker(int logCount, List<StudyCompletionTracker>
                studyCompletionTrackers)
            {
                this.logsCount = logCount;
                this.studyCompletionTrackers = studyCompletionTrackers;
            }
        }
    }

    namespace Logging
    {
        [Serializable]
        public class JsonABMLMap
        {
            public List<JsonABMap> maps;

            public JsonABMLMap(List<JsonABMap> maps)
            {
                this.maps = maps;
            }
        }

        [Serializable]
        public class JsonABMap
        {
            public List<JsonRoom> rooms;
            public List<JsonRoom> corridors;
            public List<JsonObject> objcects;
        }

        [Serializable]
        public class JsonABDigger
        {
            public int forwardProbability;
            public int leftProbability;
            public int rigthProbability;
            public int visitedProbability;
            public int stairProbability;

            public JsonABDigger(int forwardProbability, int leftProbability, int rigthProbability,
                int visitedProbability, int stairProbability = 0)
            {
                this.forwardProbability = forwardProbability;
                this.leftProbability = leftProbability;
                this.rigthProbability = rigthProbability;
                this.visitedProbability = visitedProbability;
                this.stairProbability = stairProbability;
            }
        }

        [Serializable]
        public class JsonRoom
        {
            public int x;
            public int y;
            public int dimension;
        }

        [Serializable]
        public class JsonObject
        {
            public int x;
            public int y;
            public char value;
        }
    }
}