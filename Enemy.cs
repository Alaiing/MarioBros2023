using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
        public enum EnemyType : int { Turtle = 0, Crab = 1, Fly = 2 }

        private const int SPAWN_Y = 44;
        private const int LEFT_SPAWN_X = 44;
        private const int RIGHT_SPAWN_X = 212;
        private const int EXIT_Y = 199;
        private const int EXIT_MARGIN = 32;
        private const string STATE_EXITING = "Exiting";
        private const string STATE_ENTERING = "Entering";

        private float _enterExitTime;

        private const int SPAWN_DISTANCE = 9;
        private const int EXIT_DISTANCE = 16;

        public bool IsExiting => _stateMachine.CurrentState == STATE_EXITING;
        public bool IsEntering => _stateMachine.CurrentState == STATE_ENTERING;

        private int _enterExitSide;
        public int SpawnSide => _enterExitSide;

        private SoundEffectInstance _spawnSound;

        protected int _phase;
        protected override float CurrentFrame => base.CurrentFrame + _phase * 8;

        public Enemy(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect spawnSound, SoundEffect bumpSound) : base(spriteSheet, level, bumpSound)
        {
            _spawnSound = spawnSound?.CreateInstance();
        }

        protected override void InitStateMachine()
        {
            base.InitStateMachine();
            _stateMachine.AddState(STATE_EXITING, OnEnter: ExitEnter, OnExit: ExitExit, OnUpdate: ExitUpdate);
            _stateMachine.AddState(STATE_ENTERING, OnEnter: EnterEnter, OnExit: EnterExit, OnUpdate: EnterUpdate);
        }

        public void IncreasePhase()
        {
            if (_phase < 2)
            {
                _phase = _phase + 1;
                SetBaseSpeed(_baseSpeed * 1.25f);
                SetAnimationSpeed(_animationSpeed * 1.5f);
            }
        }

        public void ToMaxPhase()
        {
            IncreasePhase();
            IncreasePhase();
        }

        protected override void Recover()
        {
            base.Recover();
            IncreasePhase();
        }

        public void Enter(int side)
        {
            _enterExitSide = side;
            SetState(STATE_ENTERING);
        }

        protected override void WalkUpdate(float deltaTime)
        {
            base.WalkUpdate(deltaTime);
            if (PixelPositionY >= 208)
            {
                if (MoveDirection.X < 0 && PixelPositionX - SpriteSheet.LeftMargin <= EXIT_MARGIN
                    || MoveDirection.X > 0 && PixelPositionX + SpriteSheet.RightMargin > MarioBros.SCREEN_WIDTH - EXIT_MARGIN)
                {
                    _enterExitSide = MathF.Sign(MoveDirection.X);
                    SetState(STATE_EXITING);
                }
            }
        }

        private void ExitEnter()
        {
            IgnorePlatforms = true;
            _enterExitTime = 0;
            SetAnimation(WalkAnimationName);
            MoveTo(new Vector2(Position.X, EXIT_Y));
        }

        private void ExitExit()
        {

        }

        private void ExitUpdate(float deltaTime)
        {
            _enterExitTime += deltaTime;
            Move(deltaTime);
            Animate(deltaTime);

            if (_enterExitTime > EXIT_DISTANCE / CurrentSpeed)
            {
                SetState(STATE_ENTERING);
            }
        }

        private void EnterEnter()
        {
            IgnorePlatforms = true;
            _enterExitTime = 0;
            LookTo(new Vector2(-_enterExitSide, 0));
            float enterX = _enterExitSide > 0 ? RIGHT_SPAWN_X : LEFT_SPAWN_X;
            MoveTo(new Vector2(enterX, SPAWN_Y));
            SetAnimation(WalkAnimationName);
        }
        private void EnterExit()
        {
            IgnorePlatforms = false;
            _spawnSound?.Play();
        }
        private void EnterUpdate(float deltaTime)
        {
            _enterExitTime += deltaTime;
            Move(deltaTime);
            Animate(deltaTime);

            if (_enterExitTime > SPAWN_DISTANCE / CurrentSpeed)
            {
                SetState(STATE_WALK);
            }
        }

        protected override void DyingEnter()
        {
            base.DyingEnter();
            EventsManager.FireEvent<Enemy>("ENEMY_DYING", this);
        }
    }
}
