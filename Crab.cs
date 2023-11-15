using Microsoft.Xna.Framework.Audio;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarioBros2023
{
    public class Crab : Enemy
    {
        private bool _angry;
        protected override float CurrentFrame => base.CurrentFrame + 4 * _phase;

        public override string WalkAnimationName => _angry ? "RunAngry" : "Run";
        public Crab(SpriteSheet spriteSheet, MarioBros.LevelTile[,] level, SoundEffect spawnSound, SoundEffect bumpSound) : base(spriteSheet, level, spawnSound, bumpSound)
        {

        }

        public override void Bump(int direction, bool withSound)
        {
            if (!IsFlipped && !_angry)
            {
                _angry = true;
                SetAnimation(WalkAnimationName);
                SetBaseSpeed(_baseSpeed + 7f);
                Jump(0.25f, 15);
            }
            else
            {
                base.Bump(direction, withSound);
                if (IsFlipped)
                {
                    _angry = false;
                    SetBaseSpeed(_baseSpeed - 7f);
                }
            }
        }
    }
}
