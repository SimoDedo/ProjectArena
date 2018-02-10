namespace JsonObjects {

    public class JsonLog {
        public string time;
        public string type;
        public string log;
    }

    public class JsonInfo {
        public string experimentName;
        public string[] playedMaps;
    }

    public class JsonMapInfo {
        public string height;
        public string width;
        public string tileSize;
        public string flip;
    }

    public class JsonPosition {
        public string x;
        public string y;
        public string direction;
    }

    public class JsonShoot {
        public string x;
        public string y;
        public string direction;
        public string weapon;
        public string ammoInCharger;
        public string totalAmmo;
    }

    public class JsonReload {
        public string weapon;
        public string ammoInCharger;
        public string totalAmmo;
    }

    public class JsonKill {
        public string x;
        public string y;
        public string killedEntity;
        public string killerEntity;
    }

    public class JsonHit {
        public string x;
        public string y;
        public string hittedEntity;
        public string hitterEntity;
        public string damage;
    }

    public class JsonSpawn {
        public string x;
        public string y;
        public string spawnedEntity;
    }

    public class JsonTargetStatistics {
        public string playerInitialX;
        public string playerInitialY;
        public string playerX;
        public string playerY;
        public string targetX;
        public string targetY;
        public string coveredTileDistance;
        public string time;
        public string speed;
    }

    public class JsonGameStatistics {
        public string totalShots;
        public string totalHits;
        public string accuracy;
        public string coveredDistance;
        public string mediumKillTime;
        public string mediumKillDistance;
    }

    public class JsonQuestion {
        public int questionId;
        public string questionText;
        public string options;
    }

    public class JsonOption {
        public int optionId;
        public string optionText;
    }

    public class JsonAnswer {
        public int questionId;
        public int[] answers;
    }

}