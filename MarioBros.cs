using Microsoft.VisualBasic.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
        private Player _mario;

        private SpriteSheet _turtleSpriteSheet;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private SpriteSheet _splashSpriteSheet;
        private float _splashCurrentFrame;
        private float _splashX;

        private SpriteSheet _respawnPlatform;
        private float _respawnPlatformCurrentFrame;
        private float _respawnPlatformY;
        private float _respawnPlatformTimer;
        private const float RESPAWN_PLATFORM_APPEARANCE_DURATION = 2f;
        private const float RESPAWN_PLATFORM_DURATION = 12f;
        private const float RESPAWN_PLATFORM_START_Y = 10f;
        private const float RESPAWN_PLATFORM_Y = 40f;
        private const float RESPAWN_PLATFORM_X = 108f;

        private bool[,] _level;
        private SpriteSheet _tiles;
        private SpriteSheet _bumpTiles;
        private Texture2D _levelOverlay;

        private readonly List<Bump> _bumps = new List<Bump>();

        private Texture2D _titleSprite;

        private Texture2D _marioHeadSprite;
        private Texture2D _gameOverSprite;

        private SimpleStateMachine _gameStateMachine;
        private const string STATE_TITLE = "Title";
        private const string STATE_GAME = "Game";
        private const string STATE_LEVEL_START = "LevelStart";
        private const string STATE_GAME_OVER = "GameOver";
        private const string STATE_LEVEL_CLEARED = "LevelCleared";

        private float _stateChangeTimer;

        private readonly List<Level> _levels = new List<Level>();
        private int _currentLevelIndex;
        private Level _currentLevel;
        private int _killedEnemies;

        private SoundEffect[] _pootSteps;
        private SoundEffect _skid;
        private SoundEffect _jump;
        private SoundEffect _hit;
        private SoundEffect _death;

        private SoundEffect _splash;

        private SoundEffect _startGameJingle;
        private SoundEffect _startLevelJingle;
        private SoundEffect _currentJingle;

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

            EventsManager.ListenTo<PlatformCharacter>("BUMP", StartBump);
            EventsManager.ListenTo<PlatformCharacter>("DEATH", OnCharacterDeath);
            EventsManager.ListenTo<Enemy.EnemyType>("SPAWN_ENEMY", OnEnemySpawn);

            _gameStateMachine = new SimpleStateMachine();
            _gameStateMachine.AddState(STATE_TITLE, OnEnter: null, OnExit: null, OnUpdate: TitleUpdate);
            _gameStateMachine.AddState(STATE_LEVEL_START, OnEnter: LevelStartEnter, OnExit: LevelStartEnd, OnUpdate: LevelStartUpdate);
            _gameStateMachine.AddState(STATE_GAME, OnEnter: null, OnExit: null, OnUpdate: GameplayUpdate);
            _gameStateMachine.AddState(STATE_GAME_OVER, OnEnter: () => _stateChangeTimer = 0, OnExit: null, OnUpdate: GameOverUpdate);
            _gameStateMachine.AddState(STATE_LEVEL_CLEARED, OnEnter: () => _stateChangeTimer = 0, OnExit: null, OnUpdate: LevelClearedUpdate);
            _gameStateMachine.SetState(STATE_TITLE);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            ConfigManager.LoadConfig("config.ini");

            _pootSteps = new SoundEffect[8];
            _pootSteps[0] = Content.Load<SoundEffect>("pout1");
            _pootSteps[1] = Content.Load<SoundEffect>("pout2");
            _pootSteps[2] = Content.Load<SoundEffect>("pout3");
            _pootSteps[3] = _pootSteps[1];
            _pootSteps[4] = _pootSteps[2];
            _pootSteps[5] = _pootSteps[1];
            _pootSteps[6] = _pootSteps[2];
            _pootSteps[7] = Content.Load<SoundEffect>("pout4");
            _skid = Content.Load<SoundEffect>("criii");
            _jump = Content.Load<SoundEffect>("zboui");
            _hit = Content.Load<SoundEffect>("huii");
            _death = Content.Load<SoundEffect>("flbflbflb");

            _splash = Content.Load<SoundEffect>("plouf");

            _startGameJingle = Content.Load<SoundEffect>("tadoudi");
            _startLevelJingle = Content.Load<SoundEffect>("doudidou");

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
            _gameOverSprite = Content.Load<Texture2D>("game-over");

            _marioHeadSprite = Content.Load<Texture2D>("mario-head");

            _mario = new Player(_marioSpriteSheet, _level, 2, _pootSteps, _skid, _jump, _hit, _death);
            _mario.CanBump = true;

            _titleSprite = Content.Load<Texture2D>("title-screen");

            _levels.Add(Level.CreateLevel(0));
            _levels.Add(Level.CreateLevel(1));
        }

        private void StartGame()
        {
            _mario.ResetLives(2);
            _currentLevelIndex = 0;
            _currentJingle = _startGameJingle;
            _gameStateMachine.SetState(STATE_LEVEL_START);
        }

        private void StartLevel()
        {
            _currentLevel = _levels[_currentLevelIndex];
            _currentLevel.ResetSpawn();
            _mario.ResetState();
            _mario.MoveTo(new Vector2(68, 208));
            _mario.Walk();
            _enemies.Clear();

            _killedEnemies = 0;
            _currentJingle.Play();
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SimpleControls.GetStates();

            _gameStateMachine.Update(deltaTime);

            base.Update(gameTime);
        }

        private void TitleUpdate(float deltaTime)
        {
            SimpleControls.GetStates();
            if (SimpleControls.IsStartDown())
            {
                StartGame();
                //GameplayUpdate(deltaTime);
            }
        }

        private void GameplayUpdate(float deltaTime)
        {
            UpdateBumps(deltaTime);
            UpdateSplash(deltaTime);
            UpdatePlatform(deltaTime);
            _currentLevel.Update(deltaTime);

            _mario.Update(deltaTime);

            for (int i = 0; i < _enemies.Count; i++)
            {
                Enemy enemy = _enemies[i];
                enemy.Update(deltaTime);
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
                    if (!enemy.IsDying && !enemy.IsEntering && !enemy.IsExiting)
                    {
                        int relativeYPosition = _mario.PixelPositionY - enemy.PixelPositionY;
                        if (Math.Abs(_mario.PixelPositionX - enemy.PixelPositionX) < MARIO_COLLISION_WIDTH / 2 + ENEMY_COLLISION_WIDTH / 2
                            && (relativeYPosition >= 0 && relativeYPosition < MARIO_COLLISION_HEIGHT || relativeYPosition < 0 && -relativeYPosition < ENEMY_COLLISION_HEIGHT))
                        {
                            if (enemy.IsFlipped)
                            {
                                enemy.Kill(MathF.Sign(enemy.PixelPositionX - _mario.PixelPositionX));
                            }
                            else if (!_mario.IsDying)
                            {
                                KillMario();
                            }
                        }

                        foreach (Enemy otherEnemy in _enemies)
                        {
                            if (!enemy.IsFalling && !enemy.IsFlipped && otherEnemy != enemy && otherEnemy.PixelPositionY == enemy.PixelPositionY)
                            {
                                int otherEnemyX = otherEnemy.PixelPositionX;
                                if (enemy.PixelPositionX > SCREEN_WIDTH - BETWEEN_ENEMY_COLLISION_WIDTH / 2 && enemy.MoveDirection.X > 0)
                                {
                                    otherEnemyX += SCREEN_WIDTH;
                                }
                                else if (enemy.PixelPositionX < BETWEEN_ENEMY_COLLISION_WIDTH / 2 && enemy.MoveDirection.X < 0)
                                {
                                    otherEnemyX -= SCREEN_WIDTH;
                                }

                                int relativePositionX = otherEnemyX - enemy.PixelPositionX;
                                if (relativePositionX != 0 && Math.Abs(relativePositionX) < BETWEEN_ENEMY_COLLISION_WIDTH && MathF.Sign(relativePositionX) == MathF.Sign(enemy.MoveDirection.X))
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
        }

        private void GameOverUpdate(float deltaTime)
        {
            _stateChangeTimer += deltaTime;
            if (_stateChangeTimer > 5f)
            {
                _gameStateMachine.SetState(STATE_TITLE);
            }
        }

        private float _levelStartTimer;
        private void LevelStartEnter()
        {
            _levelStartTimer = 0;
            StartLevel();
        }

        private void LevelStartUpdate(float deltaTime)
        {
            _levelStartTimer += deltaTime;
            if (_levelStartTimer > _currentJingle.Duration.TotalSeconds)
            {
                _gameStateMachine.SetState(STATE_GAME);
            }
        }

        private void LevelStartEnd()
        {
            _currentJingle = _startLevelJingle;
        }

        private void LevelClearedUpdate(float deltaTime)
        {
            _stateChangeTimer += deltaTime;
            if (_stateChangeTimer > 5f)
            {
                _currentLevelIndex++;
                if (_currentLevelIndex < _levels.Count)
                {
                    _gameStateMachine.SetState(STATE_LEVEL_START);
                }
                else
                {
                    _gameStateMachine.SetState(STATE_GAME_OVER);
                }
            }
        }

        private void KillMario()
        {
            _mario.Kill();
        }

        private void OnCharacterDeath(PlatformCharacter character)
        {
            Splash(character.PixelPositionX);
            if (character is Enemy enemy)
            {
                _enemies.Remove(enemy);
                _killedEnemies++;
                if (_killedEnemies == _currentLevel.EnemyCount)
                {
                    _gameStateMachine.SetState(STATE_LEVEL_CLEARED);
                }
            }
            else if (character is Player player)
            {
                if (player.LivesLeft >= 0)
                {
                    Respawn();
                }
                else
                {
                    GameOver();
                }
            }
        }

        private void GameOver()
        {
            _gameStateMachine.SetState(STATE_GAME_OVER);
        }

        private void Respawn()
        {
            _respawnPlatformCurrentFrame = 0;
            _respawnPlatformY = RESPAWN_PLATFORM_START_Y;
            _respawnPlatformTimer = 0;

            _mario.MoveTo(new Vector2(RESPAWN_PLATFORM_X + 8, RESPAWN_PLATFORM_START_Y));
            _mario.LookTo(new Vector2(1, 0));
            _mario.Respawn();
        }

        private void UpdatePlatform(float deltaTime)
        {
            if (_respawnPlatformCurrentFrame >= 0)
            {
                if (_mario.IsMoving)
                {
                    ClearPlatform();
                    return;
                }

                _respawnPlatformTimer += deltaTime;
                if (_respawnPlatformTimer <= RESPAWN_PLATFORM_APPEARANCE_DURATION)
                {
                    _respawnPlatformY = MathHelper.Lerp(RESPAWN_PLATFORM_START_Y, RESPAWN_PLATFORM_Y, _respawnPlatformTimer / 2f);
                    _mario.MoveTo(new Vector2(RESPAWN_PLATFORM_X + 8, _respawnPlatformY));
                }
                else
                {
                    _mario.Walk();
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
            _mario.IgnorePlatforms = false;
        }

        private void StartBump(PlatformCharacter character)
        {
            _bumps.Add(new Bump(_bumpTiles, _currentLevel.TileFrame, character));
            BumpEnemy(character.PixelPositionX, (character.PixelPositionY - 20) / 8);
        }

        private void UpdateBumps(float deltaTime)
        {
            for (int i = 0; i < _bumps.Count; i++)
            {
                _bumps[i].Update(deltaTime);
                if (!_bumps[i].Enabled)
                {
                    _bumps.Remove(_bumps[i]);
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

            switch (_gameStateMachine.CurrentState)
            {
                case STATE_TITLE:
                    TitleDraw();
                    break;
                case STATE_LEVEL_START:
                case STATE_GAME:
                case STATE_LEVEL_CLEARED:
                    GameplayDraw(deltaTime);
                    break;
                case STATE_GAME_OVER:
                    DrawLevel(_spriteBatch, deltaTime);
                    _spriteBatch.Draw(_levelOverlay, new Rectangle(0, 0, _levelOverlay.Width, _levelOverlay.Height), Color.White);
                    _spriteBatch.Draw(_gameOverSprite, new Vector2(96, 88), Color.White);
                    break;
            }
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
            _spriteBatch.Draw(_renderTarget, new Rectangle((int)MathF.Floor(CameraShake.ShakeOffset.X), (int)MathF.Floor(CameraShake.ShakeOffset.Y), SCREEN_WIDTH * SCREEN_SCALE, SCREEN_HEIGHT * SCREEN_SCALE), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void TitleDraw()
        {
            _spriteBatch.Draw(_titleSprite, Vector2.Zero, Color.White);
        }

        private void GameplayDraw(float deltaTime)
        {
            for (int i = 0; i < _mario.LivesLeft; i++)
            {
                _spriteBatch.Draw(_marioHeadSprite, new Vector2(64 + i * 12, 25), Color.White);
            }

            DrawLevel(_spriteBatch, deltaTime);

            foreach (Enemy enemy in _enemies)
            {
                if (enemy.IsExiting || enemy.IsEntering)
                {
                    enemy.Draw(_spriteBatch);
                }
            }

            _spriteBatch.Draw(_levelOverlay, new Rectangle(0, 0, _levelOverlay.Width, _levelOverlay.Height), Color.White);

            foreach (Enemy enemy in _enemies)
            {
                if (!(enemy.IsExiting || enemy.IsEntering))
                {
                    enemy.Draw(_spriteBatch);
                }
            }


            if (_respawnPlatformCurrentFrame >= 0)
            {
                _respawnPlatform.DrawFrame((int)MathF.Floor(_respawnPlatformCurrentFrame), _spriteBatch, new Vector2(108, _respawnPlatformY), 0, Vector2.One, Color.White);
            }

            _mario.Draw(_spriteBatch);
            _mario.Draw(_spriteBatch, SCREEN_WIDTH, 0);
            _mario.Draw(_spriteBatch, -SCREEN_WIDTH, 0);

            DrawSplash(_spriteBatch);
        }

        private void DrawLevel(SpriteBatch spriteBatch, float deltaTime)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 26; y++)
                {
                    if (_level[x, y])
                    {
                        bool bumped = false;
                        foreach (Bump bump in _bumps)
                        {
                            if (bump.IsBumped(x, y))
                            {
                                bump.Draw(spriteBatch, x, y);
                                bumped = true;
                            }
                        }

                        if (!bumped)
                        {
                            _tiles.DrawFrame(_currentLevel.TileFrame, spriteBatch, new Vector2(x * 8, y * 8), 0, Vector2.One, Color.White);
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

        private void OnEnemySpawn(Enemy.EnemyType type)
        {
            switch (type)
            {
                default:
                case Enemy.EnemyType.Turtle:
                    SpawnTurtle(_random.Next(0, 2) * 2 - 1);
                    break;
            }
        }

        private void SpawnTurtle(int side)
        {
            Enemy newTurtle = new Enemy(_turtleSpriteSheet, _level);

            newTurtle.SetBaseSpeed(ConfigManager.GetConfig("TURTLE_SPEED", 25f));
            newTurtle.Enter(side);
            _enemies.Add(newTurtle);
        }

        private void Splash(int x)
        {
            _splashX = x;
            _splashCurrentFrame = 0;
            _splash.Play();
        }
    }
}