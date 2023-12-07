using Microsoft.Xna.Framework.Audio;
using Oudidon;

namespace MarioBros2023
{
    public class Crab : Enemy
    {
        private bool _angry;
        protected override float CurrentFrame => base.CurrentFrame + 4 * _phase;

        public override string WalkAnimationName => _angry ? "RunAngry" : "Run";
        public Crab(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect spawnSound, SoundEffect bumpSound) : base(spriteSheet, level, spawnSound, bumpSound) { }

        public override void Bump(PlatformCharacter player, int direction, bool withSound)
        {
            if (!IsFlipped && !_angry)
            {
                _angry = true;
                SetAnimation(WalkAnimationName);
                SetBaseSpeed(_baseSpeed + 7f);
                SetSpeed(0f);
                Jump(0.25f, 15, animationName: null);
            }
            else
            {
                base.Bump(player, direction, withSound);
                if (IsFlipped)
                {
                    _angry = false;
                    SetBaseSpeed(_baseSpeed - 7f);
                }
            }
        }

        protected override void JumpExit()
        {
            if (!_isFlipped && !_angry)
            {
                SetSpeed(1f);
            }
        }

        protected override void FallExit()
        {
            base.FallExit();
            if (_angry)
            {
                SetSpeed(1f);
            }
        }
    }
}
