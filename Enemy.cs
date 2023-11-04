using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioBros2023
{
    public class Enemy : PlatformCharacter
    {
        private float _spawnExitTime;

        private const int SPAWN_DISTANCE = 9;
        private const int EXIT_DISTANCE = 16;

        private bool _isExiting;
        public bool IsExiting => _isExiting;
        private bool _isEntering;
        public bool IsEntering => _isEntering;

        public Enemy(SpriteSheet spriteSheet, float jumpDuration, float jumpHeight) : base(spriteSheet, jumpDuration, jumpHeight)
        {
            _spawnExitTime = 0;
            _isEntering = true;
        }

        public override void Update(float deltaTime, bool[,] level)
        {
            base.Update(deltaTime, level);
            if (PixelPositionY >= 208)
            {
                if (MoveDirection.X < 0 && PixelPositionX - SpriteSheet.LeftMargin <= 32
                    || MoveDirection.X > 0 && PixelPositionX + SpriteSheet.RightMargin > MarioBros.SCREEN_WIDTH - 32)
                {
                    Exit();
                }
            }

        }

        protected override void UpdateJump(float deltaTime, bool[,] level)
        {
            if (_isEntering)
            {
                _spawnExitTime += deltaTime;
                Animate(deltaTime);
                if (_spawnExitTime < SPAWN_DISTANCE / CurrentSpeed)
                {
                    _isEntering = false;
                }
            }
            else if (_isExiting)
            {
                _spawnExitTime += deltaTime;
                if (_spawnExitTime > EXIT_DISTANCE / CurrentSpeed)
                {
                    _isExiting = false;
                    LookTo(new Vector2(-MoveDirection.X, MoveDirection.Y));
                    MoveTo(new Vector2(Position.X + 16 * MoveDirection.X, 44));
                    _spawnExitTime = 0;
                    _isEntering = true;
                }
            }
            else
            {
                base.UpdateJump(deltaTime, level);
            }
        }

        public void Exit()
        {
            _isExiting = true;
            _spawnExitTime = 0;
            MoveTo(new Vector2(Position.X, 199));
        }
    }
}
