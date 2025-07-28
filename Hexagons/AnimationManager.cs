using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Hexagons
{
    public class AnimationManager
    {
        private readonly HexagonConfig _config;
        private DispatcherTimer _waveTimer;
        private bool _isWaveActive = false;
        private int _currentWaveColumn = 0;

        public AnimationManager(HexagonConfig config)
        {
            _config = config;
            InitializeWaveTimer();
        }

        private void InitializeWaveTimer()
        {
            _waveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.WaveSpeedMs)
            };
            _waveTimer.Tick += OnWaveTimerTick;
        }

        private readonly HashSet<Polygon> _continuouslyRotatingHexagons = new HashSet<Polygon>();

        public void UpdateTimerIntervals()
        {
            if (_waveTimer != null)
            {
                bool wasRunning = _waveTimer.IsEnabled;
                _waveTimer.Stop();
                _waveTimer.Interval = TimeSpan.FromMilliseconds(_config.WaveSpeedMs);
                if (wasRunning)
                {
                    _waveTimer.Start();
                }
            }
        }

        public void UpdateRotationSpeed()
        {
            // Restart all continuously rotating hexagons with new speed
            var rotatingHexagons = new List<Polygon>(_continuouslyRotatingHexagons);
            foreach (var hex in rotatingHexagons)
            {
                StopContinuousRotation(hex);
                StartContinuousRotation(hex);
            }
        }

        public void StartWaveAnimation(List<List<Polygon>> hexagonColumns)
        {
            if (_isWaveActive) return;

            try
            {
                Debug.WriteLine("Starting wave animation");
                _waveTimer.Interval = TimeSpan.FromMilliseconds(_config.WaveSpeedMs);
                _isWaveActive = true;
                _currentWaveColumn = 0;
                _hexagonColumns = hexagonColumns;
                _waveTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting wave: {ex.Message}");
            }
        }

        private List<List<Polygon>> _hexagonColumns;

        private void OnWaveTimerTick(object sender, EventArgs e)
        {
            try
            {
                if (_currentWaveColumn >= _hexagonColumns.Count)
                {
                    StopWaveAnimation();
                    return;
                }

                var column = _hexagonColumns[_currentWaveColumn];
                foreach (var hex in column)
                {
                    AnimateHexagonGlow(hex);
                }

                Debug.WriteLine($"Animated column {_currentWaveColumn} ({column.Count} hexagons)");
                _currentWaveColumn++;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in wave timer: {ex.Message}");
                StopWaveAnimation();
            }
        }

        private void StopWaveAnimation()
        {
            _waveTimer.Stop();
            _isWaveActive = false;
            _currentWaveColumn = 0;
            Debug.WriteLine("Wave animation completed");
        }

        public void AnimateHexagonGlow(Polygon hex)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var animationBrush = new SolidColorBrush(_config.PassiveColor);
                    hex.Fill = animationBrush;

                    var glowAnimation = new ColorAnimation
                    {
                        From = _config.PassiveColor,
                        To = _config.GlowColor,
                        Duration = TimeSpan.FromMilliseconds(_config.GlowDurationMs),
                        AutoReverse = true,
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, glowAnimation);

                    // Add rotation if enabled
                    if (_config.RotationEnabled)
                    {
                        AnimateHexagonRotation(hex, _config.GlowDurationMs * 2); // Match glow duration
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating glow: {ex.Message}");
            }
        }

        public void ToggleHexGlow(Polygon hex)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var animationBrush = new SolidColorBrush(_config.PassiveColor);
                    hex.Fill = animationBrush;

                    var glowAnimation = new ColorAnimation
                    {
                        From = _config.PassiveColor,
                        To = _config.GlowColor,
                        Duration = TimeSpan.FromMilliseconds(_config.GlowDurationMs / 2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                    };

                    animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, glowAnimation);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating glow: {ex.Message}");
            }
        }

        public void UnToggleHexGlow(Polygon hex)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var animationBrush = new SolidColorBrush(_config.PassiveColor);
                    hex.Fill = animationBrush;

                    var glowAnimation = new ColorAnimation
                    {
                        From = _config.GlowColor,
                        To = _config.PassiveColor,
                        Duration = TimeSpan.FromMilliseconds(_config.GlowDurationMs / 2),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };

                    animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, glowAnimation);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating glow: {ex.Message}");
            }
        }

        private void AnimateHexagonRotation(Polygon hex, double duration = 1000)
        {
            if (!_config.RotationEnabled || hex.RenderTransform == null) return;

            try
            {
                var rotateTransform = hex.RenderTransform as RotateTransform;
                if (rotateTransform == null) return;

                var rotationAnimation = new DoubleAnimation
                {
                    From = rotateTransform.Angle,
                    To = rotateTransform.Angle + 360, // Full rotation
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };

                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating rotation: {ex.Message}");
            }
        }

        public void StartContinuousRotation(Polygon hex)
        {
            if (!_config.RotationEnabled) return;
            if (hex.RenderTransform == null) return;

            try
            {
                var rotateTransform = hex.RenderTransform as RotateTransform;
                if (rotateTransform == null) return;

                // Add to tracking set
                _continuouslyRotatingHexagons.Add(hex);

                // Create infinite rotation animation based on time, not frames
                var rotationAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(60.0 / _config.RotationSpeed), // Speed = rotations per minute
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = null // Linear rotation
                };

                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotationAnimation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting continuous rotation: {ex.Message}");
            }
        }

        public void StopContinuousRotation(Polygon hex)
        {
            // Remove from tracking set
            _continuouslyRotatingHexagons.Remove(hex);

            if (hex.RenderTransform is RotateTransform rotateTransform)
            {
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, null);
            }
        }

        public void AnimateAllHexagons(List<Polygon> hexagons)
        {
            foreach (var hex in hexagons)
            {
                AnimateHexagonGlow(hex);
            }
        }

        public void AnimateSomeHexagons(List<Polygon> hexagons)
        {
            var random = new Random();
            int count = hexagons.Count / 2;

            for (int i = 0; i < count; i++)
            {
                var randomHex = hexagons[random.Next(hexagons.Count)];
                AnimateHexagonGlow(randomHex);
            }
        }

        public void StartRipple(Point origin, List<Polygon> hexagons)
        {
            double maxRadius = Math.Sqrt(SystemParameters.PrimaryScreenWidth * SystemParameters.PrimaryScreenWidth +
                                         SystemParameters.PrimaryScreenHeight * SystemParameters.PrimaryScreenHeight);

            double step = 20; // how fast the ring grows (pixels per frame)
            double currentRadius = 0;

            HashSet<Polygon> alreadyGlowing = new();

            DispatcherTimer rippleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.RippleSpeedMs) // Now uses dedicated ripple timing
            };

            rippleTimer.Tick += (s, e) =>
            {
                currentRadius += step;

                foreach (var hex in hexagons)
                {
                    Point hexCenter = GetPolygonCenter(hex);
                    double distance = GetDistance(origin, hexCenter);

                    if (distance <= currentRadius && !alreadyGlowing.Contains(hex))
                    {
                        AnimateHexagonGlow(hex);
                        alreadyGlowing.Add(hex);
                    }
                }

                if (currentRadius >= maxRadius)
                {
                    rippleTimer.Stop();
                }
            };

            rippleTimer.Start();
        }

        public void StopAllAnimations()
        {
            _waveTimer?.Stop();
        }

        // Utility methods
        private Point GetPolygonCenter(Polygon poly)
        {
            double sumX = 0, sumY = 0;
            foreach (var p in poly.Points)
            {
                sumX += p.X;
                sumY += p.Y;
            }
            return new Point(sumX / poly.Points.Count, sumY / poly.Points.Count);
        }

        private double GetDistance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}