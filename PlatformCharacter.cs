using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;

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

        public virtual string WalkAnimationName => "Run";

        public bool IsFalling => _stateMachine.CurrentState == STATE_FALL;
        private float _jumpTimer;
        private float _jumpStartingY;

        private float _jumpDuration;
        private float _jumpHeight;

        public bool IsJumping => _stateMachine.CurrentState == STATE_JUMP;
        protected bool _isFlipped;
        private float _flipTimer;
        public bool IsDead => _stateMachine.CurrentState == STATE_DEAD;
        public bool IsDying { get; protected set; }
        public bool IsFlipped => _isFlipped;
        protected float _flippedDuration;

        protected PlatformCharacter _killer;

        protected SimpleStateMachine _stateMachine;
        private readonly MarioBros.LevelTile[,] _level;

        public bool CanBump;

        private SoundEffectInstance _bumpSound;

        public PlatformCharacter(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect bumpSound) : base(spriteSheet)
        {
            _flippedDuration = ConfigManager.GetConfig("FLIPPED_DURATION", 10);
            _level = level;
            _bumpSound = bumpSound?.CreateInstance();

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

            SetState(STATE_WALK);
        }

        public void SetState(string stateName)
        {
            _stateMachine.SetState(stateName);
        }

        public virtual void Update(float deltaTime)
        {
            _stateMachine.Update(deltaTime);
        }

        public override void Move(float deltaTime)
        {
            base.Move(deltaTime);
            UpdateSideScreen();
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
            if (!_isBumping)
            {
                _jumpTimer += deltaTime;
                float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * _jumpDuration)) * _jumpHeight;
                MoveTo(new Vector2(Position.X, _jumpStartingY - y));
            }

            Move(deltaTime);
            Animate(deltaTime);

            if (!IgnorePlatforms && IsUnderPlatform())
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

            if (!IgnorePlatforms && IsOnPlatform())
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
            return gridPositionY == 29 || _level[gridPositionX, gridPositionY] != MarioBros.LevelTile.Empty;
        }

        private bool IsUnderPlatform()
        {
            int gridPositionX = PixelPositionX / 8;
            int gridPositionY = (PixelPositionY - 20) / 8;
            return gridPositionY > 0 && _level[gridPositionX, gridPositionY] != MarioBros.LevelTile.Empty;
        }


        private bool _isBumping;
        protected void StartBump()
        {
            _isBumping = true;
            EventsManager.FireEvent("BUMP", this);
        }

        public void StopBump()
        {
            _isBumping = false;
            SetState(STATE_FALL);
        }

        public virtual void Bump(PlatformCharacter bumper, int direction, bool withSound)
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
                if (withSound)
                {
                    _bumpSound?.Play();
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
            Jump(0.25f, 15, animationName: null);
        }

        public virtual void Kill(PlatformCharacter killer, int direction = 0)
        {
            _killer = killer;
            if (direction != 0)
            {
                LookTo(new Vector2(direction, 0));
            }
            SetState(STATE_DYING);
        }

        public void Jump(float duration, float height, string animationName)
        {
            _jumpHeight = height;
            _jumpDuration = duration;
            SetAnimation(animationName);
            SetState(STATE_JUMP);
        }

        public void Fall(float duration, float height)
        {
            _jumpHeight = height;
            _jumpDuration = duration;
            SetState(STATE_FALL);
        }

        public void Walk()
        {
            SetState(STATE_WALK);
        }

        public override void Draw(SpriteBatch spriteBatch, int displayOffsetX = 0, int displayOffsetY = 0)
        {
            base.Draw(spriteBatch, displayOffsetX, displayOffsetY);
            // TODO: only draw alternate sprites if needed
            base.Draw(spriteBatch, displayOffsetX + MarioBros.SCREEN_WIDTH, displayOffsetY);
            base.Draw(spriteBatch, displayOffsetX - MarioBros.SCREEN_WIDTH, displayOffsetY);
        }

        #region States
        public bool IgnorePlatforms;
        protected virtual void IdleEnter()
        {
            SetAnimation("Idle");
        }

        protected virtual void IdleExit() { }
        protected virtual void IdleUpdate(float deltaTime, float stateElapsedTime)
        {
            Animate(deltaTime);
        }


        protected virtual void WalkEnter()
        {
            SetAnimation(WalkAnimationName);
        }
        protected virtual void WalkExit() { }

        protected virtual void WalkUpdate(float deltaTime, float stateElapsedTime)
        {
            if (!IgnorePlatforms && !IsOnPlatform())
            {
                Fall(0.25f, 15);
                return;
            }

            Move(deltaTime);
            Animate(deltaTime);
        }

        protected virtual void JumpEnter()
        {
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

        protected virtual void JumpUpdate(float deltaTime, float stateElapsedTime)
        {
            UpdateJump(deltaTime, out bool climaxReached, out bool hitPlatform);

            if (climaxReached)
            {
                SetState(STATE_FALL);
            }
            if (CanBump && hitPlatform && !_isBumping)
            {
                StartBump();
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

        protected virtual void FallUpdate(float deltaTime, float stateElapsedTime)
        {
            UpdateFall(deltaTime, out bool landed, out bool hitBottom);

            if (landed)
            {
                if (_isFlipped)
                {
                    SetState(STATE_FLIPPED);
                }
                else
                {
                    SetState(STATE_WALK);
                }
            }
            else if (hitBottom)
            {
                SetState(STATE_DEAD);
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

        protected virtual void FlippedUpdate(float deltaTime, float stateElapsedTime)
        {
            _flipTimer += deltaTime;
            if (_flipTimer > _flippedDuration)
            {
                Recover();
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

        protected virtual void Recover()
        {
            SetSpeed(1f);
            SetAnimationSpeed(1f);
            SetState(STATE_WALK);
        }

        protected virtual void DyingEnter()
        {
            IgnorePlatforms = true;
            IsDying = true;
            SetSpeed(1f);
        }
        protected virtual void DyingExit() { }
        protected virtual void DyingUpdate(float deltaTime, float stateElapsedTime)
        {
            Fall(0.25f, 15);
        }

        protected virtual void DeadEnter()
        {
            EventsManager.FireEvent("DEATH", this);
        }
        protected virtual void DeadExit() { }
        protected virtual void DeadUpdate(float deltaTime, float stateElapsedTime) { }

        #endregion
    }
}
