using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Oudidon;
using System;

namespace MarioBros2023
{
    public class Player : PlatformCharacter
    {
        private const string STATE_RESPAWN = "Respawn";
        private const string STATE_FLATTENED = "Flattened";

        private float _currentSpeed;
        private float _acceleration;
        private float _maxSpeed;

        public float MaxSpeed => _maxSpeed;
        public float NormalizedCurrentSpeed => MathF.Abs(_currentSpeed) / _maxSpeed;

        private int _lives;
        public int LivesLeft => _lives;
        private bool _extraLifeGained;
        public bool ExtraLifeGained => _extraLifeGained;

        private int _score;
        public int Score => _score;

        private int _combo;
        public float LastKillTimer;

        public bool IsMoving => _stateMachine.CurrentState == STATE_JUMP || _stateMachine.CurrentState == STATE_FALL || _currentSpeed != 0;

        public bool IsFlattened => _stateMachine.CurrentState == STATE_FLATTENED;

        private SoundEffectInstance[] _pootSteps;
        private int _currentPootStepIndex;
        private SoundEffectInstance _skid;
        private SoundEffectInstance _jump;
        private SoundEffectInstance _hit;
        private SoundEffectInstance _death;

        private SimpleControls.PlayerNumber _playerNumber;

        public RespawnPlatform RespawnPlatform { get; set; }

        private float _pushedSpeed { get; set; }
        private bool _isBeingPushed;

        public static bool InContact;

        public Player(SpriteSheet spriteSheet, SimpleControls.PlayerNumber playerNumber, MarioBros.LevelTile[,] level, int lives, SoundEffect[] pootSteps, SoundEffect skid, SoundEffect jump, SoundEffect hit, SoundEffect death) : base(spriteSheet, level, null)
        {
            _maxSpeed = ConfigManager.GetConfig("MARIO_MAX_SPEED", 75f);
            _acceleration = ConfigManager.GetConfig("MARIO_ACCELERATION", 400f);
            _lives = lives;

            _pootSteps = new SoundEffectInstance[pootSteps.Length];
            for (int i = 0; i < pootSteps.Length; i++)
            {
                _pootSteps[i] = pootSteps[i].CreateInstance();
                _pootSteps[i].Volume = 0.75f;
            }

            _skid = skid.CreateInstance();
            _jump = jump.CreateInstance();
            _hit = hit.CreateInstance();
            _death = death.CreateInstance();

            _onAnimationFrame += OnFrameChange;
            _currentPootStepIndex = 0;

            _playerNumber = playerNumber;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);
            LastKillTimer += deltaTime;
        }

        public int GetKillCombo()
        {
            if (LastKillTimer < ConfigManager.GetConfig("COMBO_MIN_TIME", 0.5f))
                _combo++;
            else
                _combo = 0;
            LastKillTimer = 0;
            return _combo;
        }

        private void OnFrameChange(int frameIndex)
        {
            if (_stateMachine.CurrentState == STATE_WALK && _currentSpeed != 0)
            {
                if (frameIndex == 2)
                {
                    //_pootSteps[_currentPootStepIndex].Stop();

                    _currentPootStepIndex = (_currentPootStepIndex + 1) % _pootSteps.Length;
                    _pootSteps[_currentPootStepIndex].Play();
                }
            }
        }

        protected override void InitStateMachine()
        {
            base.InitStateMachine();
            _stateMachine.AddState(STATE_RESPAWN, RespawnEnter, null, null);
            _stateMachine.AddState(STATE_FLATTENED, FlattenedEnter, null, FlattenedUpdate);
        }

        public void Respawn()
        {
            RespawnPlatform.Reset();
            _stateMachine.SetState(STATE_RESPAWN);
        }

        public void Flatten()
        {
            _stateMachine.SetState(STATE_FLATTENED);
        }

        public void AddLife()
        {
            _lives = Math.Min(3, _lives + 1);
            _extraLifeGained = true;
        }

        public void ResetLives(int lives)
        {
            _lives = lives;
        }

        public void ResetState()
        {
            IsDying = false;
            _currentSpeed = 0;
            _currentPootStepIndex = 0;
            SetSpeed(1f);
            Walk();
        }

        protected override void WalkEnter()
        {
            SetAnimation("Idle");
        }

        protected override void WalkUpdate(float deltaTime, float stateElapsedTime)
        {
            SimpleControls.GetStates();
            bool hasInput = false;
            if (!_isBeingPushed)
            {
                if (SimpleControls.IsLeftDown(_playerNumber))
                {
                    hasInput = true;
                    if (_currentSpeed == 0)
                    {
                        _currentSpeed = -_maxSpeed / 2;
                    }
                    else
                    {
                        _currentSpeed += -_acceleration * deltaTime;
                    }

                    if (MathF.Abs(_currentSpeed) >= _maxSpeed)
                    {
                        _currentSpeed = -_maxSpeed;
                    }
                    if (_currentSpeed >= 0)
                    {
                        SetAnimation("Slip");
                        _skid.Play();
                    }
                    else
                    {
                        SetAnimation(WalkAnimationName);
                    }
                }
                else if (SimpleControls.IsRightDown(_playerNumber))
                {
                    hasInput = true;
                    if (_currentSpeed == 0)
                    {
                        _currentSpeed = _maxSpeed / 2;
                    }
                    else
                    {
                        _currentSpeed += _acceleration * deltaTime;
                    }
                    if (MathF.Abs(_currentSpeed) >= _maxSpeed)
                    {
                        _currentSpeed = _maxSpeed;
                    }

                    if (_currentSpeed <= 0)
                    {
                        SetAnimation("Slip");
                        _skid.Play();
                    }
                    else
                    {
                        SetAnimation(WalkAnimationName);
                    }
                }
            }
            if (SimpleControls.IsADown(_playerNumber))
            {
                hasInput = true;
                PlayerJump();
                _jump.Play();
            }

            if (!hasInput)
            {
                if (_currentSpeed != 0)
                {
                    if (CanSkid())
                    {
                        SetAnimation("Slip");
                        _skid.Play();
                    }
                    float previousSpeed = _currentSpeed;
                    _currentSpeed += -MathF.Sign(_currentSpeed) * _acceleration * deltaTime;
                    if (previousSpeed * _currentSpeed < 0)
                    {
                        _currentSpeed = 0;
                    }
                }
                else
                {
                    if (!_isBeingPushed)
                    {
                        SetAnimation("Idle");
                    }
                    else
                    {
                        SetAnimation("Slip");
                        _skid.Play();
                    }
                }
            }
            else
            {
                IgnorePlatforms = false;
            }

            if (_currentSpeed != 0)
            {
                LookTo(new Vector2(MathF.Sign(_currentSpeed), 0));
            }

            SetBaseSpeed(MathF.Abs(_currentSpeed));
            base.WalkUpdate(deltaTime, stateElapsedTime);
        }

        public void PlayerJump()
        {
            Jump(ConfigManager.GetConfig("MARIO_JUMP_DURATION", 0.5f), ConfigManager.GetConfig("MARIO_JUMP_HEIGHT", 75), "Jump");
        }

        private bool CanSkid()
        {
            return MathF.Abs(_currentSpeed) >= _maxSpeed;
        }

        public void IncreaseScore(int scoreIncrease)
        {
            _score += scoreIncrease;
        }

        public void ResetScore()
        {
            _score = 0;
        }

        protected override void DyingEnter()
        {
            base.DyingEnter();
            SetAnimation("Hit");
            _hit.Play();
        }

        protected override void DyingUpdate(float deltaTime, float stateElapsedTime)
        {
            if (stateElapsedTime > 1f)
            {
                SetSpeed(0f);
                _currentSpeed = 0;
                Jump(0.25f, 15, "Death");
                _death.Play();
            }
        }

        protected override void DeadEnter()
        {
            if (_lives >= 0)
            {
                _lives--;
            }
            base.DeadEnter();
        }

        private void RespawnEnter()
        {
            SetSpeed(1f);
            SetAnimation("Idle");
            IsDying = false;
        }

        protected override void JumpExit() { }

        protected override void FallExit()
        {
            SetSpeed(1f);
        }

        private void FlattenedEnter()
        {
            SetAnimation("Flatten", () => Walk());
        }

        private void FlattenedUpdate(float deltaTime, float stateElapsedTime)
        {
            Animate(deltaTime);
        }

        public override void Bump(PlatformCharacter bumper, int direction, bool withSound)
        {
            SetSpeed(0f);
            _currentSpeed = 0;
            Jump(0.25f, 15, animationName: null);
        }

        public void Push(float velocity)
        {
            _pushedSpeed = velocity;
            _isBeingPushed = _currentSpeed == 0;
        }

        public void StopPush()
        {
            _pushedSpeed = 0;
            _isBeingPushed = false;
        }

        public override void Move(float deltaTime)
        {
            base.Move(deltaTime);
            _position += new Vector2(1, 0) * _pushedSpeed * deltaTime;
        }
    }
}
