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
    public class RespawnPlatform
    {
        public enum RespawnSide { Left = -1, Right = 1 };

        private const float RESPAWN_PLATFORM_APPEARANCE_DURATION = 2f;
        private const float RESPAWN_PLATFORM_DURATION = 12f;
        private const float RESPAWN_PLATFORM_START_Y = 10f;
        private const float RESPAWN_PLATFORM_Y = 40f;
        private const float RESPAWN_PLATFORM_LEFT_X = 108f;
        private const float RESPAWN_PLATFORM_RIGHT_X = 132f;

        private float _timer;
        private float _currentFrame;
        private Player _player;
        private float _currentY;
        private float _positionX;

        private SpriteSheet _spriteSheet;
        private RespawnSide _side;

        public RespawnPlatform(Player player, SpriteSheet spriteSheet, RespawnSide side)
        {
            _player = player;
            _spriteSheet = spriteSheet;
            if (side == RespawnSide.Left)
            {
                _positionX = RESPAWN_PLATFORM_LEFT_X;
            }
            else
            {
                _positionX = RESPAWN_PLATFORM_RIGHT_X;
            }
            _side = side;
            ClearPlatform();
        }

        public void Update(float deltaTime)
        {
            if (_currentFrame >= 0)
            {
                if (_player.IsMoving)
                {
                    ClearPlatform();
                    return;
                }

                _timer += deltaTime;
                if (_timer <= RESPAWN_PLATFORM_APPEARANCE_DURATION)
                {
                    _currentY = MathHelper.Lerp(RESPAWN_PLATFORM_START_Y, RESPAWN_PLATFORM_Y, _timer / 2f);
                    _player.MoveTo(new Vector2(_positionX + 8, _currentY));
                }
                else
                {
                    _player.Walk();
                    float platformTime = _timer - RESPAWN_PLATFORM_APPEARANCE_DURATION;
                    if (platformTime >= RESPAWN_PLATFORM_DURATION)
                    {
                        ClearPlatform();
                    }
                    else
                    {
                        _currentFrame = platformTime / (RESPAWN_PLATFORM_DURATION / 3);
                    }
                }
            }
        }

        public void Reset()
        {
            _currentFrame = 0;
            _currentY = RESPAWN_PLATFORM_START_Y;
            _timer = 0;

            _player.MoveTo(new Vector2(_positionX + 8, _currentY));
            _player.LookTo(new Vector2(-(int)_side, 0));
        }

        public void ClearPlatform()
        {
            _currentFrame = -1f;
            _player.IgnorePlatforms = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_currentFrame >= 0)
            {
                _spriteSheet.DrawFrame((int)MathF.Floor(_currentFrame), spriteBatch, new Vector2(_positionX, _currentY), 0, Vector2.One, Color.White);
            }
        }
    }
}
