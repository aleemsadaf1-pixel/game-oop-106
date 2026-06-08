// ══════════════════════════════════════════════════════════════
//  BRICK BREAKER GAME — OOP Lab Project (Single File)
//  Covers: Interfaces, Abstract Class, Inheritance,
//          Polymorphism, Encapsulation, Database (SQLite)
// ══════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace BrickBreakerGame
{
    // ── ENTRY POINT ──────────────────────────────────────────
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new GameForm());
        }
    }

    // ══════════════════════════════════════════════════════════
    //  INTERFACE — IGameObject (Abstraction)
    // ══════════════════════════════════════════════════════════
    public interface IGameObject
    {
        void Update();
        void Draw(Graphics g);
    }

    // ══════════════════════════════════════════════════════════
    //  INTERFACE — ICollidable (Abstraction)
    // ══════════════════════════════════════════════════════════
    public interface ICollidable
    {
        RectangleF Bounds { get; }
        void OnHit();
    }

    // ══════════════════════════════════════════════════════════
    //  INTERFACE — IScoreable (Abstraction)
    // ══════════════════════════════════════════════════════════
    public interface IScoreable
    {
        int GetScore();
    }

    // ══════════════════════════════════════════════════════════
    //  ABSTRACT CLASS — GameEntity (Encapsulation + Abstraction)
    // ══════════════════════════════════════════════════════════
    public abstract class GameEntity : IGameObject
    {
        // Private fields — ENCAPSULATION
        private float _x, _y, _width, _height;
        private bool _isActive = true;

        // Public properties
        public float X      { get => _x;      set => _x = value; }
        public float Y      { get => _y;      set => _y = value; }
        public float Width  { get => _width;   protected set => _width = value; }
        public float Height { get => _height;  protected set => _height = value; }
        public bool IsActive { get => _isActive; set => _isActive = value; }
        public RectangleF Bounds => new(_x, _y, _width, _height);

        // Constructor
        protected GameEntity(float x, float y, float w, float h)
        { _x = x; _y = y; _width = w; _height = h; }

        // Abstract methods — must be overridden (POLYMORPHISM)
        public abstract void Update();
        public abstract void Draw(Graphics g);
    }

    // ══════════════════════════════════════════════════════════
    //  BALL — Inherits GameEntity, Implements ICollidable
    // ══════════════════════════════════════════════════════════
    public class Ball : GameEntity, ICollidable
    {
        private float _dx, _dy, _speed;

        public float DX { get => _dx; set => _dx = value; }
        public float DY { get => _dy; set => _dy = value; }

        public Ball(float x, float y, float speed) : base(x, y, 16, 16)
        {
            _speed = speed;
            _dx = speed;
            _dy = -speed;
        }

        // OVERRIDE — Polymorphism
        public override void Update()
        {
            X += _dx;
            Y += _dy;
            if (X <= 0 || X + Width >= 700) _dx = -_dx;
            if (Y <= 0) _dy = -_dy;
        }

        // OVERRIDE — Polymorphism
        public override void Draw(Graphics g)
        {
            using var brush = new SolidBrush(Color.Red);
            g.FillEllipse(brush, X, Y, Width, Height);
            // White shine
            using var shine = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
            g.FillEllipse(shine, X + 3, Y + 2, 6, 5);
        }

        public void OnHit() => _dy = -_dy;

        public void DeflectFrom(RectangleF paddle)
        {
            float hitPos = (X + Width / 2 - paddle.X) / paddle.Width - 0.5f;
            _dx = _speed * hitPos * 2;
            _dy = -Math.Abs(_dy);
            Y = paddle.Y - Height - 1;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PADDLE — Inherits GameEntity
    // ══════════════════════════════════════════════════════════
    public class Paddle : GameEntity
    {
        public Paddle() : base(300, 520, 100, 12) { }

        public override void Update() { }

        // OVERRIDE — Polymorphism
        public override void Draw(Graphics g)
        {
            using var brush = new LinearGradientBrush(Bounds,
                Color.DodgerBlue, Color.MediumBlue, LinearGradientMode.Vertical);
            g.FillRectangle(brush, Bounds);
        }

        public void MoveTo(float mouseX)
        {
            X = Math.Clamp(mouseX - Width / 2, 0, 700 - Width);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  BRICK (Base) — Inherits GameEntity, Implements ICollidable + IScoreable
    // ══════════════════════════════════════════════════════════
    public class Brick : GameEntity, ICollidable, IScoreable
    {
        private Color _color;
        private int _points;

        public Color BrickColor { get => _color; protected set => _color = value; }

        public Brick(float x, float y, float w, float h, Color color, int points = 10)
            : base(x, y, w, h)
        { _color = color; _points = points; }

        public override void Update() { }

        // OVERRIDE — Polymorphism
        public override void Draw(Graphics g)
        {
            if (!IsActive) return;
            using var brush = new SolidBrush(_color);
            g.FillRectangle(brush, Bounds);
            using var pen = new Pen(Color.FromArgb(80, 0, 0, 0));
            g.DrawRectangle(pen, X, Y, Width, Height);
        }

        // Virtual — can be overridden by subclasses (POLYMORPHISM)
        public virtual void OnHit() => IsActive = false;
        public virtual int GetScore() => _points;
    }

    // ══════════════════════════════════════════════════════════
    //  HARD BRICK — INHERITS Brick (Inheritance + Polymorphism)
    //  Needs 2 hits to break
    // ══════════════════════════════════════════════════════════
    public class HardBrick : Brick
    {
        private int _hp = 2;

        public HardBrick(float x, float y, float w, float h)
            : base(x, y, w, h, Color.Gray, 25) { }

        // OVERRIDE — Polymorphism (different behavior than parent)
        public override void OnHit()
        {
            _hp--;
            if (_hp <= 0) IsActive = false;
            else BrickColor = Color.DarkGray; // crack effect
        }

        // OVERRIDE — Polymorphism (double score)
        public override int GetScore() => base.GetScore() * 2;

        public override void Draw(Graphics g)
        {
            if (!IsActive) return;
            base.Draw(g);
            // Draw HP dots
            using var dot = new SolidBrush(Color.White);
            for (int i = 0; i < _hp; i++)
                g.FillEllipse(dot, X + 5 + i * 10, Y + 3, 5, 5);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  EXPLOSIVE BRICK — INHERITS Brick (Inheritance + Polymorphism)
    //  Destroys neighbors on hit
    // ══════════════════════════════════════════════════════════
    public class ExplosiveBrick : Brick
    {
        private List<Brick>? _allBricks;

        public ExplosiveBrick(float x, float y, float w, float h)
            : base(x, y, w, h, Color.Orange, 50) { }

        public void SetGrid(List<Brick> bricks) => _allBricks = bricks;

        // OVERRIDE — Polymorphism (chain explosion)
        public override void OnHit()
        {
            IsActive = false;
            if (_allBricks == null) return;
            var zone = RectangleF.Inflate(Bounds, Width, Height);
            foreach (var b in _allBricks)
                if (b.IsActive && b != this && b.Bounds.IntersectsWith(zone))
                    b.OnHit();
        }

        // OVERRIDE — Polymorphism (triple score)
        public override int GetScore() => base.GetScore() * 3;

        public override void Draw(Graphics g)
        {
            if (!IsActive) return;
            base.Draw(g);
            using var f = new Font("Arial", 8, FontStyle.Bold);
            using var b = new SolidBrush(Color.White);
            g.DrawString("X", f, b, X + Width / 2 - 5, Y + 2);
        }
    }

    // ══════════════════════════════════════════════════════════
    //  DATABASE SERVICE — SQLite (Database Integration)
    // ══════════════════════════════════════════════════════════
    public record ScoreRecord(string Name, int Score, int Level, string Date);

    public class DatabaseService
    {
        private readonly string _connStr;

        public DatabaseService(string dbPath)
        {
            _connStr = $"Data Source={dbPath}";
            Execute(@"CREATE TABLE IF NOT EXISTS Scores (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PlayerName TEXT NOT NULL,
                Score INTEGER NOT NULL,
                Level INTEGER NOT NULL,
                DatePlayed TEXT NOT NULL)");
        }

        // INSERT score
        public void SaveScore(string name, int score, int level)
        {
            Execute("INSERT INTO Scores(PlayerName,Score,Level,DatePlayed) VALUES(@n,@s,@l,@d)",
                ("@n", name), ("@s", score), ("@l", level),
                ("@d", DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
        }

        // SELECT top scores
        public List<ScoreRecord> GetTopScores()
        {
            var list = new List<ScoreRecord>();
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT PlayerName,Score,Level,DatePlayed FROM Scores ORDER BY Score DESC LIMIT 10";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new ScoreRecord(r.GetString(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3)));
            return list;
        }

        // DELETE all scores
        public void ClearScores() => Execute("DELETE FROM Scores");

        private void Execute(string sql, params (string name, object val)[] parms)
        {
            using var conn = new SqliteConnection(_connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            foreach (var (name, val) in parms)
                cmd.Parameters.AddWithValue(name, val);
            cmd.ExecuteNonQuery();
        }
    }

    // ══════════════════════════════════════════════════════════
    //  GAME FORM — Main GUI (Windows Forms)
    // ══════════════════════════════════════════════════════════
    public class GameForm : Form
    {
        private Ball _ball;
        private Paddle _paddle = new();
        private List<Brick> _bricks = new();
        private DatabaseService _db;
        private System.Windows.Forms.Timer _timer;
        private int _score = 0, _lives = 3, _level = 1;
        private bool _gameOver = false, _scoreSaved = false;

        public GameForm()
        {
            Text = "Brick Breaker Game";
            ClientSize = new Size(700, 560);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 20, 40);

            // Database setup
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BrickBreaker", "scores.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            _db = new DatabaseService(dbPath);

            // Create ball and bricks
            _ball = NewBall();
            BuildLevel();

            // Game timer — 60 FPS
            _timer = new() { Interval = 16 };
            _timer.Tick += GameLoop;
            _timer.Start();

            MouseMove += (_, e) => _paddle.MoveTo(e.X);
            KeyDown += OnKey;
        }

        // ── GAME LOOP ────────────────────────────────────────

        private void GameLoop(object? s, EventArgs e)
        {
            if (_gameOver) return;

            _ball.Update();

            // Ball hits paddle
            if (_ball.DY > 0 && _ball.Bounds.IntersectsWith(_paddle.Bounds))
                _ball.DeflectFrom(_paddle.Bounds);

            // Ball hits bricks
            foreach (var brick in _bricks)
            {
                if (!brick.IsActive) continue;
                if (!_ball.Bounds.IntersectsWith(brick.Bounds)) continue;

                _ball.OnHit();
                bool was = brick.IsActive;
                brick.OnHit(); // Polymorphic call!
                if (!brick.IsActive && was) _score += brick.GetScore(); // Polymorphic!
                break;
            }

            // Ball falls out
            if (_ball.Y > 560)
            {
                _lives--;
                if (_lives <= 0) { _gameOver = true; }
                else _ball = NewBall();
            }

            // Level complete
            if (_bricks.All(b => !b.IsActive))
            {
                _level++;
                _bricks.Clear();
                BuildLevel();
                _ball = NewBall();
            }

            Invalidate();
        }

        // ── DRAWING ──────────────────────────────────────────

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background
            using var bg = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(15, 15, 35), Color.FromArgb(25, 25, 55),
                LinearGradientMode.Vertical);
            g.FillRectangle(bg, ClientRectangle);

            // Draw all objects
            foreach (var brick in _bricks) brick.Draw(g);
            _paddle.Draw(g);
            _ball.Draw(g);

            // HUD
            using var font = new Font("Segoe UI", 12, FontStyle.Bold);
            using var white = new SolidBrush(Color.White);
            g.DrawString($"Score: {_score}   Lives: {_lives}   Level: {_level}", font, white, 10, 5);

            using var hint = new Font("Segoe UI", 9);
            using var gray = new SolidBrush(Color.Gray);
            g.DrawString("Mouse=Move  P=Pause  L=Leaderboard  Esc=Exit", hint, gray, 350, 8);

            // Game Over overlay
            if (_gameOver)
            {
                using var dim = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
                g.FillRectangle(dim, ClientRectangle);

                using var big = new Font("Segoe UI", 40, FontStyle.Bold);
                using var sub = new Font("Segoe UI", 16);
                var sf = new StringFormat { Alignment = StringAlignment.Center };
                g.DrawString("GAME OVER", big, white, 350, 200, sf);
                g.DrawString($"Score: {_score}", sub, white, 350, 270, sf);
                g.DrawString("Press R to Restart", sub, gray, 350, 310, sf);

                if (!_scoreSaved) { SaveScore(); _scoreSaved = true; }
            }
        }

        // ── KEYBOARD INPUT ───────────────────────────────────

        private void OnKey(object? s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) Close();
            if (e.KeyCode == Keys.L) ShowLeaderboard();
            if (e.KeyCode == Keys.R && _gameOver) Restart();
        }

        // ── HELPERS ──────────────────────────────────────────

        private Ball NewBall() => new(_paddle.X + 42, 500, 4f + _level * 0.3f);

        private void BuildLevel()
        {
            var colors = new[] { Color.Red, Color.Orange, Color.Yellow, Color.LimeGreen, Color.DodgerBlue };
            var rng = new Random(_level * 42);

            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 10; col++)
                {
                    float x = 12 + col * 68, y = 40 + row * 26;
                    int roll = rng.Next(100);

                    Brick brick;
                    if (_level >= 2 && roll < 12)
                        brick = new HardBrick(x, y, 62, 20);
                    else if (_level >= 3 && roll < 20)
                        brick = new ExplosiveBrick(x, y, 62, 20);
                    else
                        brick = new Brick(x, y, 62, 20, colors[row], (row + 1) * 10);

                    _bricks.Add(brick);
                }

            foreach (var b in _bricks.OfType<ExplosiveBrick>())
                b.SetGrid(_bricks);
        }

        private void Restart()
        {
            _score = 0; _lives = 3; _level = 1;
            _gameOver = false; _scoreSaved = false;
            _bricks.Clear(); BuildLevel();
            _ball = NewBall();
        }

        private void SaveScore()
        {
            string name = "Player";
            using var dlg = new Form {
                Text = "Save Score", ClientSize = new Size(300, 100),
                FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(20, 20, 50)
            };
            var txt = new TextBox { Text = "Player", Location = new Point(10, 10), Size = new Size(280, 25),
                Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(40, 40, 70), ForeColor = Color.White };
            var btn = new Button { Text = "Save", Location = new Point(110, 50), Size = new Size(80, 30),
                DialogResult = DialogResult.OK, FlatStyle = FlatStyle.Flat,
                BackColor = Color.DodgerBlue, ForeColor = Color.White };
            btn.FlatAppearance.BorderSize = 0;
            dlg.Controls.AddRange(new Control[] { txt, btn });
            dlg.AcceptButton = btn;
            if (dlg.ShowDialog() == DialogResult.OK) name = txt.Text;
            if (string.IsNullOrWhiteSpace(name)) name = "Player";
            _db.SaveScore(name.Trim(), _score, _level);
        }

        private void ShowLeaderboard()
        {
            _timer.Stop();
            var scores = _db.GetTopScores();
            string msg = "=== TOP SCORES ===\n\n";
            for (int i = 0; i < scores.Count; i++)
                msg += $"{i + 1}. {scores[i].Name} — {scores[i].Score} pts (Lvl {scores[i].Level}) [{scores[i].Date}]\n";
            if (scores.Count == 0) msg += "No scores yet!";
            MessageBox.Show(msg, "Leaderboard", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _timer.Start();
        }
    }
}