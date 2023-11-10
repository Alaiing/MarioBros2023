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
    public class Player : PlatformCharacter
    {
        private const string STATE_RESPAWN = "Respawn";

        private float _currentSpeed;
        private float _acceleration;
        private float _maxSpeed;

        private int _lives;
        public int LivesLeft => _lives;

        public bool IsMoving => _stateMachine.CurrentState == STATE_JUMP || _stateMachine.CurrentState == STATE_FALL || _currentSpeed != 0;

        public Player(SpriteSheet spriteSheet, bool[,] level, int lives) : base(spriteSheet, level)
        {
            _maxSpeed = ConfigManager.GetConfig("MARIO_MAX_SPEED", 75f);
            _acceleration = ConfigManager.GetConfig("MARIO_ACCELERATION", 400f);
            _lives = lives;
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
            Walk();
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
                }
                else
                {
                    SetAnimation("Run");
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
                }
                else
                {
                    SetAnimation("Run");
                }
            }
            if (SimpleControls.IsADown(SimpleControls.PlayerNumber.Player1))
            {
                hasInput = true;
                Jump(ConfigManager.GetConfig("MARIO_JUMP_DURATION", 0.5f), ConfigManager.GetConfig("MARIO_JUMP_HEIGHT", 75));
            }

            if (!hasInput)
            {
                if (_currentSpeed != 0)
                {
                    SetAnimation("Slip");
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
                _ignorePlatforms = false;
            }

            if (_currentSpeed != 0)
            {
                LookTo(new Vector2(MathF.Sign(_currentSpeed), 0));
            }

            SetBaseSpeed(MathF.Abs(_currentSpeed));
            base.WalkUpdate(deltaTime);
        }

        private float _dyingTimer;
        protected override void DyingEnter()
        {
            base.DyingEnter();
            _dyingTimer = 0;
            SetAnimation("Hit");
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
    }
}
