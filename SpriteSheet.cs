using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Oudidon
{
    public class SpriteSheet
    {
        public struct Animation
        {
            public int startingFrame;
            public int endFrame;
            public float speed;
            public readonly int FrameCount => endFrame - startingFrame + 1;
        }

        private readonly Texture2D _texture;
        private Rectangle[] allFrames;
        private Dictionary<string, Animation> _animations = new Dictionary<string, Animation>();

        public int FrameCount => allFrames.Length;

        private Vector2 _spritePivot;
        public Vector2 SpritePivot => _spritePivot;

        public int SpriteWidth { get; private set; }
        public int SpriteHeight { get; private set; }

        public int LeftMargin { get; private set; }
        public int RightMargin { get; private set; }
        public int TopMargin { get; private set; }
        public int BottomMargin { get; private set; }

        public SpriteSheet(ContentManager content, string asset, int spriteWidth, int spriteHeight, int spritePivotX = 0, int spritePivotY = 0)
        {
            _texture = content.Load<Texture2D>(asset);
            _spritePivot = new Vector2(spritePivotX, spritePivotY);
            LeftMargin = spritePivotX;
            RightMargin = spriteWidth - spritePivotX;
            TopMargin = spritePivotY;
            BottomMargin = spriteHeight - spritePivotY;
            SpriteWidth = spriteWidth;
            SpriteHeight = spriteHeight;
            InitFrames(spriteWidth, spriteHeight);
        }

        private void InitFrames(int spriteWidth, int spriteHeight)
        {
            int xCount = _texture.Width / spriteWidth;
            int yCount = _texture.Height / spriteHeight;
            allFrames = new Rectangle[xCount * yCount];

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    allFrames[x + y * xCount] = new Rectangle(x * spriteWidth, y * spriteHeight, spriteWidth, spriteHeight);
                }
            }
        }

        public void RegisterAnimation(string name, int startingFrame, int endingFrame, float animationSpeed)
        {
            _animations.Add(name, new Animation { startingFrame = startingFrame, endFrame = endingFrame, speed = animationSpeed });
        }

        public bool HasAnimation(string name)
        {
            return !string.IsNullOrEmpty(name) && _animations.ContainsKey(name);
        }

        public int GetAnimationFrameCount(string animationName)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return animation.FrameCount;
            }

            return -1;
        }

        public float GetAnimationSpeed(string animationName)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                return animation.speed;
            }

            return 0f;
        }

        public void DrawAnimationFrame(string animationName, int frameIndex, SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 scale, Color color)
        {
            if (_animations.TryGetValue(animationName, out Animation animation) /*&& frameIndex < animation.FrameCount*/)
            {
                DrawFrame(animation.startingFrame + frameIndex, spriteBatch, position, rotation, scale, color);
            }
        }

        public void DrawFrame(int frameIndex, SpriteBatch spriteBatch, Vector2 position, float rotation, Vector2 scale, Color color)
        {
            if (frameIndex < allFrames.Length)
            {
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (scale.X < 0)
                {
                    spriteEffects |= SpriteEffects.FlipHorizontally;
                    scale.X = -scale.X;
                }
                if (scale.Y < 0)
                {
                    spriteEffects |= SpriteEffects.FlipVertically;
                    scale.Y = -scale.Y;
                }
                spriteBatch.Draw(_texture, position, allFrames[frameIndex], color, rotation, _spritePivot, scale, spriteEffects, 0);
            }
        }
    }
}