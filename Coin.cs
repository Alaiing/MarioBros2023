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

        private SoundEffectInstance _collectSound;

        public Coin(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect spawnSound, SoundEffect collectSound) : base(spriteSheet, level, spawnSound, null)
        {
            _collectSound = collectSound?.CreateInstance();
            _onAnimationFrame += OnAnimationFrame;
        }

        private void OnAnimationFrame(int frameIndex)
        {
            if (frameIndex == 3)
                _movementDone = true;
        }

        protected override void InitStateMachine()
        {
            base.InitStateMachine();
        }

        public override void Bump(int direction, bool withSound)
        {
            Kill(0);
        }

        private bool _animationDone;
        private bool _movementDone;
        protected override void DyingEnter()
        {
            SetSpeed(0f);
            _collectSound?.Play();
            _animationDone = false;
            _movementDone = false;
            SetAnimation("Collect", () => _animationDone = true);
        }

        protected override void DyingUpdate(float deltaTime)
        {
            if (_animationDone)
            {
                SetState(STATE_DEAD);
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
    }
}
