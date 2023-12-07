using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Oudidon;
using System;

namespace MarioBros2023
{
    public class Bump
    {
        public bool Enabled { get; private set; }
        private float _currentFrame;
        private readonly PlatformCharacter _character;
        private readonly SpriteSheet _spriteSheet;

        private int _gridCellX;
        private int _gridCellY;

        private int _tileSetIndex;

        public Bump(SpriteSheet spriteSheet, int tileSetIndex, PlatformCharacter character)
        {
            _character = character;
            _spriteSheet = spriteSheet;
            _currentFrame = 0;
            _gridCellX = character.PixelPositionX / 8;
            _gridCellY = (character.PixelPositionY - 20) / 8;
            Enabled = true;
            _tileSetIndex = tileSetIndex;
        }

        public void Update(float deltaTime)
        {
            if (Enabled)
            {
                _currentFrame += deltaTime * 20;
                if (_currentFrame >= 5)
                {
                    _character.StopBump();
                    Enabled = false;
                }
            }
        }

        public bool IsBumped(int x, int y)
        {
            return y == _gridCellY && x >= _gridCellX - 1 && x <= _gridCellX + 1;
        }

        public void Draw(SpriteBatch spriteBatch, int x, int y)
        {
            if (x == _gridCellX - 1)
                _spriteSheet.DrawFrame((int)MathF.Floor(_currentFrame) * 3 + _tileSetIndex * 15, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
            else if (x == _gridCellX)
                _spriteSheet.DrawFrame((int)MathF.Floor(_currentFrame) * 3 + _tileSetIndex * 15 + 1, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
            else if (x == _gridCellX + 1)
                _spriteSheet.DrawFrame((int)MathF.Floor(_currentFrame) * 3 + _tileSetIndex * 15 + 2, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
        }
    }
}
