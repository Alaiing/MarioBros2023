using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioBros2023
{
    public class Player : PlatformCharacter
    {
        private const string STATE_RESPAWN = "Respawn";

        private float _currentSpeed;
        private float _acceleration;
        private float _maxSpeed;

        private int _lives;
        public int LivesLeft => _lives;

        private int _score;
        public int Score => _score;

        private int _combo;
        public float LastKillTimer;

        public bool IsMoving => _stateMachine.CurrentState == STATE_JUMP || _stateMachine.CurrentState == STATE_FALL || _currentSpeed != 0;

        private SoundEffectInstance[] _pootSteps;
        private int _currentPootStepIndex;
        private SoundEffectInstance _skid;
        private SoundEffectInstance _jump;
        private SoundEffectInstance _hit;
        private SoundEffectInstance _death;

        public Player(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, int lives, SoundEffect[] pootSteps, SoundEffect skid, SoundEffect jump, SoundEffect hit, SoundEffect death) : base(spriteSheet, level, null)
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
        }

        public void Respawn()
        {
            _stateMachine.SetState(STATE_RESPAWN);
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

        protected override void WalkUpdate(float deltaTime)
        {
            SimpleControls.GetStates();
            bool hasInput = false;
            if (SimpleControls.IsLeftDown(SimpleControls.PlayerNumber.Player1))
            {
                hasInput = true;
                _currentSpeed += -_acceleration * deltaTime;
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
            else if (SimpleControls.IsRightDown(SimpleControls.PlayerNumber.Player1))
            {
                hasInput = true;
                _currentSpeed += _acceleration * deltaTime;
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
            if (SimpleControls.IsADown(SimpleControls.PlayerNumber.Player1))
            {
                hasInput = true;
                Jump(ConfigManager.GetConfig("MARIO_JUMP_DURATION", 0.5f), ConfigManager.GetConfig("MARIO_JUMP_HEIGHT", 75));
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
                    SetAnimation("Idle");
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
            base.WalkUpdate(deltaTime);
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

        private float _dyingTimer;
        protected override void DyingEnter()
        {
            base.DyingEnter();
            _dyingTimer = 0;
            SetAnimation("Hit");
            _hit.Play();
        }

        protected override void DyingUpdate(float deltaTime)
        {
            _dyingTimer += deltaTime;
            if (_dyingTimer > 1f)
            {
                SetSpeed(0f);
                _currentSpeed = 0;
                Jump(0.25f, 15);
                SetAnimation("Death");
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
    }
}
