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
    public class Enemy : Character
    {

        private bool _isFalling;
        private float _jumpTimer;
        private float _jumpStartingY;
        private float _spawnExitTime;

        private const int SPAWN_DISTANCE = 9;
        private const int EXIT_DISTANCE = 16;
        private const float JUMP_DURATION = 0.25f;
        private const float JUMP_HEIGHT = 15f;

        private bool _isExiting;
        private bool _isEntering;
        private bool _isJumping;
        private bool _isFlipped;
        private bool _isDying;
        private bool _isDead;
        public bool IsDead => _isDead;
        public bool IsDying => _isDying;
        public bool IsFlipped => _isFlipped;
        private float _initialDirection;

        public Enemy(SpriteSheet spriteSheet) : base(spriteSheet)
        {
            _spawnExitTime = 0;
            _isEntering = true;
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
                if (_isJumping)
                {
                    _jumpTimer += deltaTime;
                    float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * JUMP_DURATION)) * JUMP_HEIGHT;
                    MoveTo(new Vector2(Position.X, _jumpStartingY - y));

                    if (_jumpTimer >= JUMP_DURATION)
                    {
                        Fall();
                        SetSpeed(1f);
                    }
                }
                else
                {
                    if (!_isDying && IsOnPlatform(level))
                    {
                        if (_isFalling)
                        {
                            _isFalling = false;
                            MoveTo(new Vector2(Position.X, (PixelPositionY / 8) * 8));
                            if (_isFlipped)
                            {
                                SetSpeed(0);
                            }
                        }
                        Animate(deltaTime);
                    }
                    else
                    {
                        if (!_isFalling)
                        {
                            Fall();
                        }
                        else
                        {
                            _jumpTimer += deltaTime;
                            float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * JUMP_DURATION));

                            y = (y - 1) * JUMP_HEIGHT;
                            MoveTo(new Vector2(Position.X, _jumpStartingY - y));

                            if (_isDying && Position.Y >= MarioBros.SCREEN_HEIGHT)
                            {
                                _isFalling = false;
                                SetSpeed(0f);
                                _isDead = true;
                            }
                        }
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

        public void Fall()
        {
            _isJumping = false;
            _isFalling = true;
            _jumpTimer = JUMP_DURATION;
            _jumpStartingY = Position.Y;
        }

        public void Jump()
        {
            _isJumping = true;
            _jumpStartingY = Position.Y;
            _jumpTimer = 0;
        }

        public void Bump(int direction)
        {
            _isFlipped = !_isFlipped;
            if (_isFlipped)
            {
                _initialDirection = MoveDirection.X;
                SetAnimation("OnBack");
                LookTo(new Vector2(direction, 0));
            }
            else
            {
                SetAnimation("Walk");
                if (direction != 0)
                {
                    SetSpeed(1f);
                }

                if (direction == 0 && MoveDirection.X == 0)
                {
                    LookTo(new Vector2(_initialDirection, 0));
                }
                else
                {
                    LookTo(new Vector2(direction, 0));
                }
            }
            Jump();
        }

        public void Exit()
        {
            _isExiting = true;
            _spawnExitTime = 0;
            MoveTo(new Vector2(Position.X, 199));
        }

        public void Kill(int direction)
        {
            _isDying = true;
            SetSpeed(1f);
            LookTo(new Vector2(direction, 0));
            Fall();
        }

        public override void Draw(SpriteBatch spriteBatch, int displayOffsetX = 0, int displayOffsetY = 0)
        {
            base.Draw(spriteBatch, displayOffsetX, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX + MarioBros.SCREEN_WIDTH, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX - MarioBros.SCREEN_WIDTH, displayOffsetY);
        }
    }
}
