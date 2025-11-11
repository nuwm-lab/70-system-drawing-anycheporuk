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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }

    public class GraphForm : Form
    {
        private const double tStart = 0.2;
        private const double tEnd = 0.8;
        private const double tStep = 0.1;
        private readonly Padding plotMargin = new Padding(50, 30, 30, 50); // відступи від країв

        public GraphForm()
        {
            this.Text = "Графік y = (tan(2t) - 3t) / (t + 3)";
            this.BackColor = Color.White;
            this.MinimumSize = new Size(400, 300);
            this.DoubleBuffered = true;
            this.Resize += (s, e) => Invalidate();
            this.Paint += DrawGraph;
        }

        private void DrawGraph(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // обчислюємо точки
            List<KeyValuePair<double, double>> points = ComputeFunctionPoints();

            // знаходимо мінімум і максимум по Y
            double minY = double.PositiveInfinity, maxY = double.NegativeInfinity;
            foreach (var p in points)
            {
                if (p.Value < minY) minY = p.Value;
                if (p.Value > maxY) maxY = p.Value;
            }

            if (Math.Abs(maxY - minY) < 1e-6)
            {
                minY -= 1;
                maxY += 1;
            }

            Rectangle plotRect = new Rectangle(
                plotMargin.Left,
                plotMargin.Top,
                this.ClientSize.Width - plotMargin.Left - plotMargin.Right,
                this.ClientSize.Height - plotMargin.Top - plotMargin.Bottom
            );

            // фон
            using (var bg = new SolidBrush(Color.FromArgb(250, 250, 250)))
                g.FillRectangle(bg, plotRect);

            // осі
            DrawAxes(g, plotRect, minY, maxY);

            // функція для перетворення координат
            PointF ToPixel(double t, double y)
            {
                float x = (float)Map(t, tStart, tEnd, plotRect.Left, plotRect.Right);
                float ypx = (float)Map(y, minY, maxY, plotRect.Bottom, plotRect.Top);
                return new PointF(x, ypx);
            }

            // малюємо лінію
            using (var pen = new Pen(Color.Blue, 2))
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    PointF p1 = ToPixel(points[i].Key, points[i].Value);
                    PointF p2 = ToPixel(points[i + 1].Key, points[i + 1].Value);
                    g.DrawLine(pen, p1, p2);
                }
            }

            // маркери точок
            using (var brush = new SolidBrush(Color.DarkBlue))
            using (var font = new Font("Segoe UI", 8))
            {
                foreach (var p in points)
                {
                    PointF pt = ToPixel(p.Key, p.Value);
                    g.FillEllipse(brush, pt.X - 3, pt.Y - 3, 6, 6);
                    string label = $"t={p.Key:0.0}";
                    SizeF sz = g.MeasureString(label, font);
                    g.DrawString(label, font, brush, pt.X - sz.Width / 2, plotRect.Bottom + 2);
                }
            }

            // назва функції
            using (var font = new Font("Segoe UI", 9))
            using (var brush = new SolidBrush(Color.Black))
            {
                g.DrawString("y = (tan(2t) - 3t) / (t + 3)", font, brush, plotRect.Left, 4);
            }
        }

        private void DrawAxes(Graphics g, Rectangle rect, double minY, double maxY)
        {
            using (var axisPen = new Pen(Color.Black, 1))
            {
                g.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                g.DrawLine(axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
            }

            using (var gridPen = new Pen(Color.LightGray, 1))
            using (var textBrush = new SolidBrush(Color.Black))
            using (var font = new Font("Segoe UI", 8))
            {
                int stepsT = (int)Math.Round((tEnd - tStart) / tStep);
                for (int i = 0; i <= stepsT; i++)
                {
                    double t = tStart + i * tStep;
                    float x = (float)Map(t, tStart, tEnd, rect.Left, rect.Right);
                    g.DrawLine(gridPen, x, rect.Top, x, rect.Bottom);
                    string label = t.ToString("0.0");
                    SizeF sz = g.MeasureString(label, font);
                    g.DrawString(label, font, textBrush, x - sz.Width / 2, rect.Bottom + 2);
                }

                int yTicks = 6;
                for (int i = 0; i <= yTicks; i++)
                {
                    double y = Map(i, 0, yTicks, minY, maxY);
                    float ypx = (float)Map(y, minY, maxY, rect.Bottom, rect.Top);
                    g.DrawLine(gridPen, rect.Left, ypx, rect.Right, ypx);
                    string yLabel = y.ToString("0.###");
                    SizeF sz = g.MeasureString(yLabel, font);
                    g.DrawString(yLabel, font, textBrush, rect.Left - sz.Width - 6, ypx - sz.Height / 2);
                }
            }
        }

        private List<KeyValuePair<double, double>> ComputeFunctionPoints()
        {
            var list = new List<KeyValuePair<double, double>>();
            for (double t = tStart; t <= tEnd + 1e-9; t = Math.Round(t + tStep, 10))
            {
                double y = Evaluate(t);
                list.Add(new KeyValuePair<double, double>(Math.Round(t, 1), y));
            }
            return list;
        }

        private double Evaluate(double t)
        {
            double numerator = Math.Tan(2 * t) - 3 * t;
            double denominator = t + 3;
            return numerator / denominator;
        }

        private static double Map(double a, double aMin, double aMax, double bMin, double bMax)
        {
            if (Math.Abs(aMax - aMin) < 1e-12)
                return (bMin + bMax) / 2;
            return bMin + (a - aMin) * (bMax - bMin) / (aMax - aMin);
        }
    }
}
