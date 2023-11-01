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
    public class PlatformCharacter : Character
    {

        private bool _isFalling;
        private float _jumpTimer;
        private float _jumpStartingY;
        private float _spawnExitTime;

        private const int SPAWN_DISTANCE = 9;
        private const int EXIT_DISTANCE = 16;

        private bool _isExiting;        

        public PlatformCharacter(SpriteSheet spriteSheet) : base(spriteSheet) 
        { 
            _spawnExitTime = 0;
        }

        public void Update(float deltaTime, bool[,] level)
        {
            if (Position.X >= MarioBros.SCREEN_WIDTH)
            {
                MoveTo(new Vector2(Position.X - MarioBros.SCREEN_WIDTH, Position.Y));
            }
            else if (Position.X < 0)
            {
                MoveTo(new Vector2(Position.X + MarioBros.SCREEN_WIDTH, Position.Y));
            }

            if (_spawnExitTime < SPAWN_DISTANCE / _speed)
            {
                _spawnExitTime += deltaTime;
                Animate(deltaTime);
            }
            else if (_isExiting)
            {
                _spawnExitTime += deltaTime;
                if (_spawnExitTime > EXIT_DISTANCE / _speed) 
                { 
                    _isExiting = false;
                    LookTo(new Vector2(-MoveDirection.X, MoveDirection.Y));
                    MoveTo(new Vector2(Position.X + 16 * MoveDirection.X, 44));
                    _spawnExitTime = 0;
                }
            }
            else
            {
                if (IsOnPlatform(level))
                {
                    if (_isFalling)
                    {
                        _isFalling = false;
                        MoveTo(new Vector2(Position.X, (PixelPositionY / 8) * 8));
                    }
                    Animate(deltaTime);
                }
                else
                {
                    if (!_isFalling)
                    {
                        _isFalling = true;
                        _jumpTimer = MarioBros.JUMP_DURATION;
                        _jumpStartingY = Position.Y;
                    }
                    else
                    {
                        _jumpTimer += deltaTime;
                        float t = _jumpTimer;
                        float y = -(1 / (MarioBros.JUMP_DURATION * MarioBros.JUMP_DURATION)) * t * t + (2 / MarioBros.JUMP_DURATION) * t;

                        y = (y - 1) * MarioBros.MARIO_JUMP_HEIGHT;
                        MoveTo(new Vector2(Position.X, _jumpStartingY - y));
                    }
                }
            }
            Move(deltaTime);
        }

        private bool IsOnPlatform(bool[,] level)
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = PixelPositionY / 8;
            return gridPositionY == 29 || level[gridPositionX, gridPositionY];
        }

        public void Exit()
        {
            _isExiting = true;
            _spawnExitTime = 0;
            MoveTo(new Vector2(Position.X, 199));
        }

        public override void Draw(SpriteBatch spriteBatch, int displayOffsetX = 0, int displayOffsetY = 0)
        {
            base.Draw(spriteBatch, displayOffsetX, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX + MarioBros.SCREEN_WIDTH, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX - MarioBros.SCREEN_WIDTH, displayOffsetY);
        }
    }
}
