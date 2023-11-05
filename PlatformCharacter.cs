using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarioBros2023
{
    public class PlatformCharacter : Character
    {
        protected const string STATE_IDLE = "Idle";
        protected const string STATE_WALK = "Walk";
        protected const string STATE_JUMP = "Jump";
        protected const string STATE_FALL = "Fall";
        protected const string STATE_FLIPPED = "Flipped";
        protected const string STATE_DYING = "Dying";
        protected const string STATE_DEAD = "Dead";

        public bool IsFalling => _stateMachine.CurrentState == STATE_FALL;
        private float _jumpTimer;
        private float _jumpStartingY;

        private float _jumpDuration;
        private float _jumpHeight;

        public bool IsJumping => _stateMachine.CurrentState == STATE_JUMP;
        private bool _isFlipped;
        private float _flipTimer;
        public bool IsDead => _stateMachine.CurrentState == STATE_DEAD;
        public bool IsDying { get; private set; }
        public bool IsFlipped => _isFlipped;
        protected float _flippedDuration;

        protected SimpleStateMachine _stateMachine;
        private readonly bool[,] _level;

        public PlatformCharacter(SpriteSheet spriteSheet, float jumpDuration, float jumpHeight, bool[,] level) : base(spriteSheet)
        {
            _jumpDuration = jumpDuration;
            _jumpHeight = jumpHeight;
            _flippedDuration = ConfigManager.GetConfig("FLIPPED_DURATION", 10);
            _level = level;

            InitStateMachine();
        }

        protected virtual void InitStateMachine()
        {
            _stateMachine = new SimpleStateMachine();

            _stateMachine.AddState(STATE_IDLE, IdleEnter, IdleExit, IdleUpdate);
            _stateMachine.AddState(STATE_WALK, WalkEnter, WalkExit, WalkUpdate);
            _stateMachine.AddState(STATE_JUMP, JumpEnter, JumpExit, JumpUpdate);
            _stateMachine.AddState(STATE_FALL, FallEnter, FallExit, FallUpdate);
            _stateMachine.AddState(STATE_FLIPPED, FlippedEnter, FlippedExit, FlippedUpdate);
            _stateMachine.AddState(STATE_DYING, DyingEnter, DyingExit, DyingUpdate);
            _stateMachine.AddState(STATE_DEAD, DeadEnter, DeadExit, DeadUpdate);

            _stateMachine.SetState(STATE_WALK);
        }

        public virtual void Update(float deltaTime, bool[,] level)
        {
            UpdateSideScreen();

            _stateMachine.Update(deltaTime);
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

        protected virtual void UpdateJump(float deltaTime, out bool climaxReached, out bool hitPlatform)
        {
            climaxReached = hitPlatform = false;
            _jumpTimer += deltaTime;
            float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * _jumpDuration)) * _jumpHeight;
            MoveTo(new Vector2(Position.X, _jumpStartingY - y));
            Move(deltaTime);
            Animate(deltaTime);

            if (IsUnderPlatform())
            {
                hitPlatform = true;
            }
            else if (_jumpTimer >= _jumpDuration)
            {
                climaxReached = true;
            }
        }

        protected virtual void UpdateFall(float deltaTime, out bool landed, out bool hitBottom)
        {
            landed = hitBottom = false;

            if (!_ignorePlatforms && IsOnPlatform())
            {
                MoveTo(new Vector2(Position.X, (PixelPositionY / 8) * 8));
                landed = true;
            }
            else
            {
                _fallTimer += deltaTime;
                float y = MathUtils.NormalizedParabolicPosition((_fallTimer + _jumpDuration) / (2 * _jumpDuration));

                y = (y - 1) * _jumpHeight;
                MoveTo(new Vector2(Position.X, _fallStartY - y));
                Move(deltaTime);

                if (Position.Y >= MarioBros.SCREEN_HEIGHT)
                {
                    hitBottom = true;
                }
            }
        }

        private bool IsOnPlatform()
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = PixelPositionY / 8;
            return gridPositionY == 29 || _level[gridPositionX, gridPositionY];
        }

        private bool IsUnderPlatform()
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = (PixelPositionY - 20) / 8;
            return gridPositionY > 0 && _level[gridPositionX, gridPositionY];
        }

        public void Bump(int direction)
        {
            _isFlipped = !_isFlipped;
            if (_isFlipped)
            {
                SetAnimation("OnBack");
                if (direction == 0)
                {
                    SetSpeed(0f);
                    LookTo(-MoveDirection); // if bumped straight Up, reverse walking direction
                }
                else
                {
                    LookTo(new Vector2(direction, 0));
                }
            }
            else
            {
                SetAnimation("Walk");
                if (direction == 0)
                {
                    SetSpeed(0f);
                }
                else
                {
                    SetSpeed(1f);
                    LookTo(new Vector2(direction, 0));
                }
            }
            _stateMachine.SetState(STATE_JUMP);
        }

        public void Kill(int direction)
        {
            _ignorePlatforms = true;
            _stateMachine.SetState(STATE_FALL);
            IsDying = true;
            SetSpeed(1f);
            LookTo(new Vector2(direction, 0));
        }

        public override void Draw(SpriteBatch spriteBatch, int displayOffsetX = 0, int displayOffsetY = 0)
        {
            base.Draw(spriteBatch, displayOffsetX, displayOffsetY);
            // TODO: only draw alternate sprites if needed
            base.Draw(spriteBatch, displayOffsetX + MarioBros.SCREEN_WIDTH, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX - MarioBros.SCREEN_WIDTH, displayOffsetY);
        }

        #region States
        protected bool _ignorePlatforms;
        protected virtual void IdleEnter()
        {
            SetAnimation("Idle");
        }

        protected virtual void IdleExit() { }
        protected virtual void IdleUpdate(float deltaTime)
        {
            Animate(deltaTime);
        }


        protected virtual void WalkEnter()
        {
            SetAnimation("Walk");
        }
        protected virtual void WalkExit() { }

        protected virtual void WalkUpdate(float deltaTime)
        {
            if (!_ignorePlatforms && !IsOnPlatform())
            {
                _stateMachine.SetState(STATE_FALL);
                return;
            }

            Move(deltaTime);
            Animate(deltaTime);
        }

        protected virtual void JumpEnter()
        {
            SetAnimation("Jump");
            _jumpTimer = 0;
            _jumpStartingY = Position.Y;
        }

        protected virtual void JumpExit()
        {
            if (!_isFlipped)
            {
                SetSpeed(1f);
            }
        }
        protected virtual void JumpUpdate(float deltaTime)
        {
            UpdateJump(deltaTime, out bool climaxReached, out bool hitPlatform);

            if (climaxReached)
            {
                _stateMachine.SetState(STATE_FALL);
            }
        }

        private float _fallTimer;
        private float _fallStartY;
        protected virtual void FallEnter()
        {
            _fallTimer = 0;
            _fallStartY = Position.Y;
        }

        protected virtual void FallExit()
        {
        }

        protected virtual void FallUpdate(float deltaTime)
        {
            UpdateFall(deltaTime, out bool landed, out bool hitBottom);

            if (landed)
            {
                if (_isFlipped)
                {
                    _stateMachine.SetState(STATE_FLIPPED);
                }
                else
                {
                    _stateMachine.SetState(STATE_WALK);
                }
            }
            else if (hitBottom)
            {
                _stateMachine.SetState(STATE_DEAD);
            }
        }

        protected virtual void FlippedEnter()
        {
            SetSpeed(0f);
            _flipTimer = 0;
        }
        protected virtual void FlippedExit()
        {
            _isFlipped = false;
        }

        protected virtual void FlippedUpdate(float deltaTime)
        {
            _flipTimer += deltaTime;
            if (_flipTimer > _flippedDuration)
            {
                SetSpeed(1f);
                SetAnimationSpeed(1f);
                _stateMachine.SetState(STATE_WALK);
            }
            else
            {
                if (_flipTimer > _flippedDuration / 2)
                {
                    SetAnimationSpeed(3f);
                }
                Animate(deltaTime);
            }
        }

        protected virtual void DyingEnter() { }
        protected virtual void DyingExit() { }
        protected virtual void DyingUpdate(float deltaTime) { }

        protected virtual void DeadEnter() { }
        protected virtual void DeadExit() { }
        protected virtual void DeadUpdate(float deltaTime) { }

        #endregion
    }
}
