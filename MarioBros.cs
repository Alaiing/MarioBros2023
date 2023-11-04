using Microsoft.VisualBasic.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Oudidon;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace MarioBros2023
{
    public class MarioBros : Game
    {
        public const int SCREEN_WIDTH = 256;
        public const int SCREEN_HEIGHT = 224;
        private const int SCREEN_SCALE = 4;

        private const int ENEMY_SPAWN_Y = 44;
        private const int ENEMY_LEFT_SPAWN_X = 44;
        private const int ENEMY_RIGHT_SPAWN_X = 212;

        private const int MARIO_COLLISION_WIDTH = 16;
        private const int MARIO_COLLISION_HEIGHT = 16;
        private const int ENEMY_COLLISION_WIDTH = 8;
        private const int ENEMY_COLLISION_HEIGHT = 8;
        private const int BETWEEN_ENEMY_COLLISION_WIDTH = 12;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private RenderTarget2D _renderTarget;

        private Random _random;

        private SpriteSheet _marioSpriteSheet;
        private Character _mario;

        private SpriteSheet _turtleSpriteSheet;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private SpriteSheet _splashSpriteSheet;
        private float _splashCurrentFrame;
        private float _splashX;

        private SpriteSheet _respawnPlatform;
        private float _respawnPlatformCurrentFrame;
        private float _respawnPlatformY;
        private float _respawnPlatformTimer;
        private bool IsRespawnPlatformReady => _respawnPlatformTimer >= RESPAWN_PLATFORM_APPEARANCE_DURATION;
        private const float RESPAWN_PLATFORM_APPEARANCE_DURATION = 2f;
        private const float RESPAWN_PLATFORM_DURATION = 12f;
        private const float RESPAWN_PLATFORM_START_Y = 10f;
        private const float RESPAWN_PLATFORM_Y = 40f;
        private const float RESPAWN_PLATFORM_X = 108f;

        private float _marioMaxSpeed = 75f;
        public float _marioJumpDuration = 0.5f;
        public int _marioJumpHeight = 55;
        private float _jumpTimer = 0;
        private float _currentMarioSpeed;
        private float _acceleration = 400f;
        private bool _isJumping = false;
        private bool _isFalling = false;
        private float _jumpStartingY;

        private bool _marioIsDying;
        private float _dyingTimer;
        private bool _marioIsRespawning;

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
            _marioJumpDuration = ConfigManager.GetConfig("MARIO_JUMP_DURATION", 0.5f);
            _marioJumpHeight = ConfigManager.GetConfig("MARIO_JUMP_HEIGHT", 55);
            _marioMaxSpeed = ConfigManager.GetConfig("MARIO_MAX_SPEED", 75f);
            _acceleration = ConfigManager.GetConfig("MARIO_ACCELERATION", 400f);

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

            _splashSpriteSheet = new SpriteSheet(Content, "splash", 16, 16, 8, 16);
            _turtleSpriteSheet.RegisterAnimation("Splash", 0, 2, 1f / 3f);
            _splashCurrentFrame = -1f;

            _respawnPlatform = new SpriteSheet(Content, "respawn", 16, 8);
            _respawnPlatformCurrentFrame = -1f;

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
            _enemies[0].name = "Turtle 1";
            SpawnTurtle(-1);
            _enemies[1].name = "Turtle 2";
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SimpleControls.GetStates();

            UpdateBump(deltaTime);
            UpdateSplash(deltaTime);
            UpdatePlatform(deltaTime);

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
                if (!isOnPlatform && !_marioIsDying && !_marioIsRespawning)
                {
                    _isFalling = true;
                    _jumpTimer = 0;
                    _jumpStartingY = _mario.Position.Y;
                    _mario.SetAnimationSpeed(0);
                }
                else
                {
                    if (!_marioIsDying)
                    {
                        if (!_marioIsRespawning || IsRespawnPlatformReady)
                        {
                            if (SimpleControls.IsLeftDown(SimpleControls.PlayerNumber.Player1))
                            {
                                hasInput = true;
                                _currentMarioSpeed += -_acceleration * deltaTime;
                                if (MathF.Abs(_currentMarioSpeed) >= _marioMaxSpeed)
                                {
                                    _currentMarioSpeed = -_marioMaxSpeed;
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
                                if (MathF.Abs(_currentMarioSpeed) >= _marioMaxSpeed)
                                {
                                    _currentMarioSpeed = _marioMaxSpeed;
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
                            else if (_marioIsRespawning)
                            {
                                ClearPlatform();
                            }
                        }
                    }
                    else
                    {
                        _dyingTimer += deltaTime;
                        if (_dyingTimer > 1f)
                        {
                            _isJumping = true;
                            _jumpTimer = 0;
                            _marioJumpHeight = 15;
                            _marioJumpDuration = 0.25f;
                            _jumpStartingY = _mario.Position.Y;
                            _mario.SetAnimation("Death");
                        }
                    }
                }
            }
            else
            {
                if (isOnPlatform && !_marioIsDying)
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

                        float y = MathUtils.NormalizedParabolicPosition(_jumpTimer / (2 * _marioJumpDuration)) * _marioJumpHeight;

                        if (!isUnderPlatform || _marioIsDying)
                        {
                            _mario.MoveTo(new Vector2(_mario.Position.X, _jumpStartingY - y));
                        }
                        else
                        {
                            StartBump(gridPositionX, gridPositionY);
                        }

                        if (_jumpTimer > _marioJumpDuration || _bumpState == 2)
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
                        float y = MathUtils.NormalizedParabolicPosition((_marioJumpDuration + _jumpTimer) / (2 * _marioJumpDuration));

                        y = _jumpStartingY - (y - 1) * _marioJumpHeight;

                        if (y >= SCREEN_HEIGHT)
                        {
                            Splash(_mario.PixelPositionX);
                            // respawn;
                            Respawn();
                        }
                        else
                        {
                            _mario.MoveTo(new Vector2(_mario.Position.X, y));
                        }
                    }
                }
            }

            if (_currentMarioSpeed != 0)
            {
                _mario.LookTo(new Vector2(MathF.Sign(_currentMarioSpeed), 0));
            }

            _mario.SetBaseSpeed(MathF.Abs(_currentMarioSpeed));
            _mario.Animate(deltaTime);
            _mario.Move(deltaTime);

            foreach (Enemy enemy in _enemies)
            {
                enemy.Update(deltaTime, _level);
            }

            // Test enemy contact
            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                if (enemy.IsDead)
                {
                    Splash(enemy.PixelPositionX);
                    _enemies.Remove(enemy);
                }
                else
                {
                    if (!enemy.IsDying && !enemy.IsEntering && ! enemy.IsExiting)
                    {
                        int relativeYPosition = _mario.PixelPositionY - enemy.PixelPositionY;
                        if (Math.Abs(_mario.PixelPositionX - enemy.PixelPositionX) < MARIO_COLLISION_WIDTH / 2 + ENEMY_COLLISION_WIDTH / 2
                            && (relativeYPosition >= 0 && relativeYPosition < MARIO_COLLISION_HEIGHT || relativeYPosition < 0 && -relativeYPosition < ENEMY_COLLISION_HEIGHT))
                        {
                            if (enemy.IsFlipped)
                            {
                                enemy.Kill(MathF.Sign(enemy.PixelPositionX - _mario.PixelPositionX));
                            }
                            else if (!_marioIsDying)
                            {
                                KillMario();
                            }
                        }

                        foreach(Enemy otherEnemy in _enemies)
                        {
                            if (!enemy.IsFalling && !enemy.IsFlipped && otherEnemy != enemy && otherEnemy.PixelPositionY == enemy.PixelPositionY)
                            {
                                int otherEnemyX = otherEnemy.PixelPositionX;
                                if (enemy.PixelPositionX > SCREEN_WIDTH - BETWEEN_ENEMY_COLLISION_WIDTH / 2 && enemy.MoveDirection.X > 0)
                                {
                                    otherEnemyX += SCREEN_WIDTH;
                                }
                                else if (enemy.PixelPositionX < BETWEEN_ENEMY_COLLISION_WIDTH/ 2 && enemy.MoveDirection.X < 0)
                                {
                                    otherEnemyX -= SCREEN_WIDTH;
                                }

                                int relativePositionX = otherEnemyX - enemy.PixelPositionX;
                                if (relativePositionX != 0  && Math.Abs(relativePositionX) < BETWEEN_ENEMY_COLLISION_WIDTH && MathF.Sign(relativePositionX) == MathF.Sign(enemy.MoveDirection.X))
                                {
                                    enemy.LookTo(-enemy.MoveDirection);
                                    enemy.SetSpeed(0);
                                    enemy.SetAnimation("Turn", onAnimationEnd: () =>
                                    {
                                        enemy.SetSpeed(1f);
                                        enemy.SetAnimation("Walk");
                                    });
                                }
                            }

                        }
                    }
                }
            }

            base.Update(gameTime);
        }

        private void KillMario()
        {
            // Arrêter mario
            _marioIsDying = true;
            _isJumping = false;
            _isFalling = false;
            _dyingTimer = 0;
            _mario.SetSpeed(0f);
            _mario.SetAnimation("Hit");
        }

        private void Respawn()
        {
            _respawnPlatformCurrentFrame = 0;
            _respawnPlatformY = RESPAWN_PLATFORM_START_Y;
            _respawnPlatformTimer = 0;

            _marioIsDying = false;
            _isFalling = false;
            _marioIsRespawning = true;
            _currentMarioSpeed = 0;
            _marioJumpHeight = ConfigManager.GetConfig("MARIO_JUMP_HEIGHT", 55);
            _marioJumpDuration = ConfigManager.GetConfig("MARIO_JUMP_DURATION", 0.5f);
            _mario.MoveTo(new Vector2(RESPAWN_PLATFORM_X + 8, RESPAWN_PLATFORM_START_Y));
            _mario.LookTo(new Vector2(1, 0));
            _mario.SetSpeed(1f);
            _mario.SetAnimation("Idle");

            // faire descendre la plateforme
            // si le joueur touche une commande, la plateforme disparaît
            // un bout d'un certain temps, la plateforme disparaît toute seule
        }

        private void UpdatePlatform(float deltaTime)
        {
            if (_respawnPlatformCurrentFrame >= 0)
            {
                _respawnPlatformTimer += deltaTime;
                if (_respawnPlatformTimer < RESPAWN_PLATFORM_APPEARANCE_DURATION)
                {
                    _respawnPlatformY = MathHelper.Lerp(RESPAWN_PLATFORM_START_Y, RESPAWN_PLATFORM_Y, _respawnPlatformTimer / 2f);
                    _mario.MoveTo(new Vector2(RESPAWN_PLATFORM_X + 8, _respawnPlatformY));
                }
                else
                {
                    float platformTime = _respawnPlatformTimer - RESPAWN_PLATFORM_APPEARANCE_DURATION;
                    if (platformTime >= RESPAWN_PLATFORM_DURATION)
                    {
                        ClearPlatform();
                    }
                    else
                    {
                        _respawnPlatformCurrentFrame = platformTime / (RESPAWN_PLATFORM_DURATION / 3);
                    }
                }
            }
        }

        private void ClearPlatform()
        {
            _respawnPlatformCurrentFrame = -1f;
            _marioIsRespawning = false;
        }

        private void StartBump(int x, int y)
        {
            if (_bumpState == 0)
            {
                _bumpState = 1;
                _bumpCurrentFrame = 0;
                _bumpX = x;
                _bumpY = y;
                BumpEnemy(_mario.PixelPositionX, y);
            }
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

        private void UpdateSplash(float deltaTime)
        {
            if (_splashCurrentFrame >= 0)
            {
                _splashCurrentFrame += deltaTime * 10;
                if (_splashCurrentFrame >= _splashSpriteSheet.FrameCount)
                {
                    _splashCurrentFrame = -1;
                }
            }
        }

        protected void BumpEnemy(int positionX, int gridY)
        {
            Debug.WriteLine("BUMP");
            foreach (Enemy enemy in _enemies)
            {
                int gridPositionY = enemy.PixelPositionY / 8;
                if (gridPositionY == gridY)
                {
                    if (positionX <= enemy.PixelPositionX - 4 && positionX >= enemy.PixelPositionX - 12)
                    {
                        enemy.Bump(1);
                    }
                    if (positionX >= enemy.PixelPositionX + 4 && positionX <= enemy.PixelPositionX + 12)
                    {
                        enemy.Bump(-1);
                    }
                    if (positionX >= enemy.PixelPositionX - 4 && positionX <= enemy.PixelPositionX + 4)
                    {
                        enemy.Bump(0);
                    }
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

            foreach (Enemy enemy in _enemies)
            {
                enemy.Draw(_spriteBatch);
            }

            _spriteBatch.Draw(_levelOverlay, new Rectangle(0, 0, _levelOverlay.Width, _levelOverlay.Height), Color.White);

            if (_respawnPlatformCurrentFrame >= 0)
            {
                _respawnPlatform.DrawFrame((int)MathF.Floor(_respawnPlatformCurrentFrame), _spriteBatch, new Vector2(108, _respawnPlatformY), 0, Vector2.One, Color.White);
            }

            _mario.Draw(_spriteBatch);
            _mario.Draw(_spriteBatch, SCREEN_WIDTH, 0);
            _mario.Draw(_spriteBatch, -SCREEN_WIDTH, 0);

            DrawSplash(_spriteBatch);

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
                for (int y = 0; y < 26; y++)
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
                                _bumpTiles.DrawFrame((int)MathF.Floor(_bumpCurrentFrame) * 3 + 2, spriteBatch, new Vector2(x * 8, (y - 1) * 8), 0, Vector2.One, Color.White);
                        }
                        else
                        {
                            _tiles.DrawFrame(0, spriteBatch, new Vector2(x * 8, y * 8), 0, Vector2.One, Color.White);
                        }
                    }
                }
            }
        }

        private void DrawSplash(SpriteBatch spriteBatch)
        {
            if (_splashCurrentFrame >= 0)
            {
                _splashSpriteSheet.DrawFrame((int)MathF.Floor(_splashCurrentFrame), spriteBatch, new Vector2(_splashX, SCREEN_HEIGHT), 0, Vector2.One, Color.White);
            }
        }

        private void SpawnTurtle(int side)
        {
            Enemy newTurtle = new Enemy(_turtleSpriteSheet, 0.25f, 15f);

            int x = side > 0 ? ENEMY_RIGHT_SPAWN_X : ENEMY_LEFT_SPAWN_X;
            newTurtle.SetAnimation("Walk");
            newTurtle.MoveTo(new Vector2(x, ENEMY_SPAWN_Y));
            newTurtle.LookTo(new Vector2(-side, 0));
            newTurtle.SetBaseSpeed(30f);

            _enemies.Add(newTurtle);
        }

        private void Splash(int x)
        {
            _splashX = x;
            _splashCurrentFrame = 0;
        }
    }
}