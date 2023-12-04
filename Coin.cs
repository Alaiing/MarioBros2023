using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioBros2023
{
    public class Coin : Enemy
    {
        public override string WalkAnimationName => "Rotate";

        private bool _isBeingCollected;
        public bool IsBeingCollected => _isBeingCollected;
        private Vector2 _initialPosition;
        public Vector2 InitialPosition => _initialPosition; 

        private SoundEffectInstance _collectSound;

        public Coin(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect spawnSound, SoundEffect collectSound) : base(spriteSheet, level, spawnSound, null)
        {
            _collectSound = collectSound?.CreateInstance();
            _onAnimationFrame += OnAnimationFrame;
        }

        private void OnAnimationFrame(int frameIndex)
        {
            if (frameIndex == 3)
            {
                _movementDone = true;
            }
        }

        public override void Bump(PlatformCharacter bumper, int direction, bool withSound)
        {
            Kill(bumper, 0);
        }

        private bool _animationDone;
        private bool _movementDone;
        protected override void DyingEnter()
        {
            base.DyingEnter();
            SetSpeed(0f);
            _collectSound?.Play();
            _animationDone = false;
            _movementDone = false;
            _isBeingCollected = true;
            _initialPosition = Position;
            SetAnimation("Collect", () => _animationDone = true);
        }

        protected override void DyingUpdate(float deltaTime)
        {
            if (_animationDone)
            {
                SetState(STATE_DEAD);
                EventsManager.FireEvent("COIN_COLLECTED", this, _killer);
            }
            else
            {
                Animate(deltaTime);
                if (!_movementDone)
                {
                    MoveTo(Position - new Vector2(0, deltaTime * 32));
                }
            }
        }

        protected override void ExitUpdate(float deltaTime)
        {
            _enterExitTime += deltaTime;
            Move(deltaTime);
            Animate(deltaTime);

            if (_enterExitTime > EXIT_DISTANCE / CurrentSpeed)
            {
                SetState(STATE_DEAD);
            }
        }

    }
}
