﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
<<<<<<< HEAD
=======
using System.Security.Cryptography.Pkcs;
>>>>>>> Develop
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Hexagons
{
<<<<<<< HEAD
=======
    #region Win32 Interop
>>>>>>> Develop
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
<<<<<<< HEAD

    public partial class MainWindow : Window
    {
        private double hexRadius = 50;
        private double hexHeight;
        private List<Polygon> hexagons = new();
        private MouseHook mouseHook;
        public KeyboardHook keyboardHook;
        private DispatcherTimer holdTimer;
        private DispatcherTimer waveTimer;
        private bool isMouseDown = false;
        private Point currentMousePosition;
        private bool isWaveActive = false;
        private int currentWaveColumn = 0;
        private List<List<Polygon>> hexagonColumns = new();

        //customization
        public int hexGlowTime = 250;

        // Updated wave animation duration property
        private int _waveAnimDuration = 65;
        public int waveAnimDuration
        {
            get => _waveAnimDuration;
            set
            {
                _waveAnimDuration = value;
                if (waveTimer != null)
                    waveTimer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }

        private int _UpdateDelay = 35;
        public int UpdateDelay
        {
            get => _UpdateDelay;
            set
            {
                _UpdateDelay = value;
                if (waveTimer != null)
                    holdTimer.Interval = TimeSpan.FromMilliseconds(value); // Check every 20ms-50ms for smooth animation
            }
        }

        //Gloom Hexagon Colors
        public byte hexagonA = 180;
        public byte hexagonR = 100;
        public byte hexagonG = 200;
        public byte hexagonB = 255;

        //Passive Hexagon Colors
        public byte hexagonAP = 0;
        public byte hexagonRP = 0;
        public byte hexagonGP = 150;
        public byte hexagonBP = 255;

        //other customization
        

        public MainWindow()
        {
            InitializeComponent();
            //initialize customization variables

            // Calculate hex height first
            hexHeight = hexRadius * Math.Sqrt(3);

            // Set window properties
=======
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
>>>>>>> Develop
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            WindowState = WindowState.Maximized;
            ShowInTaskbar = false;
<<<<<<< HEAD

            // Enable keyboard input
            Focusable = true;
            KeyDown += MainWindow_KeyDown;

            // Initialize hold timer
            holdTimer = new DispatcherTimer();
            holdTimer.Interval = TimeSpan.FromMilliseconds(UpdateDelay); // Check every 50ms for smooth animation
            holdTimer.Tick += HoldTimer_Tick;

            // Initialize wave timer
            waveTimer = new DispatcherTimer();
            waveTimer.Interval = TimeSpan.FromMilliseconds(waveAnimDuration); // Wave speed - adjust for faster/slower wave
            waveTimer.Tick += WaveTimer_Tick;

            // Wait for window to be fully loaded before setting transparency
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Make window click-through
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = Win32.GetWindowLong(hwnd, Win32.GWL_EXSTYLE);
            Win32.SetWindowLong(hwnd, Win32.GWL_EXSTYLE, extendedStyle | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT);

            DrawHexGrid();

            // Set up mouse hook
            mouseHook = new MouseHook();
            mouseHook.LeftButtonDown += MouseHook_LeftButtonDown;
            mouseHook.LeftButtonUp += MouseHook_LeftButtonUp;
            mouseHook.MouseMove += MouseHook_MouseMove;
            mouseHook.Hook();

            // Set up keyboard hook for global key detection
            keyboardHook = new KeyboardHook();
            keyboardHook.KeyDown += KeyboardHook_KeyDown;
            keyboardHook.Hook();

            // Force focus and keep it
            this.Focus();
            this.Activate();

            Closed += (s, e) => {
                holdTimer?.Stop();
                waveTimer?.Stop();
                mouseHook?.Unhook();
                keyboardHook?.Unhook();
            };
        }

        private void MouseHook_LeftButtonDown(Point screenPoint)
        {
            try
            {
                Debug.WriteLine($"Mouse button down at: {screenPoint.X}, {screenPoint.Y}");
                isMouseDown = true;
                currentMousePosition = screenPoint;

                // Trigger immediate glow on click
                TriggerGlowAtPosition(screenPoint);

                // Start the hold timer for continuous glowing
                holdTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in mouse down handler: {ex.Message}");
            }
        }

        private void MouseHook_LeftButtonUp(Point screenPoint)
        {
            try
            {
                Debug.WriteLine($"Mouse button up at: {screenPoint.X}, {screenPoint.Y}");
                isMouseDown = false;
                holdTimer.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in mouse up handler: {ex.Message}");
            }
        }

        private void MouseHook_MouseMove(Point screenPoint)
        {
            try
            {
                if (isMouseDown)
                {
                    currentMousePosition = screenPoint;
                    // The timer will handle the glow triggering
=======
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
>>>>>>> Develop
                }
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                Debug.WriteLine($"Error in mouse move handler: {ex.Message}");
            }
        }

        private void KeyboardHook_KeyDown(int vkCode, bool ctrl, bool alt, bool shift)
        {
            // Check for Ctrl+Alt+Shift+W combination
            if (vkCode == 0x57 && ctrl && alt && shift) // 0x57 is virtual key code for 'W'
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    StartWaveAnimation();
                });
            }
            if (vkCode == 0x53 && ctrl && shift) // 0x53 is virtual key code for 'S'
            {
                AnimAll();
                Tools Tools = new Tools(this, hexagons.Count());
                Tools.Topmost = true;
                Tools.ShowDialog();
                StartWaveAnimation();
            }
            if (vkCode == 0x41 && ctrl && shift)
            {
                AnimAll();
            }
            if (vkCode == 0x52 && ctrl && shift)
            {
                AnimSome();
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Trigger wave animation with Ctrl+Alt+Shift+W (very uncommon combo)
=======
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
>>>>>>> Develop
            if (e.Key == Key.W &&
                Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))
            {
                StartWaveAnimation();
            }
        }

<<<<<<< HEAD
        public void StartWaveAnimation()
        {
            if (isWaveActive) return; // Don't start if already running
=======
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
>>>>>>> Develop

            try
            {
                Debug.WriteLine("Starting wave animation");
<<<<<<< HEAD
                // Update timer interval with current waveAnimDuration value
                waveTimer.Interval = TimeSpan.FromMilliseconds(waveAnimDuration);
                isWaveActive = true;
                currentWaveColumn = 0;
                waveTimer.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting wave animation: {ex.Message}");
            }
        }

        private void WaveTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (currentWaveColumn >= hexagonColumns.Count)
                {
                    // Wave completed
                    waveTimer.Stop();
                    isWaveActive = false;
                    currentWaveColumn = 0;
                    Debug.WriteLine("Wave animation completed");
                    return;
                }

                // Animate all hexagons in the current column
                if (currentWaveColumn < hexagonColumns.Count)
                {
                    var column = hexagonColumns[currentWaveColumn];
                    foreach (var hex in column)
                    {
                        AnimateGlow(hex);
                    }
                    Debug.WriteLine($"Glowing column {currentWaveColumn} with {column.Count} hexagons");
                }

                currentWaveColumn++;
=======
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
>>>>>>> Develop
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in wave timer: {ex.Message}");
<<<<<<< HEAD
                waveTimer.Stop();
                isWaveActive = false;
            }
        }

        private void HoldTimer_Tick(object sender, EventArgs e)
        {
            if (isMouseDown)
            {
                TriggerGlowAtPosition(currentMousePosition);
            }
=======
                StopWaveAnimation();
            }
        }

        private void StopWaveAnimation()
        {
            _waveTimer.Stop();
            _isWaveActive = false;
            _currentWaveColumn = 0;
            Debug.WriteLine("Wave animation completed");
>>>>>>> Develop
        }

        private void TriggerGlowAtPosition(Point screenPoint)
        {
            try
            {
<<<<<<< HEAD
                // Convert screen coordinates to canvas coordinates
                Point canvasPoint = MainCanvas.PointFromScreen(screenPoint);

                foreach (var hex in hexagons)
                {
                    if (IsPointInPolygon(hex.Points, canvasPoint))
                    {
                        AnimateGlow(hex);
                        // Don't break - allow multiple hexagons to glow if overlapping
                    }
=======
                Point canvasPoint = MainCanvas.PointFromScreen(screenPoint);

                foreach (var hex in _hexagons.Where(h => IsPointInHexagon(h.Points, canvasPoint)))
                {
                    AnimateHexagonGlow(hex);
>>>>>>> Develop
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering glow: {ex.Message}");
            }
        }

<<<<<<< HEAD
        public class MouseHook
        {
            private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
            private LowLevelMouseProc _proc;
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
                using Process curProcess = Process.GetCurrentProcess();
                using ProcessModule curModule = curProcess.MainModule;
                return SetWindowsHookEx(14 /*WH_MOUSE_LL*/, proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0)
                {
                    try
                    {
                        MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                        Point mousePoint = new Point(hookStruct.pt.x, hookStruct.pt.y);

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
                        Debug.WriteLine($"Error in hook callback: {ex.Message}");
                    }
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            #region WinAPI

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
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

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
            private LowLevelKeyboardProc _proc;
            private IntPtr _hookID = IntPtr.Zero;

            public event Action<int, bool, bool, bool> KeyDown; // vkCode, ctrl, alt, shift

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
                using Process curProcess = Process.GetCurrentProcess();
                using ProcessModule curModule = curProcess.MainModule;
                return SetWindowsHookEx(13 /*WH_KEYBOARD_LL*/, proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)0x0100) // WM_KEYDOWN
                {
                    try
                    {
                        int vkCode = Marshal.ReadInt32(lParam);
                        bool ctrl = (GetAsyncKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                        bool alt = (GetAsyncKeyState(0x12) & 0x8000) != 0;  // VK_MENU
                        bool shift = (GetAsyncKeyState(0x10) & 0x8000) != 0; // VK_SHIFT

                        KeyDown?.Invoke(vkCode, ctrl, alt, shift);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in keyboard hook: {ex.Message}");
                    }
                }
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            [DllImport("user32.dll")]
            private static extern short GetAsyncKeyState(int vKey);
        }

        private bool IsPointInPolygon(PointCollection polygon, Point testPoint)
=======
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
>>>>>>> Develop
        {
            int n = polygon.Count;
            bool inside = false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Point pi = polygon[i];
                Point pj = polygon[j];

                bool intersect = ((pi.Y > testPoint.Y) != (pj.Y > testPoint.Y)) &&
<<<<<<< HEAD
                                 (testPoint.X < (pj.X - pi.X) * (testPoint.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);
=======
                                (testPoint.X < (pj.X - pi.X) * (testPoint.Y - pi.Y) / (pj.Y - pi.Y) + pi.X);
>>>>>>> Develop

                if (intersect)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

<<<<<<< HEAD
        private void DrawHexGrid()
        {
            try
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                Debug.WriteLine($"Drawing hex grid - Screen: {screenWidth}x{screenHeight}, HexHeight: {hexHeight}");

                MainCanvas.Children.Clear();
                hexagons.Clear();
                hexagonColumns.Clear();

                int hexCount = 0;

                // Fixed hexagon spacing - no overlap
                double verticalSpacing = hexHeight * 0.75;  // Proper vertical spacing for hexagons
                double horizontalSpacing = hexRadius * 1.5; // Proper horizontal spacing

                // Calculate number of columns for wave animation
                int numColumns = (int)Math.Ceiling((screenWidth + 2 * hexRadius) / horizontalSpacing) + 1;
                for (int i = 0; i < numColumns; i++)
                {
                    hexagonColumns.Add(new List<Polygon>());
                }

                int columnIndex = 0;
                for (double x = -hexRadius; x < screenWidth + 2 * hexRadius; x += horizontalSpacing)
                {
                    for (double y = -hexHeight; y < screenHeight + hexHeight; y += verticalSpacing)
                    {
                        int rowIndex = (int)(y / verticalSpacing);
                        bool isOddRow = rowIndex % 2 == 1;

                        // Offset every other row by half the horizontal spacing
                        double hexX = x + (isOddRow ? horizontalSpacing * 0.5 : 0);
                        double hexY = y;

                        Polygon hex = CreateHexagon(hexX, hexY, hexRadius);
                        hexagons.Add(hex);
                        MainCanvas.Children.Add(hex);

                        // Add to appropriate column for wave animation
                        if (columnIndex < hexagonColumns.Count)
                        {
                            hexagonColumns[columnIndex].Add(hex);
                        }

                        hexCount++;
                    }
                    columnIndex++;
                }

                Debug.WriteLine($"Created {hexCount} hexagons in {hexagonColumns.Count} columns");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing hex grid: {ex.Message}");
            }
        }

        private Polygon CreateHexagon(double centerX, double centerY, double radius)
        {
            Polygon hex = new Polygon();
            PointCollection points = new PointCollection();

            for (int i = 0; i < 6; i++)
            {
                double angle_deg = 60 * i - 30;
                double angle_rad = Math.PI / 180 * angle_deg;
                double x = centerX + radius * Math.Cos(angle_rad);
                double y = centerY + radius * Math.Sin(angle_rad);
                points.Add(new Point(x, y));
            }

            hex.Points = points;
            hex.Stroke = Brushes.Transparent;           // Invisible stroke
            hex.StrokeThickness = 0;                    // No stroke thickness
            hex.Fill = Brushes.Transparent;             // Completely transparent fill
            hex.Tag = false;

            return hex;
        }

        public void AnimAll()
        {
            foreach (Polygon hex in hexagons)
            {
                AnimateGlow(hex);
            }
        }
        
        private void AnimSome()
        {
            Random random = new Random();
            for(int i = 0; i < (hexagons.Count()/2); i++)
            {
                AnimateGlow(hexagons[random.Next(1, hexagons.Count)]);
            }
        }

        private void AnimateGlow(Polygon hex)
        {
            try
            {
                // Ensure we're on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Define colors: start and end with transparent, peak with bright blue
                    var transparentColor = Color.FromArgb(hexagonAP, hexagonRP, hexagonGP, hexagonBP);      // User's Color; Completely invisible by default
                    var glowColor = Color.FromArgb(hexagonA, hexagonR, hexagonG, hexagonB);                 // User's Color; Bright blue glow by default

                    // Create new brush for animation (start transparent)
                    var animationBrush = new SolidColorBrush(transparentColor);
                    hex.Fill = animationBrush;

                    // Create the glow animation: transparent -> bright -> transparent
                    var glowAnimation = new ColorAnimation
                    {
                        From = transparentColor,
                        To = glowColor,
                        Duration = TimeSpan.FromMilliseconds(hexGlowTime), //default is 250ms
                        AutoReverse = true,  // Fade back to transparent
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                    };

                    // Apply the animation
                    animationBrush.BeginAnimation(SolidColorBrush.ColorProperty, glowAnimation);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error animating glow: {ex.Message}");
            }
        }
    }
=======
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
>>>>>>> Develop
}