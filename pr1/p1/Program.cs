using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace FlightSimulatorWPF
{
    public partial class MainWindow : Window
    {
        // Поля класса
        private List<TrajectoryPoint> trajectory = new List<TrajectoryPoint>();
        private DispatcherTimer simulationTimer;
        private double simulationTime = 0;
        private List<Obstacle> obstacles = new List<Obstacle>();
        private List<Pig> pigs = new List<Pig>();
        private bool isSimulating = false;
        private double currentBoost = 1.0;
        private double airDensity = 1.225;
        private double dragCoefficient = 0.47;

        public MainWindow()
        {
            InitializeComponent();
            InitializeSimulationTimer();
            InitializeSimulationObjects();

            canvasTrajectory.SizeChanged += (sender, e) => DrawTrajectory();

        }



        private void InitializeSimulationObjects()
        {
            obstacles = new List<Obstacle>
            {
                new Obstacle("Wood", 30) { X = 20, Width = 5, Height = 10 },
                new Obstacle("Stone", 80) { X = 40, Width = 10, Height = 5 }
            };

            pigs = new List<Pig>
            {
                new Pig("Green Pig", 50) { X = 60, Width = 8, Height = 6 },
                new Pig("Big Pig", 100) { X = 80, Width = 12, Height = 8 }
            };
        }

        private void InitializeSimulationTimer()
        {
            simulationTimer = new DispatcherTimer();
            simulationTimer.Interval = TimeSpan.FromMilliseconds(16);
            simulationTimer.Tick += SimulationTimer_Tick;
        }

        private void BtnSimulate_Click(object sender, RoutedEventArgs e)
        {
            if (isSimulating)
            {
                StopSimulation();
                return;
            }
            StartSimulation();
        }

        private void ShowMainParameters(object sender, RoutedEventArgs e)
        {
            MainParametersPanel.Visibility = Visibility.Visible;
            AirResistancePanel.Visibility = Visibility.Collapsed;
            ObstaclesPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowAirResistance(object sender, RoutedEventArgs e)
        {
            MainParametersPanel.Visibility = Visibility.Collapsed;
            AirResistancePanel.Visibility = Visibility.Visible;
            ObstaclesPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowObstacles(object sender, RoutedEventArgs e)
        {
            MainParametersPanel.Visibility = Visibility.Collapsed;
            AirResistancePanel.Visibility = Visibility.Collapsed;
            ObstaclesPanel.Visibility = Visibility.Visible;
        }

        private void ShowResults(object sender, RoutedEventArgs e)
        {
            DrawTrajectory();
            DisplayResults();
        }

        private void StartSimulation()
        {
            try
            {
                double mass = GetSelectedBirdMass();
                currentBoost = GetSelectedBoostValue();
                double force = slForce.Value * currentBoost;
                double angle = slAngle.Value;
                airDensity = slAirDensity.Value;
                dragCoefficient = slDragCoefficient.Value;

                trajectory = CalculateTrajectory(mass, force, angle);
                simulationTime = 0;
                isSimulating = true;
                btnSimulate.Content = "Остановить";
                simulationTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка симуляции");
            }
        }

        private void StopSimulation()
        {
            simulationTimer.Stop();
            isSimulating = false;
            btnSimulate.Content = "Запустить";
            DrawTrajectory();
        }

        private void ShowImpactResults()
        {
            if (trajectory.Count == 0) return;

            var lastPoint = trajectory.Last();
            double impactForce = CalculateImpactForce(lastPoint);

            var impactResults = new List<string>();
            foreach (var obstacle in obstacles)
            {
                bool destroyed = obstacle.TakeDamage(impactForce);
                impactResults.Add($"Obstacle ({obstacle.Material}): " +
                                $"{(destroyed ? "Уничтожено" : "Повреждено")}");
            }

            foreach (var pig in pigs)
            {
                bool killed = pig.TakeDamage(impactForce);
                impactResults.Add($"{pig.Name}: {(killed ? "Устранено" : "Ранена")}");
            }

            tbResults.Text += "\nImpact Results:\n" + string.Join("\n", impactResults);

            MessageBox.Show($"Simulation Complete!\n\nImpact Force: {impactForce:F2}N\n\n" +
                          "Damage Report:\n" + string.Join("\n", impactResults),
                          "Simulation Results",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private double CalculateImpactForce(TrajectoryPoint point, double contactTime = 0.1)
        {
            return point.Speed / contactTime;
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            simulationTime += 0.05;

            var currentPoint = trajectory.FirstOrDefault(p => p.Time >= simulationTime);
            if (currentPoint == null)
            {
                StopSimulation();
                return;
            }

            if (CheckCollision(currentPoint))
            {
                StopSimulation();
                MessageBox.Show("Столкновение с препятствием!", "Столкновение");
                return;
            }

            DrawRealTimeTrajectory(simulationTime);
        }

        private bool CheckCollision(TrajectoryPoint point)
        {
            foreach (var obstacle in obstacles)
            {
                if (point.X >= obstacle.X && point.X <= obstacle.X + obstacle.Width &&
                    point.Y <= obstacle.Height)
                {
                    return true;
                }
            }

            foreach (var pig in pigs)
            {
                if (point.X >= pig.X && point.X <= pig.X + pig.Width &&
                    point.Y <= pig.Height)
                {
                    return true;
                }
            }
            return false;
        }

        private List<TrajectoryPoint> CalculateTrajectory(double mass, double force, double angle)
        {
            const double g = 9.81;
            const double dt = 0.05;
            var points = new List<TrajectoryPoint>();

            double angleRad = angle * Math.PI / 180;
            double vx = (force / mass) * Math.Cos(angleRad);
            double vy = (force / mass) * Math.Sin(angleRad);
            double x = 0, y = 0, t = 0;
            double crossSectionArea = 0.01;

            while (y >= 0)
            {
                double speed = Math.Sqrt(vx * vx + vy * vy);
                points.Add(new TrajectoryPoint(t, x, y, speed));

                double dragForce = 0.5 * airDensity * speed * speed * dragCoefficient * crossSectionArea;
                double dragAngle = Math.Atan2(vy, vx);
                double dragForceX = dragForce * Math.Cos(dragAngle + Math.PI);
                double dragForceY = dragForce * Math.Sin(dragAngle + Math.PI);

                double ax = dragForceX / mass;
                double ay = -g + dragForceY / mass;

                vx += ax * dt;
                vy += ay * dt;

                x += vx * dt;
                y += vy * dt;

                t += dt;
            }

            return points;
        }

        private void DrawRealTimeTrajectory(double currentTime)
        {
            canvasTrajectory.Children.Clear();
            if (trajectory.Count < 2) return;

            double maxX = trajectory.Max(p => p.X);
            double maxY = trajectory.Max(p => p.Y);
            double scaleX = (canvasTrajectory.ActualWidth - 40) / maxX;
            double scaleY = (canvasTrajectory.ActualHeight - 40) / maxY;
            double scale = Math.Min(scaleX, scaleY) * 0.9;

            DrawAxes(scale);

            DrawObstacles(scale);

            DrawPigs(scale);

            var visiblePoints = trajectory.Where(p => p.Time <= currentTime).ToList();
            if (visiblePoints.Count > 1)
            {
                var polyline = new Polyline
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };

                foreach (var point in visiblePoints)
                {
                    double canvasX = point.X * scale + 20;
                    double canvasY = canvasTrajectory.ActualHeight - point.Y * scale - 20;
                    polyline.Points.Add(new Point(canvasX, canvasY));
                }
                canvasTrajectory.Children.Add(polyline);
            }

            // Рисуем текущую позицию
            var currentPoint = visiblePoints.LastOrDefault();
            if (currentPoint != null)
            {
                double canvasX = currentPoint.X * scale + 20;
                double canvasY = canvasTrajectory.ActualHeight - currentPoint.Y * scale - 20;

                var birdEllipse = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = GetBirdColor(),
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(birdEllipse, canvasX - 5);
                Canvas.SetTop(birdEllipse, canvasY - 5);
                canvasTrajectory.Children.Add(birdEllipse);
            }

            DisplayResults();
        }

        private void DrawPigs(double scale)
        {
            foreach (var pig in pigs)
            {
                double canvasX = pig.X * scale + 20;
                double canvasWidth = pig.Width * scale;
                double canvasHeight = pig.Height * scale;
                double canvasY = canvasTrajectory.ActualHeight - 20;

                var rect = new Rectangle
                {
                    Width = canvasWidth,
                    Height = canvasHeight,
                    Fill = Brushes.Green,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, canvasX);
                Canvas.SetTop(rect, canvasY - canvasHeight);
                canvasTrajectory.Children.Add(rect);

                var label = new TextBlock
                {
                    Text = pig.Name,
                    FontSize = 8,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(label, canvasX + 2);
                Canvas.SetTop(label, canvasY - canvasHeight + 2);
                canvasTrajectory.Children.Add(label);
            }
        }

        private void DrawTrajectory()
        {
            canvasTrajectory.Children.Clear();
            if (trajectory.Count < 2) return;

            // Масштабирование
            double maxX = trajectory.Max(p => p.X);
            double maxY = trajectory.Max(p => p.Y);
            double scaleX = (canvasTrajectory.ActualWidth - 40) / maxX;
            double scaleY = (canvasTrajectory.ActualHeight - 40) / maxY;
            double scale = Math.Min(scaleX, scaleY) * 0.9;

            DrawAxes(scale);
            DrawObstacles(scale);
            DrawPigs(scale);

            // Рисуем всю траекторию
            var polyline = new Polyline
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            };

            foreach (var point in trajectory)
            {
                double canvasX = point.X * scale + 20;
                double canvasY = canvasTrajectory.ActualHeight - point.Y * scale - 20;
                polyline.Points.Add(new Point(canvasX, canvasY));
            }

            canvasTrajectory.Children.Add(polyline);
            DisplayResults();
        }

        private void DrawAxes(double scale)
        {
            // X-axis
            canvasTrajectory.Children.Add(new Line
            {
                X1 = 20,
                Y1 = canvasTrajectory.ActualHeight - 20,
                X2 = canvasTrajectory.ActualWidth - 20,
                Y2 = canvasTrajectory.ActualHeight - 20,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            });

            // Y-axis
            canvasTrajectory.Children.Add(new Line
            {
                X1 = 20,
                Y1 = 20,
                X2 = 20,
                Y2 = canvasTrajectory.ActualHeight - 20,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            });

            // Labels
            var xLabel = new TextBlock { Text = "Distance (m)", FontSize = 10 };
            Canvas.SetLeft(xLabel, canvasTrajectory.ActualWidth / 2 - 30);
            Canvas.SetTop(xLabel, canvasTrajectory.ActualHeight - 15);
            canvasTrajectory.Children.Add(xLabel);

            var yLabel = new TextBlock { Text = "Height (m)", FontSize = 10, RenderTransform = new RotateTransform(-90) };
            Canvas.SetLeft(yLabel, 5);
            Canvas.SetTop(yLabel, canvasTrajectory.ActualHeight / 2 - 30);
            canvasTrajectory.Children.Add(yLabel);
        }

        private void DrawObstacles(double scale)
        {
            foreach (var obstacle in obstacles)
            {
                double canvasX = obstacle.X * scale + 20;
                double canvasWidth = obstacle.Width * scale;
                double canvasHeight = obstacle.Height * scale;
                double canvasY = canvasTrajectory.ActualHeight - 20;

                var rect = new Rectangle
                {
                    Width = canvasWidth,
                    Height = canvasHeight,
                    Fill = obstacle.Material == "Wood" ? Brushes.Brown : Brushes.Gray,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, canvasX);
                Canvas.SetTop(rect, canvasY - canvasHeight);
                canvasTrajectory.Children.Add(rect);

                var label = new TextBlock
                {
                    Text = obstacle.Material,
                    FontSize = 8,
                    Foreground = Brushes.White
                };
                Canvas.SetLeft(label, canvasX + 2);
                Canvas.SetTop(label, canvasY - canvasHeight + 2);
                canvasTrajectory.Children.Add(label);
            }
        }

        private void DisplayResults()
        {
            if (trajectory.Count == 0) return;

            var last = trajectory[trajectory.Count - 1];
            double maxY = trajectory.Max(p => p.Y);
            tbResults.Text = $"Distance: {last.X:F1} m\n" +
                            $"Max height: {maxY:F1} m\n" +
                            $"Flight time: {last.Time:F1} s\n" +
                            $"Boost: x{currentBoost:F1}";
        }

        private double GetSelectedBirdMass()
        {
            switch (cbBirds.SelectedIndex)
            {
                case 0: return 0.7;
                case 1: return 1.0;
                case 2: return 1.5;
                default: return 1.0;
            }
        }

        private double GetSelectedBoostValue()
        {
            switch (cbBoost.SelectedIndex)
            {
                case 0: return 1.0;
                case 1: return 1.2;
                case 2: return 1.5;
                case 3: return 2.0;
                default: return 1.0;
            }
        }

        private Brush GetBirdColor()
        {
            switch (cbBirds.SelectedIndex)
            {
                case 0: return Brushes.Blue;
                case 1: return Brushes.Yellow;
                case 2: return Brushes.Red;
                default: return Brushes.Blue;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            if (trajectory.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта", "Ошибка");
                return;
            }

            var dialog = new SaveFileDialog { Filter = "CSV файлы|*.csv" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Время;X;Y;Скорость");
                        foreach (var point in trajectory)
                        {
                            writer.WriteLine($"{point.Time:F2};{point.X:F2};{point.Y:F2};{point.Speed:F2}");
                        }
                    }
                    MessageBox.Show("Данные успешно экспортированы", "Успех");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
                }
            }
        }

        private void BtnAddObstacle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var obstacle = new Obstacle("Custom", 50)
                {
                    X = double.Parse(txtObstacleX.Text),
                    Width = double.Parse(txtObstacleWidth.Text),
                    Height = double.Parse(txtObstacleHeight.Text)
                };

                obstacles.Add(obstacle);
                DrawTrajectory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления препятствия: {ex.Message}", "Ошибка");
            }
        }

        private void BtnClearObstacles_Click(object sender, RoutedEventArgs e)
        {
            obstacles.Clear();
            DrawTrajectory();
        }
    }

    public class TrajectoryPoint
    {
        public double Time { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }

        public TrajectoryPoint(double time, double x, double y, double speed)
        {
            Time = time;
            X = x;
            Y = y;
            Speed = speed;
        }
    }

    public class Obstacle
    {
        public string Material { get; set; }
        public int Durability { get; private set; }
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        private readonly int initialDurability;

        public Obstacle(string material, int durability)
        {
            Material = material;
            Durability = durability;
            initialDurability = durability;
        }

        public bool TakeDamage(double force)
        {
            Durability -= (int)force;
            return Durability <= 0;
        }

        public void Reset()
        {
            Durability = initialDurability;
        }
    }

    public class Pig
    {
        public string Name { get; set; }
        public int Health { get; private set; }
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        private readonly int initialHealth;

        public Pig(string name, int health)
        {
            Name = name;
            Health = health;
            initialHealth = health;
        }

        public bool TakeDamage(double force)
        {
            Health -= (int)force;
            return Health <= 0;
        }

        public void Reset()
        {
            Health = initialHealth;
        }
    }
}