using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
        private Color _marioScoreColor = new Color(0.282f, 0.804f, 0.871f);

        private int _highscore;

        private SpriteSheet _turtleSpriteSheet;
        private SpriteSheet _crabSpriteSheet;
        private readonly List<Enemy> _enemies = new List<Enemy>();

        private SpriteSheet _redFireballSpriteSheet;
        private SpriteSheet _greenFireballSpriteSheet;

        private SpriteSheet _coinSpriteSheet;

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

        public enum LevelTile { Empty, Tile, Pow }
        private LevelTile[,] _level;
        private SpriteSheet _tiles;
        private SpriteSheet _bumpTiles;
        private Texture2D _levelOverlay;

        private readonly List<Bump> _bumps = new List<Bump>();

        private Texture2D _titleSprite;

        private Texture2D _marioHeadSprite;
        private Texture2D _gameOverSprite;

        private SpriteSheet _powSprite;

        private SpriteSheet _digits;
        private SpriteSheet _scoreTitles;
        private SpriteSheet _scorePopup;

        private SimpleStateMachine _gameStateMachine;
        private const string STATE_TITLE = "Title";
        private const string STATE_GAME = "Game";
        private const string STATE_LEVEL_START = "LevelStart";
        private const string STATE_GAME_OVER = "GameOver";
        private const string STATE_LEVEL_CLEARED = "LevelCleared";
        private const string STATE_BONUS_LEVEL_COUNT = "BonusLevelCount";

        private float _stateChangeTimer;

        private readonly List<Level> _levels = new List<Level>();
        private int _currentLevelIndex;
        private Level _currentLevel;
        private int _killedEnemies;
        private int _bonusCoinsCollected;
        private int _powLeft;

        private SoundEffect[] _pootSteps;
        private SoundEffect _skidSounds;
        private SoundEffect _jumpSounds;
        private SoundEffect _hitSound;
        private SoundEffect _deathSound;
        private SoundEffect _respawnSound;

        private SoundEffect _splash;

        private SoundEffect _titleMusic;
        private SoundEffectInstance _titleMusicInstance;
        private SoundEffect _startGameJingle;
        private SoundEffect _startLevelJingle;
        private SoundEffect _currentJingle;
        private SoundEffect _endLevelJingle;
        private SoundEffect _gameOverJingle;

        private SoundEffect _enemySpawnSpound;
        private SoundEffect _enemyFlip;
        private SoundEffect _enemyDie;

        private SoundEffect _powSound;

        private SoundEffect _redFireballSound;
        private SoundEffect _greenFireballSound;
        private SoundEffectInstance _greenFireballSoundInstance;
        private SoundEffect _greenFireballSpawnSound;

        private SoundEffect _coinSpawnSound;
        private SoundEffect _coinCollectSound;

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
            EventsManager.ListenTo<Enemy>("ENEMY_DYING", OnEnemyDying);
            EventsManager.ListenTo<Coin>("COIN_COLLECTED", OnCoinCollected);

            _gameStateMachine = new SimpleStateMachine();
            _gameStateMachine.AddState(STATE_TITLE, OnEnter: () => _titleMusicInstance.Play(), OnExit: () => _titleMusicInstance.Stop(), OnUpdate: TitleUpdate);
            _gameStateMachine.AddState(STATE_LEVEL_START, OnEnter: LevelStartEnter, OnExit: LevelStartEnd, OnUpdate: LevelStartUpdate);
            _gameStateMachine.AddState(STATE_GAME, OnEnter: null, OnExit: null, OnUpdate: GameplayUpdate);
            _gameStateMachine.AddState(STATE_GAME_OVER, OnEnter: GameOverEnter, OnExit: null, OnUpdate: GameOverUpdate);
            _gameStateMachine.AddState(STATE_LEVEL_CLEARED, OnEnter: () => _stateChangeTimer = 0, OnExit: null, OnUpdate: LevelClearedUpdate);
            _gameStateMachine.AddState(STATE_BONUS_LEVEL_COUNT, OnEnter: null, OnExit: null, OnUpdate: null);

            CameraShake.Enabled = true;

            base.Initialize();
        }

        private void OnCoinCollected(Coin coin)
        {
            if (_currentLevel.IsBonusLevel)
            {
                _bonusCoinsCollected++;
                if (_bonusCoinsCollected == 10)
                {
                    _gameStateMachine.SetState(STATE_LEVEL_CLEARED);
                }
            }
            else
            {
                IncreaseScore(_mario, ConfigManager.GetConfig("COIN_SCORE", 800), coin.InitialPosition);
            }
        }

        private void OnEnemyDying(Enemy enemy)
        {
            if (enemy is not Coin)
            {
                if (_killedEnemies == _currentLevel.EnemyCount - 1)
                {
                    _endLevelJingle.Play();
                }
                else
                {
                    _enemyDie.Play();
                    if (_killedEnemies == _currentLevel.EnemyCount - 2)
                    {
                        _enemies.Find(e => e != enemy).ToMaxPhase();
                    }

                }
            }
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
            _skidSounds = Content.Load<SoundEffect>("criii");
            _jumpSounds = Content.Load<SoundEffect>("zboui2");
            _hitSound = Content.Load<SoundEffect>("huii");
            _deathSound = Content.Load<SoundEffect>("flbflbflb");
            _respawnSound = Content.Load<SoundEffect>("tbltbltbltbl");

            _splash = Content.Load<SoundEffect>("plouf");

            _enemySpawnSpound = Content.Load<SoundEffect>("pehou");
            _enemyFlip = Content.Load<SoundEffect>("beuha");
            _enemyDie = Content.Load<SoundEffect>("grlgrlgrl");

            _titleMusic = Content.Load<SoundEffect>("title");
            _titleMusicInstance = _titleMusic.CreateInstance();
            _startGameJingle = Content.Load<SoundEffect>("tadoudi");
            _startLevelJingle = Content.Load<SoundEffect>("doudidou");
            _endLevelJingle = Content.Load<SoundEffect>("grlgrlgrlgrlgrlgrl");
            _gameOverJingle = Content.Load<SoundEffect>("dimdoum");

            _powSound = Content.Load<SoundEffect>("powpow");

            _redFireballSound = Content.Load<SoundEffect>("brruui");
            _greenFireballSound = Content.Load<SoundEffect>("bruibrui");
            _greenFireballSpawnSound = Content.Load<SoundEffect>("brrrrrr");
            _greenFireballSoundInstance = _greenFireballSound.CreateInstance();
            _greenFireballSoundInstance.IsLooped = true;

            _coinSpawnSound = Content.Load<SoundEffect>("peuing");
            _coinCollectSound = Content.Load<SoundEffect>("puddin");

            _marioSpriteSheet = new SpriteSheet(Content, "Mario", 16, 24, 8, 24);
            _marioSpriteSheet.RegisterAnimation("Idle", 0, 0, 0);
            _marioSpriteSheet.RegisterAnimation("Run", 1, 3, 30f);
            _marioSpriteSheet.RegisterAnimation("Jump", 4, 4, 0);
            _marioSpriteSheet.RegisterAnimation("Slip", 5, 5, 0);
            _marioSpriteSheet.RegisterAnimation("Hit", 6, 6, 0);
            _marioSpriteSheet.RegisterAnimation("Death", 7, 7, 0);
            _marioSpriteSheet.RegisterAnimation("Flatten", 8, 9, 1f);

            _turtleSpriteSheet = new SpriteSheet(Content, "turtle", 16, 16, 8, 16);
            _turtleSpriteSheet.RegisterAnimation("Run", 0, 3, 20f);
            _turtleSpriteSheet.RegisterAnimation("Turn", 4, 5, 4f);
            _turtleSpriteSheet.RegisterAnimation("OnBack", 6, 7, 1f);

            _crabSpriteSheet = new SpriteSheet(Content, "crab", 16, 16, 8, 16);
            _crabSpriteSheet.RegisterAnimation("Run", 0, 3, 20f);
            _crabSpriteSheet.RegisterAnimation("RunAngry", 4, 7, 20f);
            _crabSpriteSheet.RegisterAnimation("Turn", 8, 9, 4f);
            _crabSpriteSheet.RegisterAnimation("OnBack", 10, 11, 1f);

            _coinSpriteSheet = new SpriteSheet(Content, "coin", 16, 16, 8, 16);
            _coinSpriteSheet.RegisterAnimation("Rotate", 0, 4, 20f);
            _coinSpriteSheet.RegisterAnimation("Collect", 5, 9, 10f);

            _redFireballSpriteSheet = new SpriteSheet(Content, "fireball-red", 8, 8, 4, 4);
            _redFireballSpriteSheet.RegisterAnimation("Move", 0, 3, 20f);
            _redFireballSpriteSheet.RegisterAnimation("Disappear", 4, 8, 8f);

            _greenFireballSpriteSheet = new SpriteSheet(Content, "fireball-green", 8, 8, 4, 4);
            _greenFireballSpriteSheet.RegisterAnimation("Move", 0, 3, 50f);
            _greenFireballSpriteSheet.RegisterAnimation("Appear", 4, 6, 16f);
            _greenFireballSpriteSheet.RegisterAnimation("Disappear", 4, 8, 8f);

            _splashSpriteSheet = new SpriteSheet(Content, "splash", 16, 16, 8, 16);
            _turtleSpriteSheet.RegisterAnimation("Splash", 0, 2, 1f / 3f);
            _splashCurrentFrame = -1f;

            _respawnPlatform = new SpriteSheet(Content, "respawn", 16, 8);
            _respawnPlatformCurrentFrame = -1f;

            _digits = new SpriteSheet(Content, "digits", 7, 7);
            _scoreTitles = new SpriteSheet(Content, "prefixes", 20, 7, 20, 0);
            _scorePopup = new SpriteSheet(Content, "scores", 15, 7, 9, 7);

            _level = new LevelTile[32, 30];
            for (int i = 0; i < 12; i++)
            {
                _level[i, 20] = LevelTile.Tile;
                _level[31 - i, 20] = LevelTile.Tile;
            }
            for (int i = 0; i < 4; i++)
            {
                _level[i, 15] = LevelTile.Tile;
                _level[31 - i, 15] = LevelTile.Tile;
            }
            for (int i = 0; i < 16; i++)
            {
                _level[8 + i, 14] = LevelTile.Tile;
            }

            for (int i = 0; i < 14; i++)
            {
                _level[i, 8] = LevelTile.Tile;
                _level[31 - i, 8] = LevelTile.Tile;
            }
            for (int i = 0; i < 32; i++)
            {
                _level[i, 26] = LevelTile.Tile;
                _level[i, 27] = LevelTile.Tile;
            }
            _tiles = new SpriteSheet(Content, "tiles", 8, 8);
            _bumpTiles = new SpriteSheet(Content, "tile-bump", 8, 16);

            _levelOverlay = Content.Load<Texture2D>("overlay");
            _gameOverSprite = Content.Load<Texture2D>("game-over");

            _marioHeadSprite = Content.Load<Texture2D>("mario-head");

            _mario = new Player(_marioSpriteSheet, _level, 2, _pootSteps, _skidSounds, _jumpSounds, _hitSound, _deathSound);
            _mario.CanBump = true;

            _titleSprite = Content.Load<Texture2D>("title-screen");

            _powSprite = new SpriteSheet(Content, "pow", 16, 16);

            _levels.Add(Level.CreateLevel(0));
            _levels.Add(Level.CreateLevel(1));
            _levels.Add(Level.CreateLevel(2));
            _levels.Add(Level.CreateLevel(3));

            _gameStateMachine.SetState(STATE_TITLE);

        }

        private void StartGame()
        {
            _mario.ResetLives(2);
            _mario.ResetScore();
            _currentLevelIndex = 0;
            InitPOW();
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

            if (_currentLevel.IsBonusLevel)
            {
                _bonusCoinsCollected = 0;
                SpawnBonusLevelCoins();
            }

            _killedEnemies = 0;
            _redFireballSpawnTimer = 0;
            if (_redFireball != null)
                _redFireball.Visible = false;
            _greenFireballSpawned = false;
            _greenFireballTimer = 0;
            if (_greenFireball != null)
                _greenFireball.Visible = false;
            _currentJingle.Play();
        }

        protected override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            SimpleControls.GetStates();

            UpdateBumps(deltaTime);
            UpdateSplash(deltaTime);
            _gameStateMachine.Update(deltaTime);

            CameraShake.Update(deltaTime);

            base.Update(gameTime);
        }

        private void TitleUpdate(float deltaTime)
        {
            SimpleControls.GetStates();
            if (SimpleControls.IsStartDown())
            {
                StartGame();
            }
        }

        private void GameplayUpdate(float deltaTime)
        {
            UpdatePlatform(deltaTime);
            UpdatePopScores(deltaTime);
            UpdateRedFireball(deltaTime);
            UpdateGreenFireball(deltaTime);
            _currentLevel.Update(deltaTime);
            if (_currentLevel.IsBonusLevel && _currentLevel.LevelTimer >= 20)
            {
                _gameStateMachine.SetState(STATE_LEVEL_CLEARED);
                return;
            }

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
                            if (enemy is Coin || enemy.IsFlipped)
                            {
                                if (!enemy.IsDying)
                                {
                                    enemy.Kill(MathF.Sign(enemy.PixelPositionX - _mario.PixelPositionX));
                                    if (enemy is not Coin)
                                    {
                                        int combo = _mario.GetKillCombo();
                                        IncreaseScore(_mario, ConfigManager.GetConfig("ENEMY_SCORE", 800) + ConfigManager.GetConfig("COMBO_SCORE", 800) * combo, enemy.Position);
                                    }
                                }
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
                                    if (enemy is not Coin)
                                    {
                                        enemy.SetSpeed(0);
                                        enemy.SetAnimation("Turn", onAnimationEnd: () =>
                                        {
                                            enemy.SetSpeed(1f);
                                            enemy.SetAnimation(enemy.WalkAnimationName);
                                        });
                                    }
                                }
                            }

                        }
                    }
                }
            }
        }

        private void GameOverEnter()
        {
            _gameOverJingle.Play();
            _stateChangeTimer = 0;
        }
        private void GameOverUpdate(float deltaTime)
        {
            _stateChangeTimer += deltaTime;
            if (_stateChangeTimer > _gameOverJingle.Duration.TotalSeconds)
            {
                _gameStateMachine.SetState(STATE_TITLE);
            }
        }

        private float _levelStartTimer;
        private void LevelStartEnter()
        {
            _levelStartTimer = 0;
            _popScores.Clear();
            StartLevel();
        }

        private void LevelStartUpdate(float deltaTime)
        {
            _levelStartTimer += deltaTime;
            foreach(Enemy enemy in _enemies) 
            {
                enemy.Animate(deltaTime);
            }
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
            if (_stateChangeTimer > (_currentLevel.IsBonusLevel ? 5f : _endLevelJingle.Duration.TotalSeconds))
            {
                if (_currentLevel.IsBonusLevel)
                {
                    _gameStateMachine.SetState(STATE_BONUS_LEVEL_COUNT);
                }
                else
                {
                    _currentLevelIndex++;
                    if (_currentLevelIndex < _levels.Count)
                    {
                        if (_levels[_currentLevelIndex].IsBonusLevel)
                        {
                            StartLevel();
                            _startLevelJingle.Play();
                            _gameStateMachine.SetState(STATE_GAME);
                        }
                        else
                        {
                            _gameStateMachine.SetState(STATE_LEVEL_START);
                        }
                    }
                    else
                    {
                        _gameStateMachine.SetState(STATE_GAME_OVER);
                    }
                }
            }
        }

        private void KillMario()
        {
            _mario.Kill();
        }

        private void OnCharacterDeath(PlatformCharacter character)
        {
            if (character is Coin coin)
            {
                _enemies.Remove(coin);
                // TODO add score
            }
            else
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
                    else
                    {
                        SpawnCoin(enemy.SpawnSide);
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

            _respawnSound.Play();
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
            if (IsPow(character.PixelPositionX / 8, (character.PixelPositionY - 20) / 8))
            {
                POW();
            }
            else
            {
                BumpEnemy(character.PixelPositionX, (character.PixelPositionY - 20) / 8);
            }
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
                    int bumpDirection = 999;
                    if (positionX <= enemy.PixelPositionX - 4 && positionX >= enemy.PixelPositionX - 12)
                    {
                        bumpDirection = 1;
                    }
                    if (positionX >= enemy.PixelPositionX + 4 && positionX <= enemy.PixelPositionX + 12)
                    {
                        bumpDirection = -1;
                    }
                    if (positionX >= enemy.PixelPositionX - 4 && positionX <= enemy.PixelPositionX + 4)
                    {
                        bumpDirection = 0;
                    }

                    if (bumpDirection != 999)
                    {
                        enemy.Bump(bumpDirection, true);
                        if (enemy.IsFlipped && enemy is not Coin)
                        {
                            IncreaseScore(_mario, ConfigManager.GetConfig("FLIP_SCORE", 10), Vector2.Zero);
                        }
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
                    DrawScore(_spriteBatch);
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

            DrawScore(_spriteBatch);

            DrawPopScores(_spriteBatch);

            _mario.Draw(_spriteBatch);
            _mario.Draw(_spriteBatch, SCREEN_WIDTH, 0);
            _mario.Draw(_spriteBatch, -SCREEN_WIDTH, 0);

            DrawSplash(_spriteBatch);

            DrawFireball(_spriteBatch, _redFireball);
            DrawFireball(_spriteBatch, _greenFireball);
        }

        private void DrawScore(SpriteBatch spriteBatch)
        {
            // Le score du joueur 1
            _scoreTitles.DrawFrame(0, spriteBatch, new Vector2(31, 16), 0, Vector2.One, Color.White);
            DrawNumber(spriteBatch, _mario.Score, new Vector2(72, 16), new Vector2(-8, 0));

            // Le score du joueur 2 -- uniquement si en multi
            // Le highscore
            _scoreTitles.DrawFrame(1, spriteBatch, new Vector2(111, 16), 0, Vector2.One, Color.White);
            DrawNumber(spriteBatch, _highscore, new Vector2(152, 16), new Vector2(-8, 0));
        }

        private void DrawNumber(SpriteBatch spriteBatch, int score, Vector2 position, Vector2 positionDelta)
        {
            for (int i = 0; i < 6; i++)
            {
                int number = score % 10;

                _digits.DrawFrame(number, spriteBatch, position, 0, Vector2.One, Color.White);

                score = score / 10;
                position += positionDelta;
            }
        }

        private void DrawLevel(SpriteBatch spriteBatch, float deltaTime)
        {
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 26; y++)
                {
                    if (_level[x, y] == LevelTile.Tile)
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

            if (_powLeft > 0)
            {
                _powSprite.DrawFrame(3 - _powLeft, spriteBatch, new Vector2(120, 160), 0, Vector2.One, Color.White);
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
            int side = _random.Next(0, 2) * 2 - 1;
            switch (type)
            {
                default:
                case Enemy.EnemyType.Turtle:
                    SpawnTurtle(side);
                    break;
                case Enemy.EnemyType.Crab:
                    SpawnCrab(side);
                    break;
            }
        }

        int spawnCount;
        private void SpawnTurtle(int side)
        {
            spawnCount++;
            side = -1;
            Enemy newTurtle = new Enemy(_turtleSpriteSheet, _level, _enemySpawnSpound, _enemyFlip);
            newTurtle.name = $"Turtle {spawnCount}";
            newTurtle.SetBaseSpeed(ConfigManager.GetConfig("TURTLE_SPEED", 25f));
            newTurtle.Enter(side);
            _enemies.Add(newTurtle);
        }

        private void SpawnCrab(int side)
        {
            Enemy newCrab = new Crab(_crabSpriteSheet, _level, _enemySpawnSpound, _enemyFlip);

            newCrab.SetBaseSpeed(ConfigManager.GetConfig("CRAB_SPEED", 30f));
            newCrab.Enter(side);
            _enemies.Add(newCrab);
        }

        private void Splash(int x)
        {
            _splashX = x;
            _splashCurrentFrame = 0;
            _splash.Play();
        }

        private void InitPOW()
        {
            _powLeft = _powSprite.FrameCount;
            _level[15, 20] = LevelTile.Pow;
            _level[16, 20] = LevelTile.Pow;
        }

        private bool IsPow(int gridX, int gridY)
        {
            return _level[gridX, gridY] == LevelTile.Pow;
        }

        private void ClearPOW()
        {
            _level[15, 20] = LevelTile.Empty;
            _level[16, 20] = LevelTile.Empty;
        }

        private void POW()
        {
            foreach (Enemy enemy in _enemies)
            {
                if (!enemy.IsFalling) // TODO: allow if next to landing or all the way during the POW duration
                {
                    enemy.Bump(0, false);
                }
            }

            CameraShake.Shake(new Vector2(0, -1), 32, MathF.PI / 0.25f, 0.25f);
            _powSound.Play();

            _powLeft--;
            if (_powLeft == 0)
            {
                ClearPOW();
            }
        }

        #region Coins
        private void SpawnCoin(int side)
        {
            spawnCount++;
            Coin newCoin = new Coin(_coinSpriteSheet, _level, _coinSpawnSound, _coinCollectSound);
            newCoin.SetBaseSpeed(ConfigManager.GetConfig("COIN_SPEED", 50f));
            newCoin.Enter(side);
            _enemies.Add(newCoin);
        }

        public void SpawnBonusLevelCoins()
        {
            Vector2[] positions = new Vector2[]
            {
                new Vector2(57,36),
                new Vector2(201,36),
                new Vector2(24,90),
                new Vector2(44,90),
                new Vector2(232,90),
                new Vector2(212,90),
                new Vector2(96,138),
                new Vector2(160,138),
                new Vector2(41,186),
                new Vector2(215,186),
            };
            for(int i = 0; i< 10;i++)
            {
                Coin newCoin = new Coin(_coinSpriteSheet, _level, _coinSpawnSound, _coinCollectSound);
                newCoin.SetBaseSpeed(0f);
                newCoin.MoveTo(positions[i]);
                newCoin.SetFrame(_random.Next(newCoin.SpriteSheet.GetAnimationFrameCount("Rotate")));
                newCoin.IgnorePlatforms = true;
                _enemies.Add(newCoin);
            }
        }

        #endregion

        #region Scoring
        private class PopScore
        {
            public int spriteFrame;
            public Vector2 position;
            public float timer;
        }

        private List<PopScore> _popScores = new List<PopScore>();

        private void IncreaseScore(Player player, int score, Vector2 position)
        {
            player.IncreaseScore(score);
            if (player.Score > _highscore)
            {
                _highscore = player.Score;
            }
            if (player.Score > 20000 && !player.ExtraLifeGained)
            {
                player.AddLife();
                // play sound
            }
            Vector2 scorePosition = position - new Vector2(0, _marioSpriteSheet.SpriteHeight);
            switch (score)
            {
                case 800:
                    _popScores.Add(new PopScore { spriteFrame = 0, position = scorePosition, timer = 0 });
                    break;
                case 1600:
                    _popScores.Add(new PopScore { spriteFrame = 1, position = scorePosition, timer = 0 });
                    break;
                case 2400:
                    _popScores.Add(new PopScore { spriteFrame = 2, position = scorePosition, timer = 0 });
                    break;
                case 3200:
                    _popScores.Add(new PopScore { spriteFrame = 3, position = scorePosition, timer = 0 });
                    break;
            }
        }

        private void UpdatePopScores(float deltaTime)
        {
            for (int i = 0; i < _popScores.Count; i++)
            {
                _popScores[i].timer += deltaTime;
                if (_popScores[i].timer > 2f)
                {
                    _popScores.Remove(_popScores[i]);
                }
            }
        }

        private void DrawPopScores(SpriteBatch spriteBatch)
        {
            foreach (PopScore score in _popScores)
            {
                _scorePopup.DrawFrame(score.spriteFrame, spriteBatch, score.position, 0, Vector2.One, _marioScoreColor);
            }
        }
        #endregion

        #region Fireballs
        private Character _redFireball;
        private Character _greenFireball;
        private bool _exploding;
        private float _redFireballSpawnTimer;

        private void SpawnRedFireball()
        {
            _redFireball ??= new Character(_redFireballSpriteSheet);
            _redFireball.MoveTo(new Vector2(81, 31));
            _redFireball.SetAnimation("Move");
            _redFireball.LookTo(new Vector2(_random.Next(0, 2) * 2 - 1, _random.Next(0, 2) * 2 - 1), rotate: false);
            _redFireball.Visible = true;
            _exploding = false;
        }

        private void KillRedFireBall()
        {
            _redFireball.Visible = false;
            _redFireballSpawnTimer = 0;
        }

        private void UpdateRedFireball(float deltaTime)
        {
            if (_redFireball != null && _redFireball.Visible)
            {
                if (!_exploding)
                {
                    Vector2 lookTo = _redFireball.MoveDirection;
                    bool hasBumped = false;
                    if (_redFireball.Position.X - _redFireball.SpriteSheet.LeftMargin <= 0)
                    {
                        hasBumped = BumpLeft(ref lookTo);
                    }
                    if (_redFireball.Position.X + _redFireball.SpriteSheet.RightMargin > SCREEN_WIDTH)
                    {
                        hasBumped = BumpRight(ref lookTo);
                    }

                    if (_redFireball.Position.Y - _redFireball.SpriteSheet.TopMargin <= 0)
                    {
                        hasBumped = BumpUp(ref lookTo);
                    }
                    if (_redFireball.Position.Y + _redFireball.SpriteSheet.BottomMargin >= SCREEN_HEIGHT)
                    {
                        hasBumped = BumpDown(ref lookTo);
                    }

                    // Test platform collision
                    int centerGridPositionX = (int)MathF.Floor(_redFireball.Position.X / 8);
                    int lowerGridPositionY = (int)MathF.Floor((_redFireball.Position.Y + _redFireballSpriteSheet.BottomMargin) / 8);
                    int upperGridPositionY = (int)MathF.Floor((_redFireball.Position.Y - _redFireballSpriteSheet.TopMargin) / 8);
                    if (lowerGridPositionY < 30 && _level[centerGridPositionX, lowerGridPositionY] != LevelTile.Empty)
                    {
                        hasBumped = BumpDown(ref lookTo);
                    }
                    if (upperGridPositionY >= 0 && _level[centerGridPositionX, upperGridPositionY] != LevelTile.Empty)
                    {
                        hasBumped = BumpUp(ref lookTo);
                    }

                    int centerGridPositionY = (int)MathF.Floor(_redFireball.Position.Y / 8);
                    int leftGridPositionX = (int)MathF.Floor((_redFireball.Position.X - _redFireballSpriteSheet.LeftMargin) / 8);
                    int rightGridPositionX = (int)MathF.Floor((_redFireball.Position.X + _redFireballSpriteSheet.RightMargin) / 8);
                    if (leftGridPositionX >= 0 && _level[leftGridPositionX, centerGridPositionY] != LevelTile.Empty)
                    {
                        hasBumped = BumpLeft(ref lookTo);
                    }
                    if (rightGridPositionX < 32 && _level[rightGridPositionX, centerGridPositionY] != LevelTile.Empty)
                    {
                        hasBumped = BumpRight(ref lookTo);
                    }

                    if (hasBumped)
                    {
                        _redFireballSound.Play();
                    }

                    _redFireball.LookTo(lookTo);

                    float newX = _redFireball.Position.X + _redFireball.MoveDirection.X * ConfigManager.GetConfig("MARIO_MAX_SPEED", 75) * deltaTime;
                    float newY = _redFireball.Position.Y + _redFireball.MoveDirection.Y * (ConfigManager.GetConfig("MARIO_MAX_SPEED", 75) / 1) * deltaTime;
                    _redFireball.MoveTo(new Vector2(newX, newY));

                    TestFireballCollision(_mario, _redFireball);
                }

                _redFireball.Animate(deltaTime);
            }
            else
            {
                _redFireballSpawnTimer += deltaTime;
                if (_redFireballSpawnTimer > ConfigManager.GetConfig("RED_FIREBALL_COOLDOWN", 60))
                {
                    SpawnRedFireball();
                }
            }
        }

        private bool BumpLeft(ref Vector2 lookTo)
        {
            bool hasBumped = false;
            if (lookTo.X < 0)
            {
                hasBumped = true;
            }
            lookTo.X = 1;

            return hasBumped;
        }
        private bool BumpRight(ref Vector2 lookTo)
        {
            bool hasBumped = false;
            if (lookTo.X > 0)
            {
                hasBumped = true;
            }
            lookTo.X = -1;

            return hasBumped;
        }

        private bool BumpUp(ref Vector2 lookTo)
        {
            bool hasBumped = false;
            if (lookTo.Y < 0)
            {
                hasBumped = true;
            }
            lookTo.Y = 1;

            return hasBumped;
        }
        private bool BumpDown(ref Vector2 lookTo)
        {
            bool hasBumped = false;
            if (lookTo.Y > 0)
            {
                hasBumped = true;
            }
            lookTo.Y = -1;

            return hasBumped;
        }

        private void DrawFireball(SpriteBatch spriteBatch, Character fireball)
        {
            if (fireball != null && fireball.Visible)
            {
                fireball.Draw(spriteBatch);
            }
        }

        private void TestFireballCollision(Player player, Character fireball)
        {
            int relativePositionX = Math.Abs(player.PixelPositionX - fireball.PixelPositionX);
            int relativePositionY = Math.Abs(player.PixelPositionY - MARIO_COLLISION_HEIGHT / 2 - fireball.PixelPositionY);
            if (relativePositionX <= MARIO_COLLISION_WIDTH / 2 && relativePositionY <= MARIO_COLLISION_HEIGHT / 2)
            {
                KillMario();
                _exploding = true;
                fireball.SetSpeed(0f);
                if (fireball == _redFireball)
                {
                    fireball.SetAnimation("Disappear", KillRedFireBall);
                }
                else
                {
                    _greenFireballSoundInstance.Stop();
                    fireball.SetAnimation("Disappear", KillGreenFireBall);
                }

            }
        }

        private int _greenAppearAnimationCount;
        private bool _greenFireballIsMoving;
        private float _greenFireballTimer;
        private bool _greenFireballSpawned;
        private float _greenMoveTimer;
        private float _greenStartY;
        private float _cycleDuration = 1.25f;
        private float _maxHeight = 16f;
        private float _minHeight = 8f;


        private void SpawnGreenFireball()
        {
            _greenFireball ??= new Character(_greenFireballSpriteSheet);

            // Etage de mario
            float spawnY = 53;
            if (_mario.PixelPositionY <= 64)
                spawnY += 0;
            else if (_mario.PixelPositionY <= 120)
                spawnY += 48;
            else if (_mario.PixelPositionY <= 160)
                spawnY += 48 * 2;
            else
                spawnY += 48 * 3;

            if (_mario.PixelPositionX > SCREEN_WIDTH / 2)
            {
                _greenFireball.MoveTo(new Vector2(24, spawnY));
                _greenFireball.LookTo(new Vector2(1, 0), rotate: false);
            }
            else
            {
                _greenFireball.MoveTo(new Vector2(SCREEN_WIDTH - 24, spawnY));
                _greenFireball.LookTo(new Vector2(-1, 0), rotate: false);
            }

            _greenAppearAnimationCount = 0;
            _greenFireballIsMoving = false;
            _greenFireballTimer = 0;
            _greenFireball.SetAnimation("Appear", UpdateGreenFireballAnimation);
            _greenFireballSpawnSound.Play();
            _greenFireball.Visible = true;
            _greenFireballSpawned = true;
        }

        private void UpdateGreenFireballAnimation()
        {
            _greenAppearAnimationCount++;
            if (_greenAppearAnimationCount > 2)
            {
                _greenFireball.SetAnimation("Move");
                _greenFireball.SetBaseSpeed(_mario.MaxSpeed);
                _greenFireball.SetSpeed(1f);
                _greenMoveTimer = 0;
                _greenStartY = _greenFireball.Position.Y;
                _greenFireballIsMoving = true;
                _greenFireballSoundInstance.Play();
            }
        }

        private void UpdateGreenFireball(float deltaTime)
        {
            if (_greenFireball == null || !_greenFireball.Visible)
            {
                _greenFireballTimer += deltaTime;
                if (_greenFireballTimer > (_greenFireballSpawned ? 5f : 45f))
                {
                    SpawnGreenFireball();
                }
            }
            else
            {
                _greenFireball.Animate(deltaTime);
                if (_greenFireballIsMoving)
                {
                    _greenFireball.Move(deltaTime);
                    if (_greenFireball.PixelPositionX > SCREEN_WIDTH - 24 || _greenFireball.PixelPositionX < 24)
                    {
                        _greenFireball.SetSpeed(0f);
                        _greenFireballSoundInstance.Stop();
                        _greenFireball.SetAnimation("Disappear", KillGreenFireBall);
                    }
                    else
                    {
                        _greenMoveTimer += deltaTime;

                        _greenMoveTimer %= _cycleDuration;

                        float y = (MathF.Sin(_greenMoveTimer * MathF.PI * 4 / _cycleDuration - MathF.PI / 2) + 1) / 2;

                        float height;
                        if (_greenMoveTimer < _cycleDuration / 2)
                        {
                            height = _maxHeight;
                        }
                        else
                        {

                            height = _minHeight;
                        }

                        _greenFireball.MoveTo(new Vector2(_greenFireball.Position.X, _greenStartY - y * height));
                    }
                }

                TestFireballCollision(_mario, _greenFireball);
            }
        }

        private void KillGreenFireBall()
        {
            _greenFireball.Visible = false;
            _greenFireballTimer = 0;
        }

        #endregion
    }
}