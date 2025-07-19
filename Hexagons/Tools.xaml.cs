using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Hexagons
{
    /// <summary>
    /// Interaction logic for Tools.xaml
    /// </summary>
    public partial class Tools : Window
    {
        public MainWindow MainWindow;

        public Tools(Window mainWindow)
        {
            InitializeComponent();
            MainWindow = (MainWindow)mainWindow;
            HexagonCounter.Content = "Hexagon count: " + MainWindow._hexagons.Count();

            LoadFromRam();
            EnsureDirectoryExists();
        }
        #region Loading-Saving-applying
        private void LoadFromRam()
        {
            GlowDurationSlider.Value = MainWindow._config.GlowDurationMs;
            WaveDurationSlider.Value = MainWindow._config.WaveSpeedMs;
            UpdateDelaySlider.Value = MainWindow._config.UpdateDelayMs;
            HexagonRadiusSlider.Value = MainWindow._config.Radius;
            RippleDuratonSlider.Value = MainWindow._config.RippleSpeedMs;

            //animation selection
            ResetAnimationCombobox.SelectedIndex = MainWindow._resetHexagonsAnimation;
            CloseToolsAnimationCombobox.SelectedIndex = MainWindow._closeToolsAnimation;

            //Glow Hexagon Color
            ColorA.Text = MainWindow._config.GlowColor.A.ToString();
            ColorR.Text = MainWindow._config.GlowColor.R.ToString();
            ColorG.Text = MainWindow._config.GlowColor.G.ToString();
            ColorB.Text = MainWindow._config.GlowColor.B.ToString();

            //Passive Hexagon Color
            ColorAPasive.Text = MainWindow._config.PassiveColor.A.ToString();
            ColorRPasive.Text = MainWindow._config.PassiveColor.R.ToString();
            ColorGPasive.Text = MainWindow._config.PassiveColor.G.ToString();
            ColorBPasive.Text = MainWindow._config.PassiveColor.B.ToString();

            //gameMode
            GameModeCheckBox.IsChecked = MainWindow._config.GameMode;
        }

        public void ApplyChanges()
        {
            try
            {
                MainWindow._config.GlowDurationMs = (int)GlowDurationSlider.Value;
                MainWindow._config.WaveSpeedMs = (int)WaveDurationSlider.Value;
                MainWindow._config.UpdateDelayMs = (int)UpdateDelaySlider.Value;
                MainWindow._config.Radius = (int)HexagonRadiusSlider.Value;
                MainWindow._config.RippleSpeedMs = (int)RippleDuratonSlider.Value;

                //animation selection
                MainWindow._resetHexagonsAnimation = ResetAnimationCombobox.SelectedIndex;
                MainWindow._closeToolsAnimation = CloseToolsAnimationCombobox.SelectedIndex;

                //Glow Hexagon Color
                MainWindow._config.GlowColor = Color.FromArgb(
                    byte.Parse(ColorA.Text),
                    byte.Parse(ColorR.Text),
                    byte.Parse(ColorG.Text),
                    byte.Parse(ColorB.Text)
                );

                //Passive Hexagon Color
                MainWindow._config.PassiveColor = Color.FromArgb(
                    byte.Parse(ColorAPasive.Text),
                    byte.Parse(ColorRPasive.Text),
                    byte.Parse(ColorGPasive.Text),
                    byte.Parse(ColorBPasive.Text)
                );

                //Game mode
                MainWindow._config.GameMode = GameModeCheckBox.IsChecked.Value;

                MainWindow.DrawHexagonGrid();
                //Start animation to reset hexagons
                switch (MainWindow._resetHexagonsAnimation)
                {
                    //animAll
                    case 1:
                        MainWindow.AnimateAllHexagons();
                        break;

                    //ripple
                    case 2:
                        MainWindow.StartRipple(new Point(
                        SystemParameters.PrimaryScreenWidth / 2,
                        SystemParameters.PrimaryScreenHeight / 2));
                        break;

                    //none
                    case 3:

                        break;
                    default:
                        MainWindow.StartWaveAnimation();
                        break;
                }
                HexagonCounter.Content = "Hexagon count: " + MainWindow._hexagons.Count();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePreset()
        {
            try
            {
                Save("SaveGlowDuration", (int)GlowDurationSlider.Value);
                Save("SaveWaveDuration", (int)WaveDurationSlider.Value);
                Save("SaveUpdateDelay", (int)UpdateDelaySlider.Value);
                Save("HexagonRadius", (int)HexagonRadiusSlider.Value);
                Save("RippleDuration", (int)RippleDuratonSlider.Value);

                //Animation selection
                Save("ResetHexagonsAnimation", ResetAnimationCombobox.SelectedIndex);
                Save("CloseToolsAnimation", CloseToolsAnimationCombobox.SelectedIndex);

                //Glow Hexagon Color
                Save("SaveColorA", byte.Parse(ColorA.Text));
                Save("SaveColorR", byte.Parse(ColorR.Text));
                Save("SaveColorG", byte.Parse(ColorG.Text));
                Save("SaveColorB", byte.Parse(ColorB.Text));

                //Passive Hexagon Color
                Save("SaveColorAPassive", byte.Parse(ColorAPasive.Text));
                Save("SaveColorRPassive", byte.Parse(ColorRPasive.Text));
                Save("SaveColorGPassive", byte.Parse(ColorGPasive.Text));
                Save("SaveColorBPassive", byte.Parse(ColorBPasive.Text));

                //game mode
                Save("GameMode", GameModeCheckBox.IsChecked.Value);

                ApplyChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadPreset()
        {
            try
            {
                MainWindow._config.GlowDurationMs = Load("SaveGlowDuration", 250);
                MainWindow._config.WaveSpeedMs = Load("SaveWaveDuration", 65);
                MainWindow._config.UpdateDelayMs = Load("SaveUpdateDelay", 35);
                MainWindow._config.Radius = Load("HexagonRadius", 50);
                MainWindow._config.RippleSpeedMs = Load("RippleDuration", 20);

                //Animation selection
                MainWindow._resetHexagonsAnimation = Load("ResetHexagonsAnimation", 0);
                MainWindow._closeToolsAnimation = Load("CloseToolsAnimation", 0);

                //Glow Hexagon Color
                MainWindow._config.GlowColor = Color.FromArgb(
                    (byte)Load("SaveColorA", 180),
                    (byte)Load("SaveColorR", 100),
                    (byte)Load("SaveColorG", 200),
                    (byte)Load("SaveColorB", 255)
                );

                //Passive Hexagon Color
                MainWindow._config.PassiveColor = Color.FromArgb(
                    (byte)Load("SaveColorAPassive", 0),
                    (byte)Load("SaveColorRPassive", 0),
                    (byte)Load("SaveColorGPassive", 150),
                    (byte)Load("SaveColorBPassive", 255)
                );

                //game mode

                MainWindow._config.GameMode = Load("GameMode", false);

                // Update the UI controls with loaded values
                LoadFromRam();
                ApplyChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region EventHandlers
        //Event handlers
        private void ResetHexagons_Click(object sender, RoutedEventArgs e)
        {
            ApplyChanges();
        }

        private void SavePresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClearAll();
                SavePreset();
                MainWindow.StartWaveAnimation();
                MessageBox.Show("Preset saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPresetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPreset();
                MainWindow.StartWaveAnimation();
                MessageBox.Show("Preset loaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopHexes_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Close();
            this.Close();
        }
        #endregion

        #region JsonManagment
        //Json file management tools
        private static Dictionary<string, object> settings = new Dictionary<string, object>();
        private static string fileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Hexagons",
            "Preset.json"
        );

        private static void EnsureDirectoryExists()
        {
            try
            {
                string directory = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                // Silent fail - directory creation is not critical for basic functionality
            }
        }

        // Save a single value
        public static void Save(string key, object value)
        {
            try
            {
                EnsureDirectoryExists();
                LoadAll(); // Load existing settings first
                settings[key] = value; // Add or update the value

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save setting '{key}': {ex.Message}");
            }
        }

        // Load a single value
        public static T Load<T>(string key, T defaultValue = default(T))
        {
            try
            {
                LoadAll(); // Load all settings from file

                if (settings.ContainsKey(key))
                {
                    var jsonElement = (JsonElement)settings[key];
                    return JsonSerializer.Deserialize<T>(jsonElement);
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        // Helper method to load all settings from file
        private static void LoadAll()
        {
            try
            {
                if (File.Exists(fileName))
                {
                    string json = File.ReadAllText(fileName);
                    if (!string.IsNullOrEmpty(json))
                    {
                        settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                    }
                }
            }
            catch
            {
                settings = new Dictionary<string, object>();
            }
        }

        public static void ClearAll()
        {
            try
            {
                settings.Clear();
                EnsureDirectoryExists();
                File.WriteAllText(fileName, "{}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to clear settings: {ex.Message}");
            }
        }
        #endregion


        #region Tools window setup stuff

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Find the parent ComboBox
            var border = sender as Border;
            var comboBox = border.TemplatedParent as ComboBox;

            if (comboBox != null)
            {
                comboBox.IsDropDownOpen = !comboBox.IsDropDownOpen;
            }
        }

        #endregion
    }
}