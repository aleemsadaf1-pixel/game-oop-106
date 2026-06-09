using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// ═══════════════════════════════════════════════════════════════
//  SPRITE LOADER  — loads all images from assets\ folder
// ═══════════════════════════════════════════════════════════════
static class Sprites
{
    public static Image Player      { get; private set; }
    public static Image EnemyBasic  { get; private set; }
    public static Image EnemyFast   { get; private set; }
    public static Image EnemyBoss   { get; private set; }
    public static Image BulletPlayer{ get; private set; }
    public static Image BulletEnemy { get; private set; }
    public static Image Background  { get; private set; }

    public static void Load(string assetsDir)
    {
        Player       = LoadImg(assetsDir, "player.png");
        EnemyBasic   = LoadImg(assetsDir, "enemy_basic.png");
        EnemyFast    = LoadImg(assetsDir, "enemy_fast.png");
        EnemyBoss    = LoadImg(assetsDir, "enemy_boss.png");
        BulletPlayer = LoadImg(assetsDir, "bullet_player.png");
        BulletEnemy  = LoadImg(assetsDir, "bullet_enemy.png");
        Background   = LoadImg(assetsDir, "background.png");
    }

    private static Image LoadImg(string dir, string file)
    {
        string path = Path.Combine(dir, file);
        if (!File.Exists(path)) return null;
        // Load into MemoryStream so the file handle is released
        byte[] bytes = File.ReadAllBytes(path);
        return Image.FromStream(new MemoryStream(bytes));
    }

    // Helper — draw an image scaled to a destination rectangle
    public static void Draw(Graphics g, Image img, float x, float y, float w, float h)
    {
        if (img == null) return;
        g.DrawImage(img, x, y, w, h);
    }
}

// ═══════════════════════════════════════════════════════════════
//  INTERFACES  (Interface Usage & Abstraction)
// ═══════════════════════════════════════════════════════════════
interface IDrawable   { void Draw(Graphics g); }
interface IUpdatable  { void Update(); }
interface ICollidable
{
    RectangleF Bounds { get; }
    bool CollidesWith(ICollidable other);
}

// ═══════════════════════════════════════════════════════════════
//  ABSTRACT BASE CLASS  (Class Design & Encapsulation)
// ═══════════════════════════════════════════════════════════════
abstract class GameObject : IDrawable, IUpdatable, ICollidable
{
    private float _x, _y, _width, _height;
    private bool  _isAlive = true;

    public float X       { get => _x;      set => _x      = value; }
    public float Y       { get => _y;      set => _y      = value; }
    public float Width   { get => _width;  set => _width  = value; }
    public float Height  { get => _height; set => _height = value; }
    public bool  IsAlive { get => _isAlive;set => _isAlive = value; }

    public RectangleF Bounds => new RectangleF(_x + 6, _y + 6, _width - 12, _height - 12);

    protected GameObject(float x, float y, float w, float h)
    { _x = x; _y = y; _width = w; _height = h; }

    public abstract void Draw(Graphics g);
    public abstract void Update();

    public bool CollidesWith(ICollidable other) =>
        IsAlive && Bounds.IntersectsWith(other.Bounds);
}

// ═══════════════════════════════════════════════════════════════
//  BULLET  (Inheritance)
// ═══════════════════════════════════════════════════════════════
class Bullet : GameObject
{
    private float _speed;
    private bool  _fromPlayer;
    private Image _img;

    public bool FromPlayer => _fromPlayer;

    public Bullet(float x, float y, float speed, bool fromPlayer)
        : base(x, y, 20, 36)
    {
        _speed      = speed;
        _fromPlayer = fromPlayer;
        _img        = fromPlayer ? Sprites.BulletPlayer : Sprites.BulletEnemy;
    }

    public override void Update()
    {
        Y += _speed;
        if (Y < -50 || Y > 750) IsAlive = false;
    }

    public override void Draw(Graphics g)
    {
        if (!IsAlive) return;
        if (_img != null)
            Sprites.Draw(g, _img, X, Y, Width, Height);
        else
        {
            // Fallback shape
            using var b = new SolidBrush(_fromPlayer ? Color.Cyan : Color.OrangeRed);
            g.FillRectangle(b, X, Y, Width, Height);
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  PLAYER  (Inheritance + Encapsulation)
// ═══════════════════════════════════════════════════════════════
class Player : GameObject
{
    private int   _health;
    private int   _maxHealth = 100;
    private int   _score;
    private int   _shootCooldown;
    private float _speed = 6f;

    public int Health    => _health;
    public int MaxHealth => _maxHealth;
    public int Score     { get => _score; set => _score = value; }
    public bool IsDead   => _health <= 0;

    public Player(float x, float y) : base(x, y, 56, 64)
    { _health = _maxHealth; }

    public void TakeDamage(int dmg) => _health = Math.Max(0, _health - dmg);

    public Bullet TryShoot()
    {
        if (_shootCooldown > 0) { _shootCooldown--; return null; }
        _shootCooldown = 12;
        return new Bullet(X + Width / 2 - 10, Y - 20, -16, true);
    }

    public void MoveLeft()            => X = Math.Max(0, X - _speed);
    public void MoveRight(int maxW)   => X = Math.Min(maxW - Width, X + _speed);
    public void MoveUp()              => Y = Math.Max(0, Y - _speed);
    public void MoveDown(int maxH)    => Y = Math.Min(maxH - Height - 60, Y + _speed);

    public override void Update() { if (_shootCooldown > 0) _shootCooldown--; }

    public override void Draw(Graphics g)
    {
        if (!IsAlive) return;
        if (Sprites.Player != null)
            Sprites.Draw(g, Sprites.Player, X, Y, Width, Height);
        else
        {
            // Fallback
            PointF[] pts = {
                new PointF(X + Width/2, Y), new PointF(X + Width, Y + Height),
                new PointF(X + Width/2, Y + Height - 14), new PointF(X, Y + Height)
            };
            using var b = new SolidBrush(Color.Cyan);
            g.FillPolygon(b, pts);
        }
    }
}

// ═══════════════════════════════════════════════════════════════
//  ABSTRACT ENEMY  (Polymorphism)
// ═══════════════════════════════════════════════════════════════
abstract class Enemy : GameObject
{
    protected int   _health;
    protected int   _maxHealth;
    protected int   _points;
    protected float _speed;

    public int Points    => _points;
    public int Health    => _health;
    public int MaxHealth => _maxHealth;

    protected Enemy(float x, float y, float w, float h, int hp, int pts, float spd)
        : base(x, y, w, h)
    { _health = _maxHealth = hp; _points = pts; _speed = spd; }

    public void TakeDamage(int dmg)
    { _health -= dmg; if (_health <= 0) IsAlive = false; }

    public abstract Bullet TryShoot(Random rng);
}

// ─────────────── Basic Enemy ────────────────────────────────
class BasicEnemy : Enemy
{
    private float _wobble;

    public BasicEnemy(float x, float y, Random rng)
        : base(x, y, 48, 44, 25, 100, 1.2f + (float)rng.NextDouble())
    { _wobble = (float)(rng.NextDouble() * Math.PI * 2); }

    public override void Update()
    {
        _wobble += 0.05f;
        X += (float)Math.Sin(_wobble) * 1.2f;
        Y += _speed;
        if (Y > 720) IsAlive = false;
    }

    public override Bullet TryShoot(Random rng)
    {
        if (rng.Next(0, 220) == 0)
            return new Bullet(X + Width / 2 - 10, Y + Height, 5, false);
        return null;
    }

    public override void Draw(Graphics g)
    {
        if (!IsAlive) return;
        if (Sprites.EnemyBasic != null)
            Sprites.Draw(g, Sprites.EnemyBasic, X, Y, Width, Height);
        else
        {
            PointF[] pts = {
                new PointF(X + Width/2, Y + Height), new PointF(X + Width, Y),
                new PointF(X + Width/2, Y + 10), new PointF(X, Y)
            };
            using var b = new SolidBrush(Color.Magenta);
            g.FillPolygon(b, pts);
        }
    }
}

// ─────────────── Fast Enemy ──────────────────────────────────
class FastEnemy : Enemy
{
    public FastEnemy(float x, float y, Random rng)
        : base(x, y, 36, 30, 10, 200, 3.5f + (float)rng.NextDouble() * 2) { }

    public override void Update() { Y += _speed; if (Y > 720) IsAlive = false; }

    public override Bullet TryShoot(Random rng) => null;

    public override void Draw(Graphics g)
    {
        if (!IsAlive) return;
        if (Sprites.EnemyFast != null)
            Sprites.Draw(g, Sprites.EnemyFast, X, Y, Width, Height);
        else
        {
            PointF[] pts = {
                new PointF(X + Width/2, Y + Height), new PointF(X + Width, Y + 6), new PointF(X, Y + 6)
            };
            using var b = new SolidBrush(Color.Yellow);
            g.FillPolygon(b, pts);
        }
    }
}

// ─────────────── Boss Enemy ──────────────────────────────────
class BossEnemy : Enemy
{
    private float _dir = 1;
    private int   _shootTimer;

    public BossEnemy(float x, float y)
        : base(x, y, 100, 84, 400, 1000, 1f) { }

    public override void Update()
    {
        X += _dir * _speed;
        if (X < 0 || X > 380) _dir = -_dir;
        if (_shootTimer > 0) _shootTimer--;
    }

    public override Bullet TryShoot(Random rng)
    {
        if (_shootTimer <= 0)
        {
            _shootTimer = 35;
            return new Bullet(X + Width / 2 - 10, Y + Height, 6, false);
        }
        return null;
    }

    public override void Draw(Graphics g)
    {
        if (!IsAlive) return;
        if (Sprites.EnemyBoss != null)
            Sprites.Draw(g, Sprites.EnemyBoss, X, Y, Width, Height);
        else
        {
            using var b = new SolidBrush(Color.DarkRed);
            g.FillEllipse(b, X, Y, Width, Height);
        }

        // Boss health bar (always drawn)
        float ratio = Math.Max(0f, _health / (float)_maxHealth);
        g.FillRectangle(new SolidBrush(Color.FromArgb(120, 60, 0, 0)), X, Y - 12, Width, 8);
        using var hb = new LinearGradientBrush(
            new PointF(X, Y - 12), new PointF(X + Width, Y - 12),
            Color.DarkRed, Color.OrangeRed);
        g.FillRectangle(hb, X, Y - 12, Width * ratio, 8);
        using var p = new Pen(Color.FromArgb(180, 255, 80, 80), 1);
        g.DrawRectangle(p, X, Y - 12, Width, 8);
    }
}

// ═══════════════════════════════════════════════════════════════
//  EXPLOSION PARTICLE
// ═══════════════════════════════════════════════════════════════
class Particle
{
    public float X, Y, Vx, Vy, Life;
    public Color Color;
    public float Size;

    public Particle(float x, float y, float vx, float vy, Color c, float life, float size)
    { X = x; Y = y; Vx = vx; Vy = vy; Color = c; Life = life; Size = size; }

    public bool Dead => Life <= 0;

    public void Update() { X += Vx; Y += Vy; Vy += 0.12f; Vx *= 0.97f; Life -= 2; Size *= 0.97f; }

    public void Draw(Graphics g)
    {
        int alpha = (int)Math.Max(0, Math.Min(255, Life * 2.55f));
        using var b = new SolidBrush(Color.FromArgb(alpha, Color));
        g.FillEllipse(b, X - Size / 2, Y - Size / 2, Size, Size);
    }
}

// ═══════════════════════════════════════════════════════════════
//  MAIN GAME FORM
// ═══════════════════════════════════════════════════════════════
class GameForm : Form
{
    // ── constants ──────────────────────────────────────────────
    private const int W = 480, H = 700;

    // ── game state ─────────────────────────────────────────────
    private enum GameState { Menu, Playing, GameOver }
    private GameState _state = GameState.Menu;

    // ── game objects ───────────────────────────────────────────
    private Player         _player;
    private List<Enemy>    _enemies   = new List<Enemy>();
    private List<Bullet>   _bullets   = new List<Bullet>();
    private List<Particle> _particles = new List<Particle>();

    // ── game timing / difficulty ───────────────────────────────
    private Random _rng        = new Random();
    private int    _spawnTimer = 0;
    private int    _spawnRate  = 60;
    private int    _wave       = 1;
    private int    _waveTimer  = 0;
    private int    _highScore  = 0;

    // ── input ──────────────────────────────────────────────────
    private HashSet<Keys> _keys = new HashSet<Keys>();

    // ── rendering ──────────────────────────────────────────────
    private Bitmap   _buffer;
    private Graphics _bufferG;

    // ── fonts ──────────────────────────────────────────────────
    private Font _fontBig   = new Font("Segoe UI", 30, FontStyle.Bold);
    private Font _fontMed   = new Font("Segoe UI", 14, FontStyle.Bold);
    private Font _fontSmall = new Font("Segoe UI", 10);

    // ── timer ──────────────────────────────────────────────────
    private Timer _timer = new Timer { Interval = 16 };

    // ── scrolling background ───────────────────────────────────
    private float _bgY = 0;

    public GameForm()
    {
        Text            = "SPACE SHOOTER  —  OOP C# Demo";
        ClientSize      = new Size(W, H);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = Color.Black;
        DoubleBuffered  = true;

        // Load sprites from assets folder next to exe / source
        string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        if (!Directory.Exists(assetsDir))
            assetsDir = Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "assets");
        Sprites.Load(assetsDir);

        _buffer  = new Bitmap(W, H);
        _bufferG = Graphics.FromImage(_buffer);
        _bufferG.SmoothingMode = SmoothingMode.AntiAlias;

        _timer.Tick += (s, e) => GameTick();
        _timer.Start();

        KeyDown += (s, e) => { _keys.Add(e.KeyCode); HandleMenuKey(e.KeyCode); };
        KeyUp   += (s, e) => _keys.Remove(e.KeyCode);
    }

    // ── key handler ────────────────────────────────────────────
    private void HandleMenuKey(Keys k)
    {
        if (_state == GameState.Menu    && k == Keys.Enter) StartGame();
        if (_state == GameState.GameOver && k == Keys.Enter) StartGame();
        if (_state == GameState.Playing  && k == Keys.Escape) _state = GameState.Menu;
    }

    // ── start / reset ──────────────────────────────────────────
    private void StartGame()
    {
        _player    = new Player(W / 2 - 28, H - 130);
        _enemies.Clear();
        _bullets.Clear();
        _particles.Clear();
        _wave      = 1;
        _spawnRate = 60;
        _waveTimer = 0;
        _spawnTimer= 0;
        _state     = GameState.Playing;
    }

    // ── main loop ──────────────────────────────────────────────
    private void GameTick()
    {
        if (_state == GameState.Playing) UpdateGame();
        // scroll background every frame regardless of state
        _bgY = (_bgY + 0.6f) % H;
        Render();
    }

    private void UpdateGame()
    {
        // Player input
        if (_keys.Contains(Keys.Left)  || _keys.Contains(Keys.A)) _player.MoveLeft();
        if (_keys.Contains(Keys.Right) || _keys.Contains(Keys.D)) _player.MoveRight(W);
        if (_keys.Contains(Keys.Up)    || _keys.Contains(Keys.W)) _player.MoveUp();
        if (_keys.Contains(Keys.Down)  || _keys.Contains(Keys.S)) _player.MoveDown(H);

        if (_keys.Contains(Keys.Space))
        {
            var b = _player.TryShoot();
            if (b != null) _bullets.Add(b);
        }

        _player.Update();

        // Wave scaling
        _waveTimer++;
        if (_waveTimer > 600) { _wave++; _spawnRate = Math.Max(18, _spawnRate - 5); _waveTimer = 0; }

        // Spawn enemies
        _spawnTimer++;
        if (_spawnTimer >= _spawnRate) { _spawnTimer = 0; SpawnEnemy(); }

        // Boss every 5 waves (on wave start)
        if (_waveTimer == 1 && _wave % 5 == 0)
            _enemies.Add(new BossEnemy(W / 2 - 50, -100));

        // Update enemies + their shooting
        foreach (var e in _enemies)
        {
            e.Update();
            var shot = e.TryShoot(_rng);
            if (shot != null) _bullets.Add(shot);
        }

        // Update bullets
        foreach (var b in _bullets) b.Update();

        // ── Collision: player bullets → enemies ────────────────
        foreach (var bullet in _bullets.Where(b => b.IsAlive && b.FromPlayer))
            foreach (var enemy in _enemies.Where(e => e.IsAlive))
                if (bullet.CollidesWith(enemy))
                {
                    bullet.IsAlive = false;
                    enemy.TakeDamage(30);
                    SpawnParticles(enemy.X + enemy.Width / 2, enemy.Y + enemy.Height / 2, false);
                    if (!enemy.IsAlive)
                    {
                        _player.Score += enemy.Points;
                        SpawnParticles(enemy.X + enemy.Width / 2, enemy.Y + enemy.Height / 2, true);
                    }
                }

        // ── Collision: enemy bullets → player ──────────────────
        foreach (var bullet in _bullets.Where(b => b.IsAlive && !b.FromPlayer))
            if (bullet.CollidesWith(_player))
            {
                bullet.IsAlive = false;
                _player.TakeDamage(15);
                SpawnParticles(_player.X + _player.Width / 2, _player.Y + 20, false);
            }

        // ── Collision: enemies touching player ─────────────────
        foreach (var e in _enemies.Where(en => en.IsAlive))
            if (e.CollidesWith(_player))
            {
                e.IsAlive = false;
                _player.TakeDamage(30);
                SpawnParticles(e.X + e.Width / 2, e.Y + e.Height / 2, true);
            }

        // Update particles
        foreach (var p in _particles) p.Update();

        // Cleanup dead objects
        _bullets.RemoveAll(b => !b.IsAlive);
        _enemies.RemoveAll(e => !e.IsAlive);
        _particles.RemoveAll(p => p.Dead);

        // Game-over check
        if (_player.IsDead)
        {
            if (_player.Score > _highScore) _highScore = _player.Score;
            _state = GameState.GameOver;
        }
    }

    private void SpawnEnemy()
    {
        float x = _rng.Next(10, W - 60);
        int   t = _rng.Next(0, _wave > 2 ? 3 : 2);
        Enemy en = t == 0 ? (Enemy)new BasicEnemy(x, -50, _rng)
                 : t == 1 ? (Enemy)new FastEnemy(x, -40, _rng)
                 :           (Enemy)new BossEnemy(x, -90);
        _enemies.Add(en);
    }

    private void SpawnParticles(float x, float y, bool big)
    {
        int count = big ? 32 : 10;
        Color[] colors = { Color.OrangeRed, Color.Yellow, Color.Orange, Color.White, Color.Tomato };
        for (int i = 0; i < count; i++)
        {
            float vx   = (float)(_rng.NextDouble() * 8 - 4);
            float vy   = (float)(_rng.NextDouble() * 8 - 4);
            Color col  = colors[_rng.Next(colors.Length)];
            float life = 40 + (float)_rng.NextDouble() * 60;
            float size = big ? 4 + (float)_rng.NextDouble() * 8 : 2 + (float)_rng.NextDouble() * 4;
            _particles.Add(new Particle(x, y, vx, vy, col, life, size));
        }
    }

    // ── render ─────────────────────────────────────────────────
    private void Render()
    {
        var g = _bufferG;
        g.Clear(Color.FromArgb(5, 5, 20));

        // Scrolling space background
        DrawBackground(g);

        if (_state == GameState.Playing)
        {
            foreach (var e in _enemies)   e.Draw(g);
            foreach (var b in _bullets)   b.Draw(g);
            foreach (var p in _particles) p.Draw(g);
            _player.Draw(g);
            DrawHUD(g);
        }
        else if (_state == GameState.Menu)
        {
            DrawMenu(g);
        }
        else
        {
            DrawGameOver(g);
        }

        using var screen = CreateGraphics();
        screen.DrawImageUnscaled(_buffer, 0, 0);
    }

    private void DrawBackground(Graphics g)
    {
        if (Sprites.Background != null)
        {
            // Tile the background image scrolling downward
            int bw = Sprites.Background.Width;
            int bh = Sprites.Background.Height;
            float sy = _bgY % H;
            // Draw two tiles for seamless scroll
            g.DrawImage(Sprites.Background, 0, sy - H, W, H);
            g.DrawImage(Sprites.Background, 0, sy, W, H);
        }
        else
        {
            // Fallback: simple gradient dark space
            using var grad = new LinearGradientBrush(
                new PointF(0, 0), new PointF(0, H),
                Color.FromArgb(10, 0, 20), Color.FromArgb(0, 0, 10));
            g.FillRectangle(grad, 0, 0, W, H);
        }
    }

    private void DrawHUD(Graphics g)
    {
        // Semi-transparent top bar
        using (var bar = new SolidBrush(Color.FromArgb(140, 0, 0, 20)))
            g.FillRectangle(bar, 0, 0, W, 64);

        g.DrawString($"SCORE  {_player.Score:N0}", _fontMed, Brushes.Cyan, 10, 8);
        g.DrawString($"WAVE  {_wave}", _fontMed, Brushes.LightBlue, 10, 30);
        g.DrawString($"BEST  {_highScore:N0}", _fontSmall, Brushes.Gray, 10, 52);

        // HP bar
        float ratio = _player.Health / (float)_player.MaxHealth;
        g.FillRectangle(new SolidBrush(Color.FromArgb(80, 200, 0, 0)), W - 136, 14, 126, 14);
        Color hpColor = ratio > 0.5f ? Color.Lime : ratio > 0.25f ? Color.Yellow : Color.Red;
        using (var hb = new LinearGradientBrush(
            new PointF(W - 134, 16), new PointF(W - 134 + 122 * ratio, 16),
            hpColor, Color.FromArgb(180, hpColor)))
            g.FillRectangle(hb, W - 134, 16, 122 * ratio, 10);
        using (var p = new Pen(Color.FromArgb(180, 255, 255, 255), 1))
            g.DrawRectangle(p, W - 136, 14, 126, 14);
        g.DrawString("HP", _fontSmall, Brushes.White, W - 156, 12);

        // Bottom control hints
        using (var bar = new SolidBrush(Color.FromArgb(130, 0, 0, 20)))
            g.FillRectangle(bar, 0, H - 26, W, 26);
        g.DrawString("WASD / ←→↑↓ : Move    SPACE : Shoot    ESC : Menu",
            _fontSmall, new SolidBrush(Color.FromArgb(140, 200, 200, 200)), 8, H - 20);
    }

    private void DrawMenu(Graphics g)
    {
        // Overlay
        using (var ov = new SolidBrush(Color.FromArgb(160, 0, 0, 20)))
            g.FillRectangle(ov, 0, 0, W, H);

        // Title
        using var titleFont = new Font("Segoe UI", 40, FontStyle.Bold);
        using var titleBrush = new LinearGradientBrush(
            new PointF(0, H / 2 - 130), new PointF(W, H / 2 - 90),
            Color.Cyan, Color.Magenta);
        CenterText(g, "SPACE SHOOTER", titleFont, titleBrush, H / 2 - 140);

        using var subBrush = new SolidBrush(Color.FromArgb(180, 180, 255, 200));
        CenterText(g, "OOP Demonstration — C#", _fontSmall, subBrush, H / 2 - 78);

        // Play prompt (pulsing via simple modulo)
        int alpha = 160 + (int)(95 * Math.Sin(DateTime.Now.Millisecond / 300.0));
        using var pBrush = new SolidBrush(Color.FromArgb(alpha, 255, 220, 50));
        CenterText(g, "Press  ENTER  to Start", _fontMed, pBrush, H / 2 - 30);

        if (_highScore > 0)
            CenterText(g, $"Best Score:  {_highScore:N0}", _fontSmall, Brushes.Gold, H / 2 + 14);

        // Controls
        string[] controls = { "← → ↑ ↓  or  A W S D  :  Move ship",
                               "SPACE  :  Shoot laser",
                               "ESC    :  Return to menu" };
        int cy = H / 2 + 56;
        foreach (var c in controls)
        {
            CenterText(g, c, _fontSmall, new SolidBrush(Color.FromArgb(160, 200, 200, 200)), cy);
            cy += 20;
        }

        // OOP legend box
        int bx = 24, by = H / 2 + 150;
        using (var lb = new SolidBrush(Color.FromArgb(80, 0, 40, 80)))
            g.FillRectangle(lb, bx, by - 6, W - 48, 110);
        using (var lp = new Pen(Color.FromArgb(80, 0, 200, 255), 1))
            g.DrawRectangle(lp, bx, by - 6, W - 48, 110);

        string[] oop = {
            "● Abstract class  :  GameObject",
            "● Interfaces      :  IDrawable  IUpdatable  ICollidable",
            "● Inheritance     :  Player, BasicEnemy, FastEnemy, BossEnemy",
            "● Polymorphism    :  Draw() & TryShoot() overridden per class",
            "● Encapsulation   :  private fields  +  public properties"
        };
        foreach (var line in oop)
        {
            g.DrawString(line, _fontSmall, new SolidBrush(Color.FromArgb(180, 100, 220, 160)), bx + 8, by);
            by += 19;
        }
    }

    private void DrawGameOver(Graphics g)
    {
        using (var ov = new SolidBrush(Color.FromArgb(180, 10, 0, 0)))
            g.FillRectangle(ov, 0, 0, W, H);

        using var titleBrush = new LinearGradientBrush(
            new PointF(0, H / 2 - 90), new PointF(W, H / 2 - 50),
            Color.OrangeRed, Color.Yellow);
        CenterText(g, "GAME  OVER", _fontBig, titleBrush, H / 2 - 100);

        CenterText(g, $"Score :  {_player.Score:N0}", _fontMed, Brushes.White,  H / 2 - 32);
        CenterText(g, $"Best  :  {_highScore:N0}",   _fontMed, Brushes.Gold,   H / 2);

        int alpha = 160 + (int)(95 * Math.Sin(DateTime.Now.Millisecond / 300.0));
        using var pBrush = new SolidBrush(Color.FromArgb(alpha, 255, 220, 50));
        CenterText(g, "Press  ENTER  to Play Again", _fontMed, pBrush, H / 2 + 48);
        CenterText(g, "ESC — back to menu", _fontSmall,
            new SolidBrush(Color.FromArgb(130, 200, 200, 200)), H / 2 + 82);
    }

    private void CenterText(Graphics g, string text, Font font, Brush brush, float y)
    {
        var sz = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (W - sz.Width) / 2, y);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _bufferG.Dispose();
        _buffer.Dispose();
        base.OnFormClosed(e);
    }
}

// ═══════════════════════════════════════════════════════════════
//  ENTRY POINT
// ═══════════════════════════════════════════════════════════════
static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new GameForm());
    }
}
