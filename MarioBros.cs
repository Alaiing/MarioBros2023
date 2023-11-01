using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Oudidon;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MarioBros2023
{
    public class MarioBros : Game
    {
        public const int SCREEN_WIDTH = 256;
        public const int SCREEN_HEIGHT = 240;
        private const int SCREEN_SCALE = 4;

        private const int ENEMY_SPAWN_Y = 44;
        private const int ENEMY_LEFT_SPAWN_X = 44;
        private const int ENEMY_RIGHT_SPAWN_X = 212;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _renderTarget;

        private Random _random;

        private SpriteSheet _marioSpriteSheet;
        private Character _mario;

        private SpriteSheet _turtleSpriteSheet;
        private readonly List<PlatformCharacter> _enemies = new List<PlatformCharacter>();

        private const float MARIO_MAX_SPEED = 75f;
        public const float JUMP_DURATION = 0.5f;
        public const int MARIO_JUMP_HEIGHT = 55;
        private float _jumpTimer = 0;
        private float _currentMarioSpeed;
        private float _acceleration = 400f;
        private bool _isJumping = false;
        private bool _isFalling = false;
        private float _jumpStartingY;

        private bool[,] _level;
        private SpriteSheet _tiles;
        private SpriteSheet _bumpTiles;
        private Texture2D _levelOverlay;

        private int _bumpState;
        private float _bumpCurrentFrame;
        private int _bumpX, _bumpY;

        public MarioBros()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = SCREEN_WIDTH * SCREEN_SCALE;
            _graphics.PreferredBackBufferHeight = SCREEN_HEIGHT * SCREEN_SCALE;
            _graphics.ApplyChanges();

            _random = new Random();
        }

        protected override void Initialize()
        {
            _renderTarget = new RenderTarget2D(GraphicsDevice, SCREEN_WIDTH, SCREEN_HEIGHT);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            ConfigManager.LoadConfig("config.ini");

            _marioSpriteSheet = new SpriteSheet(Content, "Mario", 16, 24, 8, 24);
            _marioSpriteSheet.RegisterAnimation("Idle", 0, 0, 0);
            _marioSpriteSheet.RegisterAnimation("Run", 1, 3, 30f);
            _marioSpriteSheet.RegisterAnimation("Jump", 4, 4, 0);
            _marioSpriteSheet.RegisterAnimation("Slip", 5, 5, 0);
            _marioSpriteSheet.RegisterAnimation("Hit", 6, 6, 0);
            _marioSpriteSheet.RegisterAnimation("Death", 7, 7, 0);
            _marioSpriteSheet.RegisterAnimation("Flatten", 8, 9, 1f);

            _turtleSpriteSheet = new SpriteSheet(Content, "turtle", 16, 16, 8, 16);
            _turtleSpriteSheet.RegisterAnimation("Walk", 0, 3, 20f);
            _turtleSpriteSheet.RegisterAnimation("Turn", 4, 5, 4f);
            _turtleSpriteSheet.RegisterAnimation("OnBack", 6, 7, 1f);

            _mario = new Character(_marioSpriteSheet);
            _mario.SetAnimation("Idle");
            _mario.MoveTo(new Vector2(SCREEN_WIDTH / 2, SCREEN_HEIGHT - 48));
            _currentMarioSpeed = 0;

            _level = new bool[32, 30];
            for (int i = 0; i < 12; i++)
            {
                _level[i, 20] = true;
                _level[31 - i, 20] = true;
            }
            for (int i = 0; i < 4; i++)
            {
                _level[i, 15] = true;
                _level[31 - i, 15] = true;
            }
            for (int i = 0; i < 16; i++)
            {
                _level[8 + i, 14] = true;
            }

            for (int i = 0; i < 14; i++)
            {
                _level[i, 8] = true;
                _level[31 - i, 8] = true;
            }
            for (int i = 0; i < 32; i++)
            {
                _level[i, 26] = true;
                _level[i, 27] = true;
            }
            _tiles = new SpriteSheet(Content, "tiles", 8, 8);
            _bumpTiles = new SpriteSheet(Content, "tile-bump", 8, 16);

            _levelOverlay = Content.Load<Texture2D>("overlay");

            SpawnTurtle(1);
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SimpleControls.GetStates();

            UpdateBump(deltaTime);

            if (_mario.Position.X >= SCREEN_WIDTH)
            {
                _mario.MoveTo(new Vector2(_mario.Position.X - SCREEN_WIDTH, _mario.Position.Y));
            }
            else if (_mario.Position.X < 0)
            {
                _mario.MoveTo(new Vector2(_mario.Position.X + SCREEN_WIDTH, _mario.Position.Y));
            }

            int gridPositionX = _mario.PixelPositionX / 8;
            int gridPositionY = _mario.PixelPositionY / 8;
            bool isOnPlatform = !_isJumping && (gridPositionY == 29 || _level[gridPositionX, gridPositionY]);

            gridPositionY = (_mario.PixelPositionY - 20) / 8;
            bool isUnderPlatform = gridPositionY > 0 && _isJumping && _level[gridPositionX, gridPositionY];

            if (!_isJumping && !_isFalling)
            {
                bool hasInput = false;
                if (!isOnPlatform)
                {
                    _isFalling = true;
                    _jumpTimer = 0;
                    _jumpStartingY = _mario.Position.Y;
                    _mario.SetAnimationSpeed(0);
                }
                else
                {
                    if (SimpleControls.IsLeftDown(SimpleControls.PlayerNumber.Player1))
                    {
                        hasInput = true;
                        _currentMarioSpeed += -_acceleration * deltaTime;
                        if (MathF.Abs(_currentMarioSpeed) >= MARIO_MAX_SPEED)
                        {
                            _currentMarioSpeed = -MARIO_MAX_SPEED;
                        }
                        if (_currentMarioSpeed >= 0)
                        {
                            _mario.SetAnimation("Slip");
                        }
                        else
                        {
                            _mario.SetAnimation("Run");
                        }
                    }
                    else if (SimpleControls.IsRightDown(SimpleControls.PlayerNumber.Player1))
                    {
                        hasInput = true;
                        _currentMarioSpeed += _acceleration * deltaTime;
                        if (MathF.Abs(_currentMarioSpeed) >= MARIO_MAX_SPEED)
                        {
                            _currentMarioSpeed = MARIO_MAX_SPEED;
                        }

                        if (_currentMarioSpeed <= 0)
                        {
                            _mario.SetAnimation("Slip");
                        }
                        else
                        {
                            _mario.SetAnimation("Run");
                        }
                    }
                    if (SimpleControls.IsADown(SimpleControls.PlayerNumber.Player1))
                    {
                        hasInput = true;
                        _isJumping = true;
                        _jumpTimer = 0;
                        _jumpStartingY = _mario.Position.Y;
                        _mario.SetAnimation("Jump");
                    }


                    if (!hasInput)
                    {
                        if (_currentMarioSpeed != 0)
                        {
                            _mario.SetAnimation("Slip");
                            float previousSpeed = _currentMarioSpeed;
                            _currentMarioSpeed += -MathF.Sign(_currentMarioSpeed) * _acceleration * deltaTime;
                            if (previousSpeed * _currentMarioSpeed < 0)
                            {
                                _currentMarioSpeed = 0;
                            }
                        }
                        else
                        {
                            _mario.SetAnimation("Idle");
                        }
                    }
                }
            }
            else
            {
                if (isOnPlatform)
                {
                    _isJumping = false;
                    _isFalling = false;
                    _mario.MoveTo(new Vector2(_mario.Position.X, (_mario.PixelPositionY / 8) * 8));
                    _mario.SetAnimationSpeed(1f);
                }
                else
                {
                    if (_isJumping)
                    {
                        _jumpTimer += deltaTime;
                        float t = _jumpTimer;
                        float y = -(1 / (JUMP_DURATION * JUMP_DURATION)) * t * t + (2 / JUMP_DURATION) * t;

                        y *= MARIO_JUMP_HEIGHT;

                        if (!isUnderPlatform)
                        {
                            _mario.MoveTo(new Vector2(_mario.Position.X, _jumpStartingY - y));
                        }
                        else if (_bumpState == 0)
                        {
                            StartBump(gridPositionX, gridPositionY);
                        }

                        if (_jumpTimer > JUMP_DURATION || _bumpState == 2)
                        {
                            _isJumping = false;
                            _isFalling = true;
                            _jumpTimer = 0;
                            _jumpStartingY = _mario.Position.Y;
                            _bumpState = 0;
                        }
                    }
                    else if (_isFalling)
                    {
                        _jumpTimer += deltaTime;
                        float t = JUMP_DURATION + _jumpTimer;
                        float y = -(1 / (JUMP_DURATION * JUMP_DURATION)) * t * t + (2 / JUMP_DURATION) * t;

                        y = (y - 1) * MARIO_JUMP_HEIGHT;
                        _mario.MoveTo(new Vector2(_mario.Position.X, _jumpStartingY - y));
                    }
                }
            }

            if (_currentMarioSpeed != 0)
            {
                _mario.LookTo(new Vector2(MathF.Sign(_currentMarioSpeed), 0));
            }

            _mario.SetSpeed(MathF.Abs(_currentMarioSpeed));
            _mario.Animate(deltaTime);
            _mario.Move(deltaTime);

            foreach (PlatformCharacter enemy in _enemies)
            {
                enemy.Update(deltaTime, _level);
                if (enemy.PixelPositionY >= 208)
                {
                    if (enemy.MoveDirection.X < 0 && enemy.PixelPositionX - enemy.SpriteSheet.LeftMargin <= 32
                        || enemy.MoveDirection.X > 0 && enemy.PixelPositionX + enemy.SpriteSheet.RightMargin > SCREEN_WIDTH - 32)
                    {
                        enemy.Exit();
                    }
                }
            }


            base.Update(gameTime);
        }

        private void StartBump(int x, int y)
        {
            _bumpState = 1;
            _bumpCurrentFrame = 0;
            _bumpX = x;
            _bumpY = y;
        }

        private void UpdateBump(float deltaTime)
        {
            if (_bumpState == 1)
            {
                _bumpCurrentFrame += deltaTime * 20;
                if (_bumpCurrentFrame >= _bumpTiles.FrameCount / 3)
                {
                    _bumpState = 2;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            DrawLevel(_spriteBatch, deltaTime);

            foreach (PlatformCharacter enemy in _enemies)
            {
                enemy.Draw(_spriteBatch);
            }

            _spriteBatch.Draw(_levelOverlay, new Rectangle(0, 0, _levelOverlay.Width, _levelOverlay.Height), Color.White);

            _mario.Draw(_spriteBatch);
            _mario.Draw(_spriteBatch, SCREEN_WIDTH, 0);
            _mario.Draw(_spriteBatch, -SCREEN_WIDTH, 0);

            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_renderTarget, new Rectangle((int)MathF.Floor(CameraShake.ShakeOffset.X), (int)MathF.Floor(CameraShake.ShakeOffset.Y), SCREEN_WIDTH * SCREEN_SCALE, SCREEN_HEIGHT * SCREEN_SCALE), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawLevel(SpriteBatch spriteBatch, float deltaTime)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 30; y++)
                {
                    if (_level[x, y])
                    {
                        if (_bumpState == 1 && y == _bumpY && x >= _bumpX - 1 && x <= _bumpX + 1)
                        {
                            if (x == _bumpX - 1)
                                _bumpTiles.DrawFrame((int)MathF.Floor(_bumpCurrentFrame) * 3, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
                            else if (x == _bumpX)
                                _bumpTiles.DrawFrame((int)MathF.Floor(_bumpCurrentFrame) * 3 + 1, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
                            else if (x == _bumpX + 1)
                                _bumpTiles.DrawFrame((int)MathF.Floor(_bumpCurrentFrame) * 3  + 2, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
                        }
                        else
                        {
                            _tiles.DrawFrame(0, spriteBatch, new Vector2(x * 8, y * 8), 0, Vector2.One, Color.White);
                        }
                    }
                }
            }
        }


        private void SpawnTurtle(int side)
        {
            PlatformCharacter newTurtle = new PlatformCharacter(_turtleSpriteSheet);

            int x = side > 0 ? ENEMY_RIGHT_SPAWN_X : ENEMY_LEFT_SPAWN_X;
            newTurtle.SetAnimation("Walk");
            newTurtle.MoveTo(new Vector2(x, ENEMY_SPAWN_Y));
            newTurtle.LookTo(new Vector2(-side, 0));
            newTurtle.SetSpeed(30f);

            _enemies.Add(newTurtle);
        }
    }
}