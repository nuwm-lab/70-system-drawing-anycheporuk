using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace GraphApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new GraphForm());
        }
    }

    public class GraphForm : Form
    {
        // ----------------------------
        // Константи та поля
        // ----------------------------
        private const double TStart = 0.2;
        private const double TEnd = 0.8;
        private const double TStep = 0.1;
        private readonly Padding _plotMargin = new(50, 30, 30, 50);

        private readonly List<KeyValuePair<double, double>> _points;
        private readonly Pen _linePen = new(Color.Blue, 2);
        private readonly Pen _axisPen = new(Color.Black, 1);
        private readonly Pen _gridPen = new(Color.LightGray, 1);
        private readonly Brush _bgBrush = new SolidBrush(Color.FromArgb(250, 250, 250));
        private readonly Brush _textBrush = new SolidBrush(Color.Black);
        private readonly Brush _pointBrush = new SolidBrush(Color.DarkBlue);
        private readonly Font _fontSmall = new("Segoe UI", 8);
        private readonly Font _fontNormal = new("Segoe UI", 9);

        private string _graphMode = "Line"; // "Line" або "Points"

        // ----------------------------
        // Конструктор
        // ----------------------------
        public GraphForm()
        {
            Text = "Графік y = (tan(2t) - 3t) / (t + 3)";
            BackColor = Color.White;
            MinimumSize = new Size(400, 300);
            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            // створюємо попередньо дані
            _points = ComputePoints();

            // елементи керування
            var combo = new ComboBox
            {
                Location = new Point(10, 5),
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            combo.Items.AddRange(new[] { "Line", "Points" });
            combo.SelectedIndex = 0;
            combo.SelectedIndexChanged += (s, e) =>
            {
                _graphMode = combo.SelectedItem!.ToString()!;
                Invalidate();
            };
            Controls.Add(combo);

            Resize += (s, e) => Invalidate();
            Paint += OnPaintGraph;
        }

        // ----------------------------
        // Основне малювання
        // ----------------------------
        private void OnPaintGraph(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle plotRect = new(
                _plotMargin.Left,
                _plotMargin.Top + 25,
                ClientSize.Width - _plotMargin.Left - _plotMargin.Right,
                ClientSize.Height - _plotMargin.Top - _plotMargin.Bottom
            );

            if (plotRect.Width <= 0 || plotRect.Height <= 0)
                return;

            // фон
            g.FillRectangle(_bgBrush, plotRect);

            // межі по Y
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
            foreach (var p in _points)
            {
                if (p.Value < minY) minY = p.Value;
                if (p.Value > maxY) maxY = p.Value;
            }
            if (Math.Abs(maxY - minY) < 1e-6) { minY -= 1; maxY += 1; }

            // осі + сітка
            DrawAxesAndGrid(g, plotRect, minY, maxY);

            // функція для мапінгу координат
            PointF ToPixel(double t, double y)
            {
                float x = (float)Map(t, TStart, TEnd, plotRect.Left, plotRect.Right);
                float ypx = (float)Map(y, minY, maxY, plotRect.Bottom, plotRect.Top);
                return new PointF(x, ypx);
            }

            // малювання графіка
            if (_graphMode == "Line")
            {
                for (int i = 0; i < _points.Count - 1; i++)
                {
                    g.DrawLine(_linePen,
                        ToPixel(_points[i].Key, _points[i].Value),
                        ToPixel(_points[i + 1].Key, _points[i + 1].Value));
                }
            }

            // малювання точок
            foreach (var p in _points)
            {
                PointF pt = ToPixel(p.Key, p.Value);
                g.FillEllipse(_pointBrush, pt.X - 3, pt.Y - 3, 6, 6);
            }

            // підписи до точок по осі t
            foreach (var p in _points)
            {
                PointF pt = ToPixel(p.Key, p.Value);
                string label = $"t={p.Key:0.0}";
                SizeF sz = g.MeasureString(label, _fontSmall);
                g.DrawString(label, _textBrush, pt.X - sz.Width / 2, plotRect.Bottom + 2);
            }

            // назва функції
            g.DrawString("y = (tan(2t) - 3t) / (t + 3)", _fontNormal, _textBrush, plotRect.Left, 4);
        }

        // ----------------------------
        // Малювання осей і сітки
        // ----------------------------
        private void DrawAxesAndGrid(Graphics g, Rectangle rect, double minY, double maxY)
        {
            g.DrawLine(_axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
            g.DrawLine(_axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);

            int stepsT = (int)Math.Round((TEnd - TStart) / TStep);
            for (int i = 0; i <= stepsT; i++)
            {
                double t = TStart + i * TStep;
                float x = (float)Map(t, TStart, TEnd, rect.Left, rect.Right);
                g.DrawLine(_gridPen, x, rect.Top, x, rect.Bottom);
                string label = t.ToString("0.0");
                SizeF sz = g.MeasureString(label, _fontSmall);
                g.DrawString(label, _textBrush, x - sz.Width / 2, rect.Bottom + 2);
            }

            int yTicks = 6;
            for (int i = 0; i <= yTicks; i++)
            {
                double y = Map(i, 0, yTicks, minY, maxY);
                float ypx = (float)Map(y, minY, maxY, rect.Bottom, rect.Top);
                g.DrawLine(_gridPen, rect.Left, ypx, rect.Right, ypx);
                string yLabel = y.ToString("0.###");
                SizeF sz = g.MeasureString(yLabel, _fontSmall);
                g.DrawString(yLabel, _textBrush, rect.Left - sz.Width - 6, ypx - sz.Height / 2);
            }
        }

        // ----------------------------
        // Обчислення функції
        // ----------------------------
        private static List<KeyValuePair<double, double>> ComputePoints()
        {
            var list = new List<KeyValuePair<double, double>>();
            for (double t = TStart; t <= TEnd + 1e-9; t = Math.Round(t + TStep, 10))
            {
                double y = Evaluate(t);
                if (!double.IsFinite(y)) continue; // ігноруємо проблемні точки
                list.Add(new KeyValuePair<double, double>(Math.Round(t, 1), y));
            }
            return list;
        }

        private static double Evaluate(double t)
        {
            double denominator = t + 3;
            if (Math.Abs(denominator) < 1e-8) return double.NaN;
            return (Math.Tan(2 * t) - 3 * t) / denominator;
        }

        // ----------------------------
        // Мапінг координат
        // ----------------------------
        private static double Map(double a, double aMin, double aMax, double bMin, double bMax)
        {
            if (Math.Abs(aMax - aMin) < 1e-12) return (bMin + bMax) / 2;
            return bMin + (a - aMin) * (bMax - bMin) / (aMax - aMin);
        }

        // ----------------------------
        // Звільнення ресурсів
        // ----------------------------
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _linePen.Dispose();
                _axisPen.Dispose();
                _gridPen.Dispose();
                _bgBrush.Dispose();
                _textBrush.Dispose();
                _pointBrush.Dispose();
                _fontSmall.Dispose();
                _fontNormal.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
