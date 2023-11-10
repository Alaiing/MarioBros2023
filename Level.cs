using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Level(int tileFrame)
        {
            _tileFrame = tileFrame;
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
                    newLevel = new Level(0);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 0f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    break;
                case 1:
                    newLevel = new Level(1);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 0f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 3f);
                    newLevel.AddSpawn(Enemy.EnemyType.Turtle, 6f);
                    break;
            }
            return newLevel;
        }
    }
}
