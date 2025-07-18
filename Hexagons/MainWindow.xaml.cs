using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Hexagons
{
    #region Win32 Interop
    public static class Win32
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    }
    #endregion

    #region Configuration Classes
    public class HexagonConfig
    {
        public double Radius { get; set; } = 50;
        public double Height => Radius * Math.Sqrt(3);

        // Glow Colors (Active)
        public Color GlowColor { get; set; } = Color.FromArgb(180, 100, 200, 255);

        // Passive Colors (Transparent)
        public Color PassiveColor { get; set; } = Color.FromArgb(0, 0, 150, 255);

        // Animation Settings
        public int GlowDurationMs { get; set; } = 250;
        public int WaveSpeedMs { get; set; } = 65;
        public int UpdateDelayMs { get; set; } = 35;

        public int RippleSpeedMs { get; set; } = 20;

        public bool GameMode = false;
    }

    public static class KeyCombinations
    {
        public const int KEY_W = 0x57;
        public const int KEY_S = 0x53;
        public const int KEY_A = 0x41;
        public const int KEY_R = 0x52;
        public const int KEY_T = 0x54;

        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12;    // Alt key
        public const int VK_SHIFT = 0x10;
    }
    #endregion

    public partial class MainWindow : Window
    {
        #region Fields and Properties
        public readonly HexagonConfig _config = new();
        public readonly List<Polygon> _hexagons = new();
        private readonly List<List<Polygon>> _hexagonColumns = new();

        private MouseHook _mouseHook;
        private KeyboardHook _keyboardHook;
        private DispatcherTimer _holdTimer;
        private DispatcherTimer _waveTimer;

        private bool _isMouseDown = false;
        private Point _currentMousePosition;
        private bool _isWaveActive = false;
        private int _currentWaveColumn = 0;
        public int _resetHexagonsAnimation = 0;
        public int _closeToolsAnimation = 0;
        #endregion

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();
            InitializeWindow();
            InitializeTimers();

            Loaded += OnWindowLoaded;
        }

        private void InitializeWindow()
        {
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            WindowState = WindowState.Maximized;
            ShowInTaskbar = false;
            Focusable = true;

            KeyDown += OnKeyDown;
        }

        private void InitializeTimers()
        {
            _holdTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.UpdateDelayMs)
            };
            _holdTimer.Tick += OnHoldTimerTick;

            _waveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_config.WaveSpeedMs)
            };
            _waveTimer.Tick += OnWaveTimerTick;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            MakeWindowClickThrough();
            DrawHexagonGrid();
            SetupInputHooks();

            this.Focus();
            this.Activate();

            Closed += OnWindowClosed;
        }

        private void MakeWindowClickThrough()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE,
                extendedStyle | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT);
        }

        private void SetupInputHooks()
        {
            _mouseHook = new MouseHook();
            _mouseHook.LeftButtonDown += OnMouseDown;
            _mouseHook.LeftButtonUp += OnMouseUp;
            _mouseHook.MouseMove += OnMouseMove;
            _mouseHook.Hook();

            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyDown += OnGlobalKeyDown;
            _keyboardHook.Hook();
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _holdTimer?.Stop();
            _waveTimer?.Stop();
            _mouseHook?.Unhook();
            _keyboardHook?.Unhook();
        }
        #endregion

        #region Input Event Handlers
        private void OnMouseDown(Point screenPoint)
        {
            if (!_config.GameMode)
            {
                try
                {
                    Debug.WriteLine($"Mouse down at: {screenPoint.X}, {screenPoint.Y}");
                    _isMouseDown = true;
                    _currentMousePosition = screenPoint;

                    TriggerGlowAtPosition(screenPoint);
                    _holdTimer.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in mouse down: {ex.Message}");
                }
            }

        }

        private void OnMouseUp(Point screenPoint)
        {
            try
            {
                Debug.WriteLine($"Mouse up at: {screenPoint.X}, {screenPoint.Y}");
                _isMouseDown = false;
                _holdTimer.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in mouse up: {ex.Message}");
            }
        }

        private void OnMouseMove(Point screenPoint)
        {
            try
            {
                if (_isMouseDown)
                {
                    _currentMousePosition = screenPoint;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in mouse move: {ex.Message}");
            }
        }

        private void OnGlobalKeyDown(int vkCode, bool ctrl, bool alt, bool shift)
        {
            // Ctrl+Alt+Shift+W: Start wave animation
            if (vkCode == KeyCombinations.KEY_W && ctrl && alt && shift)
            {
                Application.Current.Dispatcher.BeginInvoke(StartWaveAnimation);
            }
            // Ctrl+Shift+T: Open Tools
            else if (vkCode == KeyCombinations.KEY_T && ctrl && shift)
            {
                OpenTools();
            }
            // Ctrl+Shift+A: Animate all hexagons
            else if (vkCode == KeyCombinations.KEY_A && ctrl && shift)
            {
                AnimateAllHexagons();
            }
            // Ctrl+Shift+S: Animate Some hexagons
            else if (vkCode == KeyCombinations.KEY_S && ctrl && shift)
            {
                AnimateSomeHexagons();
            }
            // Ctrl+Shift+R: Start Ripple animation
            else if (vkCode == KeyCombinations.KEY_R && ctrl && shift)
            {
                StartRipple(new Point(
    SystemParameters.PrimaryScreenWidth / 2,
    SystemParameters.PrimaryScreenHeight / 2));
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W &&
                Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))
            {
                StartWaveAnimation();
            }
        }

        private void OnHoldTimerTick(object sender, EventArgs e)
        {
            if (_isMouseDown)
            {
                TriggerGlowAtPosition(_currentMousePosition);
            }
        }
        #endregion

        #region Animation Methods
        public void StartWaveAnimation()
        {
            if (_isWaveActive) return;

            try
            {
                Debug.WriteLine("Starting wave animation");
                _waveTimer.Interval = TimeSpan.FromMilliseconds(_config.WaveSpeedMs);
                _isWaveActive = true;
                _currentWaveColumn = 0;
                _waveTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting wave: {ex.Message}");
            }
        }

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

        private void TriggerGlowAtPosition(Point screenPoint)
        {
            try
            {
                Point canvasPoint = MainCanvas.PointFromScreen(screenPoint);

                foreach (var hex in _hexagons.Where(h => IsPointInHexagon(h.Points, canvasPoint)))
                {
                    AnimateHexagonGlow(hex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering glow: {ex.Message}");
            }
        }

        private void AnimateHexagonGlow(Polygon hex)
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
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating glow: {ex.Message}");
            }
        }

        public void AnimateAllHexagons()
        {
            foreach (var hex in _hexagons)
            {
                AnimateHexagonGlow(hex);
            }
        }

        private void AnimateSomeHexagons()
        {
            var random = new Random();
            int count = _hexagons.Count / 2;

            for (int i = 0; i < count; i++)
            {
                var randomHex = _hexagons[random.Next(_hexagons.Count)];
                AnimateHexagonGlow(randomHex);
            }
        }

        public void StartRipple(Point origin)
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

                foreach (var hex in _hexagons)
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

        #endregion

        #region Hexagon Grid Creation
        public void DrawHexagonGrid()
        {
            try
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                Debug.WriteLine($"Drawing grid - Screen: {screenWidth}x{screenHeight}");

                ClearGrid();

                var spacing = CalculateHexagonSpacing();
                CreateHexagonColumns(screenWidth, spacing.horizontal);
                PopulateHexagonGrid(screenWidth, screenHeight, spacing);

                Debug.WriteLine($"Created {_hexagons.Count} hexagons in {_hexagonColumns.Count} columns");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing grid: {ex.Message}");
            }
        }

        private void ClearGrid()
        {
            MainCanvas.Children.Clear();
            _hexagons.Clear();
            _hexagonColumns.Clear();
        }

        private (double horizontal, double vertical) CalculateHexagonSpacing()
        {
            return (
                horizontal: _config.Radius * 1.5,
                vertical: _config.Height * 0.75
            );
        }

        private void CreateHexagonColumns(double screenWidth, double horizontalSpacing)
        {
            int numColumns = (int)Math.Ceiling((screenWidth + 2 * _config.Radius) / horizontalSpacing) + 1;
            for (int i = 0; i < numColumns; i++)
            {
                _hexagonColumns.Add(new List<Polygon>());
            }
        }

        private void PopulateHexagonGrid(double screenWidth, double screenHeight,
            (double horizontal, double vertical) spacing)
        {
            int columnIndex = 0;

            for (double x = -_config.Radius; x < screenWidth + 2 * _config.Radius; x += spacing.horizontal)
            {
                for (double y = -_config.Height; y < screenHeight + _config.Height; y += spacing.vertical)
                {
                    var hexPosition = CalculateHexagonPosition(x, y, spacing);
                    var hex = CreateHexagon(hexPosition.x, hexPosition.y);

                    _hexagons.Add(hex);
                    MainCanvas.Children.Add(hex);

                    if (columnIndex < _hexagonColumns.Count)
                    {
                        _hexagonColumns[columnIndex].Add(hex);
                    }
                }
                columnIndex++;
            }
        }

        private (double x, double y) CalculateHexagonPosition(double x, double y,
            (double horizontal, double vertical) spacing)
        {
            int rowIndex = (int)(y / spacing.vertical);
            bool isOddRow = rowIndex % 2 == 1;

            return (
                x: x + (isOddRow ? spacing.horizontal * 0.5 : 0),
                y: y
            );
        }

        private Polygon CreateHexagon(double centerX, double centerY)
        {
            var hex = new Polygon
            {
                Points = GenerateHexagonPoints(centerX, centerY),
                Stroke = Brushes.Transparent,
                StrokeThickness = 0,
                Fill = Brushes.Transparent,
                Tag = false
            };

            return hex;
        }

        private PointCollection GenerateHexagonPoints(double centerX, double centerY)
        {
            var points = new PointCollection();

            for (int i = 0; i < 6; i++)
            {
                double angleDegrees = 60 * i - 30;
                double angleRadians = Math.PI / 180 * angleDegrees;

                double x = centerX + _config.Radius * Math.Cos(angleRadians);
                double y = centerY + _config.Radius * Math.Sin(angleRadians);

                points.Add(new Point(x, y));
            }

            return points;
        }
        #endregion

        #region Utility Methods
        private bool IsPointInHexagon(PointCollection polygon, Point testPoint)
        {
            int n = polygon.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point pi = polygon[i];
                Point pj = polygon[j];

                bool intersect = ((pi.Y > testPoint.Y) != (pj.Y > testPoint.Y)) &&
                                (testPoint.X < (pj.X - pi.X) * (testPoint.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);

                if (intersect)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private void OpenTools()
        {
            switch (_closeToolsAnimation)
            {
                case 1:
                    AnimateAllHexagons();
                    break;
                case 2:
                    StartRipple(new Point(
    SystemParameters.PrimaryScreenWidth / 2,
    SystemParameters.PrimaryScreenHeight / 2));
                    break;
                default:
                    StartWaveAnimation();
                    break;
            }
            var tools = new Tools(this)
            {
                Topmost = true
            };
            tools.ShowDialog();
            switch (_closeToolsAnimation)
            {
                case 1:
                    AnimateAllHexagons();
                    break;
                case 2:
                    StartRipple(new Point(
    SystemParameters.PrimaryScreenWidth / 2,
    SystemParameters.PrimaryScreenHeight / 2));
                    break;
                default:
                    StartWaveAnimation();
                    break;
            }
        }

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

        #endregion
    }

    #region Input Hook Classes
    public class MouseHook
    {
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event Action<Point> LeftButtonDown;
        public event Action<Point> LeftButtonUp;
        public event Action<Point> MouseMove;

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void Hook()
        {
            _hookID = SetHook(_proc);
        }

        public void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(14 /*WH_MOUSE_LL*/, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var mousePoint = new Point(hookStruct.pt.x, hookStruct.pt.y);

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        switch ((int)wParam)
                        {
                            case 0x0201: // WM_LBUTTONDOWN
                                LeftButtonDown?.Invoke(mousePoint);
                                break;
                            case 0x0202: // WM_LBUTTONUP
                                LeftButtonUp?.Invoke(mousePoint);
                                break;
                            case 0x0200: // WM_MOUSEMOVE
                                MouseMove?.Invoke(mousePoint);
                                break;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in mouse hook: {ex.Message}");
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Mouse Hook WinAPI
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }

    public class KeyboardHook
    {
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        public event Action<int, bool, bool, bool> KeyDown;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        public void Hook()
        {
            _hookID = SetHook(_proc);
        }

        public void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(13 /*WH_KEYBOARD_LL*/, proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)0x0100) // WM_KEYDOWN
            {
                try
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    bool ctrl = (GetAsyncKeyState(KeyCombinations.VK_CONTROL) & 0x8000) != 0;
                    bool alt = (GetAsyncKeyState(KeyCombinations.VK_MENU) & 0x8000) != 0;
                    bool shift = (GetAsyncKeyState(KeyCombinations.VK_SHIFT) & 0x8000) != 0;

                    KeyDown?.Invoke(vkCode, ctrl, alt, shift);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in keyboard hook: {ex.Message}");
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region Keyboard Hook WinAPI
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        #endregion
    }
    #endregion
}