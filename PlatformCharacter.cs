using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioBros2023
{
    public class PlatformCharacter : Character
    {
        private bool _isFalling;
        public bool IsFalling => _isFalling;
        private float _jumpTimer;
        private float _jumpStartingY;

        private float _jumpDuration;
        private float _jumpHeight;

        private bool _isJumping;
        private bool _isFlipped;
        private float _flipTimer;
        private bool _isDying;
        private bool _isDead;
        public bool IsDead => _isDead;
        public bool IsDying => _isDying;
        public bool IsFlipped => _isFlipped;
        private float _initialDirection;

        public PlatformCharacter(SpriteSheet spriteSheet, float jumpDuration, float jumpHeight) : base(spriteSheet)
        {
            _jumpDuration = jumpDuration;
            _jumpHeight = jumpHeight;
        }

        public virtual void Update(float deltaTime, bool[,] level)
        {
            UpdateSideScreen();

            UpdateJump(deltaTime, level);

            if (_isFlipped)
            {
                _flipTimer += deltaTime;
                if (_flipTimer > 5)
                {
                    SetAnimation("Walk");
                    SetSpeed(1f);
                    if (MoveDirection.X == 0)
                    {
                        LookTo(new Vector2(-_initialDirection, 0));
                    }
                    _isFlipped = false;
                }
            }
            Move(deltaTime);
        }

        private void UpdateSideScreen()
        {
            if (Position.X >= MarioBros.SCREEN_WIDTH)
            {
                MoveTo(new Vector2(Position.X - MarioBros.SCREEN_WIDTH, Position.Y));
            }
            else if (Position.X < 0)
            {
                MoveTo(new Vector2(Position.X + MarioBros.SCREEN_WIDTH, Position.Y));
            }
        }

        protected virtual void UpdateJump(float deltaTime, bool[,] level)
        {
            if (_isJumping)
            {
                _jumpTimer += deltaTime;
                float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * _jumpDuration)) * _jumpHeight;
                MoveTo(new Vector2(Position.X, _jumpStartingY - y));

                if (_jumpTimer >= _jumpDuration)
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
                        Land();
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
                        float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * _jumpDuration));

                        y = (y - 1) * _jumpHeight;
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

        private void Land()
        {
            _isFalling = false;
            MoveTo(new Vector2(Position.X, (PixelPositionY / 8) * 8));
            SetAnimationSpeed(1f);
            if (_isFlipped)
            {
                SetSpeed(0);
            }
            else
            {
                SetAnimation("Walk");
            }
        }

        private bool IsOnPlatform(bool[,] level)
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = PixelPositionY / 8;
            return gridPositionY == 29 || level[gridPositionX, gridPositionY];
        }

        private bool IsUnderPlatform(bool[,] level)
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = (PixelPositionY - 20) / 8;
            return gridPositionY > 0 && _isJumping && level[gridPositionX, gridPositionY];
        }
        public void Fall()
        {
            _isJumping = false;
            _isFalling = true;
            _jumpTimer = _jumpDuration;
            _jumpStartingY = Position.Y;
            SetAnimationSpeed(0f);
        }

        public void Jump()
        {
            _isJumping = true;
            _jumpStartingY = Position.Y;
            _jumpTimer = 0;
            SetAnimation("Jump");
        }

        public void Bump(int direction)
        {
            _isFlipped = !_isFlipped;
            if (_isFlipped)
            {
                _flipTimer = 0;
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
            // TODO: only draw alternate sprites if needed
            base.Draw(spriteBatch, displayOffsetX + MarioBros.SCREEN_WIDTH, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX - MarioBros.SCREEN_WIDTH, displayOffsetY);
        }
    }
}
