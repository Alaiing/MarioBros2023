using Oudidon;
using System.Collections.Generic;

namespace MarioBros2023
{
    public class Level
    {
        public struct EnemySpawn
        {
            public Enemy.EnemyType enemyType;
            public float timeToSpawn;
        }

        private readonly List<EnemySpawn> _enemySpawns = new List<EnemySpawn>();
        public int EnemyCount => _enemySpawns.Count;
        private float _spawnTimer;
        private int _spawnIndex;

        private int _tileFrame;
        public int TileFrame => _tileFrame;

        private bool _isBonusLevel;
        public bool IsBonusLevel => _isBonusLevel;
        private float _levelTimer;
        public float LevelTimer => _levelTimer;

        public Level(int tileFrame, bool isBonusLevel)
        {
            _tileFrame = tileFrame;
            _isBonusLevel = isBonusLevel;
            _levelTimer = 0;
        }

        public void AddSpawn(Enemy.EnemyType enemyType, float timeToSpawn)
        {
            _enemySpawns.Add(new EnemySpawn { enemyType = enemyType, timeToSpawn = timeToSpawn });
        }

        public void ResetSpawn()
        {
            _spawnTimer = 0;
            _spawnIndex = 0;
        }

        public void Update(float deltaTime)
        {
            _levelTimer += deltaTime;
            if (_spawnIndex < _enemySpawns.Count)
            {
                _spawnTimer += deltaTime;
                if (_spawnTimer >= _enemySpawns[_spawnIndex].timeToSpawn)
                {
                    _spawnTimer -= _enemySpawns[_spawnIndex].timeToSpawn;
                    EventsManager.FireEvent("SPAWN_ENEMY", _enemySpawns[_spawnIndex].enemyType);
                    _spawnIndex++;
                }
            }
        }

        public static Level CreateLevel(int level)
        {
            Level newLevel = null;
            switch (level)
            {
                case 0:
                    newLevel = new Level(0, isBonusLevel: false);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 0f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    break;
                case 1:
                    newLevel = new Level(0, isBonusLevel: false);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 0f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    break;

                case 2:
                    newLevel = new Level(1, isBonusLevel: true);
                    break;

                case 3:
                    newLevel = new Level(2, isBonusLevel: false);
                    newLevel.AddSpawn(Enemy.EnemyType.Crab, 0f);
                    newLevel.AddSpawn(Enemy.EnemyType.Crab, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Crab, 6f);
                    newLevel.AddSpawn(Enemy.EnemyType.Crab, 3f);
                    break;
            }
            return newLevel;
        }
    }
}
