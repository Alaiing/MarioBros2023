using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Oudidon
{
    public class Character
    {
        public string name;
        private bool _enabled = true;
        public virtual bool Visible { get; set; }
        public bool IsAlive => _enabled;
        private readonly SpriteSheet _spriteSheet;
        public SpriteSheet SpriteSheet => _spriteSheet;
        private Vector2 _position;
        public Vector2 Position => _position;
        public int PixelPositionX => (int)MathF.Floor(_position.X);
        public int PixelPositionY => (int)MathF.Floor(_position.Y);
        private float _currentFrame;
        protected virtual float CurrentFrame => _currentFrame;
        private Color _color;
        public Color Color => _color;

        private Vector2 _orientation;
        private Vector2 _moveDirection;
        public Vector2 MoveDirection => _moveDirection;
        protected float _currentRotation;
        public float CurrentRotation => _currentRotation;
        protected Vector2 _currentScale;
        public Vector2 CurrentScale => _currentScale;
        protected float _baseSpeed;
        protected float _speed;
        public float CurrentSpeed => _speed * _baseSpeed;
        private float _moveStep;
        protected float _animationSpeed;
        private string _currentAnimation;
        private int _currentAnimationFrameCount;
        private float _currentAnimationSpeed;

        public bool CanChangeDirection { get; set; }

        private Action _onAnimationEnd;
        protected Action<int> _onAnimationFrame;

        public Character(SpriteSheet spriteSheet)
        {
            _spriteSheet = spriteSheet;
            _currentScale = Vector2.One;
            _currentFrame = 0;
            _moveStep = 0;
            _color = Color.White;
            CanChangeDirection = true;
            LookTo(new Vector2(1, 0));
            _animationSpeed = 1f;
            _speed = 1f;
            Visible = true;
        }

        public void SetColor(Color color)
        {
            _color = color;
        }

        public void SetBaseSpeed(float speed)
        {
            _baseSpeed = speed;
            if (_baseSpeed == 0)
            {
                _moveStep = 0;
            }
        }

        public void SetSpeed(float speed)
        {
            _speed = speed;
            if (speed == 0)
            {
                _moveStep = 0;
            }
        }

        public void SetAnimationSpeed(float animationSpeed)
        {
            _animationSpeed = animationSpeed;
        }

        public void SetFrame(int frameIndex)
        {
            if (frameIndex > 0 && frameIndex < _spriteSheet.FrameCount)
            {
                _currentFrame = frameIndex;
            }
        }

        public void SetAnimation(string animationName, Action onAnimationEnd = null)
        {
            if (_currentAnimation != animationName && _spriteSheet.HasAnimation(animationName))
            {
                _currentAnimation = animationName;
                _currentAnimationFrameCount = _spriteSheet.GetAnimationFrameCount(animationName);
                _currentAnimationSpeed = _spriteSheet.GetAnimationSpeed(animationName);

                _currentFrame = 0;

                _onAnimationEnd = onAnimationEnd;
            }
        }

        public void MoveTo(Vector2 position)
        {
            _position = position;
        }

        public void MoveBy(Vector2 translation)
        {
            _position += translation;
        }

        public void LookTo(Vector2 direction)
        {
            _moveDirection = direction;
            if (direction.X != 0)
            {
                _orientation.X = direction.X;
                _currentScale.X = direction.X;
            }
            if (direction.Y != 0)
            {
                _orientation.Y = direction.Y;
                _currentScale.Y = -_orientation.X * _orientation.Y;
            }
            else
            {
                _currentScale.Y = 1;
            }
            _orientation.Y = direction.Y;
            _currentRotation = _orientation.X * _orientation.Y * MathF.PI / 2;
        }

        public virtual void Move(float deltaTime)
        {
            _moveStep += deltaTime * CurrentSpeed;
            if (_moveStep >= 1)
            {
                _position += _moveDirection;
                _moveStep -= 1;
            }
        }

        public void Animate(float deltaTime)
        {
            int previousFrame = (int)MathF.Floor(_currentFrame);
            _currentFrame = _currentFrame + deltaTime * _currentAnimationSpeed * _animationSpeed;
            if (_currentFrame > _currentAnimationFrameCount)
            {
                _currentFrame = 0;
                if (_onAnimationEnd != null)
                {
                    _onAnimationEnd?.Invoke();
                }
            }
            int newFrame = (int)MathF.Floor(_currentFrame);
            if (previousFrame != newFrame)
            {
                _onAnimationFrame?.Invoke(newFrame);
            }
        }

        public void Die()
        {
            _enabled = false;
        }

        public virtual void Draw(SpriteBatch spriteBatch, int displayOffsetX = 0, int displayOffsetY = 0)
        {
            if(Visible)
            {
                _spriteSheet.DrawAnimationFrame(_currentAnimation, (int)MathF.Floor(CurrentFrame), spriteBatch, new Vector2(PixelPositionX + displayOffsetX, PixelPositionY + displayOffsetY), _currentRotation, _currentScale, _color);
            }
        }
    }
}
