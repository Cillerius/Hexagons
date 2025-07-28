﻿using System;
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

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);
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

        public bool ConstantTrail = false;

        //rotation
        public bool RotationEnabled { get; set; } = false;
        public double RotationSpeed { get; set; } = 2.0; // Rotations per minute (RPM)


    }

    public static class KeyCombinations
    {
        public const int KEY_W = 0x57;
        public const int KEY_S = 0x53;
        public const int KEY_A = 0x41;
        public const int KEY_R = 0x52;
        public const int KEY_T = 0x54;
        public const int KEY_CAPS = 0x14;

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

        private HexagonGrid _hexagonGrid;
        private AnimationManager _animationManager;

        private bool _isMouseDown = false;
        private Point _currentMousePosition;
        public int _resetHexagonsAnimation = 0;
        public int _closeToolsAnimation = 0;
        private bool _isCapsLockGlowing = false;
        #endregion

        #region Initialization
        public MainWindow()
        {
            InitializeComponent();
            InitializeManagers();
            InitializeWindow();
            InitializeTimers();

            Loaded += OnWindowLoaded;
        }

        private void InitializeManagers()
        {
            _hexagonGrid = new HexagonGrid(_config, MainCanvas);
            _animationManager = new AnimationManager(_config);
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
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            MakeWindowClickThrough();
            DrawHexagonGrid();
            SetupInputHooks();

            this.Focus();
            this.Activate();

            Closed += OnWindowClosed;

            if (_config.ConstantTrail && !_holdTimer.IsEnabled)
            {
                _holdTimer.Start();
            }
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
            _animationManager?.StopAllAnimations();
            _mouseHook?.Unhook();
            _keyboardHook?.Unhook();
        }

        public void UpdateTimerIntervals()
        {
            if (_holdTimer != null)
            {
                bool wasRunning = _holdTimer.IsEnabled;
                _holdTimer.Stop();
                _holdTimer.Interval = TimeSpan.FromMilliseconds(_config.UpdateDelayMs);
                if (wasRunning)
                {
                    _holdTimer.Start();
                }
            }

            _animationManager?.UpdateTimerIntervals();
        }

        public void UpdateRotationSpeed()
        {
            _animationManager?.UpdateRotationSpeed();
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
                if (!_config.ConstantTrail)
                {
                    _holdTimer.Stop();
                }
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
                if (_isMouseDown || _config.ConstantTrail)
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
            // caps lock toggled
            else if (vkCode == KeyCombinations.KEY_CAPS)
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _isCapsLockGlowing = !_isCapsLockGlowing; // Simply toggle our tracked state

                    // Find the hexagon closest to center-top position (with 50px offset from top)
                    Point targetPosition = new Point(
                        SystemParameters.PrimaryScreenWidth / 2,  // Center horizontally
                        70  // 50px from top
                    );

                    var closestHex = FindClosestHexagon(targetPosition);

                    if (closestHex != null)
                    {
                        if (_isCapsLockGlowing)
                        {
                            _animationManager.ToggleHexGlow(closestHex);
                        }
                        else
                        {
                            _animationManager.UnToggleHexGlow(closestHex);
                        }
                    }
                });
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
            if (_isMouseDown || _config.ConstantTrail)
            {
                TriggerGlowAtPosition(_currentMousePosition);
            }
        }
        #endregion

        #region Animation Wrapper Methods
        public void StartWaveAnimation()
        {
            _animationManager.StartWaveAnimation(_hexagonColumns);
        }

        public void AnimateAllHexagons()
        {
            _animationManager.AnimateAllHexagons(_hexagons);
        }

        private void AnimateSomeHexagons()
        {
            _animationManager.AnimateSomeHexagons(_hexagons);
        }

        public void StartRipple(Point origin)
        {
            _animationManager.StartRipple(origin, _hexagons);
        }

        private void TriggerGlowAtPosition(Point screenPoint)
        {
            try
            {
                Point canvasPoint = MainCanvas.PointFromScreen(screenPoint);

                foreach (var hex in _hexagons.Where(h => IsPointInHexagon(h.Points, canvasPoint)))
                {
                    _animationManager.AnimateHexagonGlow(hex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error triggering glow: {ex.Message}");
            }
        }
        #endregion

        #region Hexagon Grid Wrapper Methods
        public void DrawHexagonGrid()
        {
            _hexagonGrid.DrawHexagonGrid(_hexagons, _hexagonColumns);
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
                //animAll
                case 1:
                    AnimateAllHexagons();
                    break;

                //ripple
                case 2:
                    StartRipple(new Point(
                        SystemParameters.PrimaryScreenWidth / 2,
                        SystemParameters.PrimaryScreenHeight / 2));
                    break;

                //none
                case 3:

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
                //animAll
                case 1:
                    AnimateAllHexagons();
                    break;

                //ripple
                case 2:
                    StartRipple(new Point(
                        SystemParameters.PrimaryScreenWidth / 2,
                        SystemParameters.PrimaryScreenHeight / 2));
                    break;

                //none
                case 3:

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

        private Polygon FindClosestHexagon(Point targetPosition)
        {
            Polygon closestHex = null;
            double minDistance = double.MaxValue;

            foreach (var hex in _hexagons)
            {
                Point hexCenter = GetPolygonCenter(hex);
                double distance = GetDistance(targetPosition, hexCenter);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestHex = hex;
                }
            }

            return closestHex;
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

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        #endregion
    }
    #endregion
}