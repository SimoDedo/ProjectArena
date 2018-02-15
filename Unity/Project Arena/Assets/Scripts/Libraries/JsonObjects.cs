using System;
using System.Collections.Generic;

namespace JsonObjects {

    namespace Game {

        [Serializable]
        public class JsonGameLog {
            public JsonMapInfo mapInfo;
            public JsonGameInfo gameInfo;
            public List<JsonSpawn> spawnLogs;
            public List<JsonPosition> positionLogs;
            public List<JsonShot> shotLogs;
            public List<JsonReload> reloadLogs;
            public List<JsonHit> hitLogs;
            public List<JsonKill> killLogs;

            public JsonGameLog() {
                spawnLogs = new List<JsonSpawn>();
                positionLogs = new List<JsonPosition>();
                shotLogs = new List<JsonShot>();
                reloadLogs = new List<JsonReload>();
                hitLogs = new List<JsonHit>();
                killLogs = new List<JsonKill>();
            }

            public void Reset() {
                spawnLogs.Clear();
                positionLogs.Clear();
                shotLogs.Clear();
                reloadLogs.Clear();
                hitLogs.Clear();
                killLogs.Clear();
            }
        }

        [Serializable]
        public class JsonPosition {
            public float timestamp;
            public float x;
            public float y;
            public float direction;

            public JsonPosition(float timestamp, float x, float y, float direction) {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
                this.direction = direction;
            }
        }

        [Serializable]
        public class JsonShot {
            public float timestamp;
            public float x;
            public float y;
            public float direction;
            public int weapon;
            public int ammoInCharger;
            public int totalAmmo;

            public JsonShot(float timestamp, float x, float y, float direction, int weapon,
                int ammoInCharger,
                int totalAmmo) {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
                this.direction = direction;
                this.weapon = weapon;
                this.ammoInCharger = ammoInCharger;
            }
        }

        [Serializable]
        public class JsonReload {
            public float timestamp;
            public int weapon;
            public int ammoInCharger;
            public int totalAmmo;

            public JsonReload(float timestamp, int weapon, int ammoInCharger, int totalAmmo) {
                this.timestamp = timestamp;
                this.weapon = weapon;
                this.ammoInCharger = ammoInCharger;
                this.totalAmmo = totalAmmo;
            }
        }

        [Serializable]
        public class JsonKill {
            public float timestamp;
            public float x;
            public float y;
            public string killedEntity;
            public string killerEntity;

            public JsonKill(float timestamp, float x, float y, string killedEntity,
                string killerEntity) {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
                this.killedEntity = killedEntity;
                this.killerEntity = killerEntity;
            }
        }

        [Serializable]
        public class JsonHit {
            public float timestamp;
            public float x;
            public float y;
            public string hittedEntity;
            public string hitterEntity;
            public int damage;

            public JsonHit(float timestamp, float x, float y, string hittedEntity,
                string hitterEntity, int damage) {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
                this.hittedEntity = hittedEntity;
                this.hitterEntity = hitterEntity;
                this.damage = damage;
            }
        }

        [Serializable]
        public class JsonSpawn {
            public float timestamp;
            public float x;
            public float y;
            public string spawnedEntity;

            public JsonSpawn(float timestamp, float x, float y, string spawnedEntity) {
                this.timestamp = timestamp;
                this.x = x;
                this.y = y;
                this.spawnedEntity = spawnedEntity;
            }
        }

    }

    namespace Statistics {

        [Serializable]
        public class JsonStatisticsLog {
            public JsonMapInfo mapInfo;
            public JsonGameInfo gameInfo;
            public List<JsonTargetStatistics> targetStatisticsLogs;
            public JsonFinalStatistics finalStatistics;

            public JsonStatisticsLog() {
                targetStatisticsLogs = new List<JsonTargetStatistics>();
            }

            public void Reset() {
                targetStatisticsLogs.Clear();
            }
        }

        [Serializable]
        public class JsonTargetStatistics {
            public float timestamp;
            public float playerInitialX;
            public float playerInitialY;
            public float playerX;
            public float playerY;
            public float targetX;
            public float targetY;
            public float coveredTileDistance;
            public float time;
            public float speed;

            public JsonTargetStatistics(float timestamp, float playerInitialX,
                float playerInitialY, float playerX, float playerY, float targetX,
                float targetY, float coveredTileDistance, float time, float speed) {
                this.timestamp = timestamp;
                this.playerInitialX = playerInitialX;
                this.playerInitialY = playerInitialY;
                this.playerX = playerX;
                this.playerY = playerY;
                this.targetX = targetX;
                this.targetY = targetY;
                this.coveredTileDistance = coveredTileDistance;
                this.time = time;
                this.speed = speed;
            }
        }

        [Serializable]
        public class JsonFinalStatistics {
            public int totalShots;
            public int totalHits;
            public float accuracy;
            public float coveredDistance;
            public float mediumKilltimestamp;
            public float mediumKillDistance;

            public JsonFinalStatistics(int totalShots, int totalHits, float accuracy,
                float coveredDistance, float mediumKilltimestamp, float mediumKillDistance) {
                this.totalShots = totalShots;
                this.totalHits = totalHits;
                this.accuracy = accuracy;
                this.coveredDistance = coveredDistance;
                this.mediumKilltimestamp = mediumKilltimestamp;
                this.mediumKillDistance = mediumKillDistance;
            }
        }

    }

    namespace Survey {

        [Serializable]
        public class JsonSurvey {
            public List<JsonQuestion> questions;

            public JsonSurvey(List<JsonQuestion> questions) {
                this.questions = questions;
            }
        }

        [Serializable]
        public class JsonQuestion {
            public int questionId;
            public string questionText;
            public List<JsonOption> options;

            public JsonQuestion(int questionId, string questionText, List<JsonOption> options) {
                this.questionId = questionId;
                this.questionText = questionText;
                this.options = options;
            }
        }

        [Serializable]
        public class JsonOption {
            public int optionId;
            public string optionText;

            public JsonOption(int optionId, string optionText) {
                this.optionId = optionId;
                this.optionText = optionText;
            }
        }

        [Serializable]
        public class JsonAnswers {
            public string experimentName;
            public string[] playedMaps;
            public List<JsonAnswer> answers;

            public JsonAnswers(string experimentName, string[] playedMaps,
                List<JsonAnswer> answers) {
                this.experimentName = experimentName;
                this.playedMaps = playedMaps;
                this.answers = answers;
            }
        }

        [Serializable]
        public class JsonAnswer {
            public int questionId;
            public int[] answers;

            public JsonAnswer(int questionId, int[] answers) {
                this.questionId = questionId;
                this.answers = answers;
            }
        }

    }

    [Serializable]
    public class JsonMapInfo {
        public float height;
        public float width;
        public float tileSize;
        public bool flip;

        public JsonMapInfo(float height, float width, float tileSize, bool flip) {
            this.height = height;
            this.width = width;
            this.tileSize = tileSize;
            this.flip = flip;
        }
    }

    [Serializable]
    public class JsonGameInfo {
        public int duration;
        public string scene;

        public JsonGameInfo(int duration, string scene) {
            this.duration = duration;
            this.scene = scene;
        }
    }

    [Serializable]
    public class JsonCompletionTracker {
        public List<ExperimentObjects.StudyCompletionTracker> studyCompletionTrackers;
        public int logsCount;

        public JsonCompletionTracker(int logCount, List<ExperimentObjects.StudyCompletionTracker>
            studyCompletionTrackers) {
            this.logsCount = logCount;
            this.studyCompletionTrackers = studyCompletionTrackers;
        }
    }

}