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

        public Tools(Window mainWindow, int hexagonCount)
        {
            InitializeComponent();
            MainWindow = (MainWindow)mainWindow;
            HexagonCounter.Content = "Hexagon count: " + hexagonCount;

            LoadFromRam();
            EnsureDirectoryExists();
        }

        private void StopHexes_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.Close();
            this.Close();
        }

        private void LoadFromRam()
        {
            GlowDurationSlider.Value = MainWindow.hexGlowTime;
            WaveDurationSlider.Value = MainWindow.waveAnimDuration;
            UpdateDelaySlider.Value = MainWindow.UpdateDelay;

            //Gloom Hexagon Color
            ColorA.Text = MainWindow.hexagonA.ToString();
            ColorR.Text = MainWindow.hexagonR.ToString();
            ColorG.Text = MainWindow.hexagonG.ToString();
            ColorB.Text = MainWindow.hexagonB.ToString();

            //Passive Hexagon Color
            ColorAPasive.Text = MainWindow.hexagonAP.ToString();
            ColorRPasive.Text = MainWindow.hexagonRP.ToString();
            ColorGPasive.Text = MainWindow.hexagonGP.ToString();
            ColorBPasive.Text = MainWindow.hexagonBP.ToString();
        }

        public void ApplyChanges()
        {
            try
            {
                MainWindow.hexGlowTime = (int)GlowDurationSlider.Value;
                MainWindow.waveAnimDuration = (int)WaveDurationSlider.Value;
                MainWindow.UpdateDelay = (int)UpdateDelaySlider.Value;

                //Gloom Hexagon Color
                MainWindow.hexagonA = byte.Parse(ColorA.Text);
                MainWindow.hexagonR = byte.Parse(ColorR.Text);
                MainWindow.hexagonG = byte.Parse(ColorG.Text);
                MainWindow.hexagonB = byte.Parse(ColorB.Text);

                //Passive Hexagon Color
                MainWindow.hexagonAP = byte.Parse(ColorAPasive.Text);
                MainWindow.hexagonRP = byte.Parse(ColorRPasive.Text);
                MainWindow.hexagonGP = byte.Parse(ColorGPasive.Text);
                MainWindow.hexagonBP = byte.Parse(ColorBPasive.Text);

                //Start animation to reset hexagons
                if (ResetAnimationCombobox.SelectedIndex == 1)
                {
                    MainWindow.AnimAll();
                }
                else
                {
                    MainWindow.StartWaveAnimation();
                }
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

                //Gloom Hexagon Color
                Save("SaveColorA", byte.Parse(ColorA.Text));
                Save("SaveColorR", byte.Parse(ColorR.Text));
                Save("SaveColorG", byte.Parse(ColorG.Text));
                Save("SaveColorB", byte.Parse(ColorB.Text));

                //Passive Hexagon Color
                Save("SaveColorAPassive", byte.Parse(ColorAPasive.Text));
                Save("SaveColorRPassive", byte.Parse(ColorRPasive.Text));
                Save("SaveColorGPassive", byte.Parse(ColorGPasive.Text));
                Save("SaveColorBPassive", byte.Parse(ColorBPasive.Text));

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
                MainWindow.hexGlowTime = Load("SaveGlowDuration", 250);
                MainWindow.waveAnimDuration = Load("SaveWaveDuration", 65);
                MainWindow.UpdateDelay = Load("SaveUpdateDelay", 35);

                //Gloom Hexagon Color
                MainWindow.hexagonA = (byte)Load("SaveColorA", 180);
                MainWindow.hexagonR = (byte)Load("SaveColorR", 100);
                MainWindow.hexagonG = (byte)Load("SaveColorG", 200);
                MainWindow.hexagonB = (byte)Load("SaveColorB", 255);

                //Passive Hexagon Color
                MainWindow.hexagonAP = (byte)Load("SaveColorAPassive", 0);
                MainWindow.hexagonRP = (byte)Load("SaveColorRPassive", 0);
                MainWindow.hexagonGP = (byte)Load("SaveColorGPassive", 150);
                MainWindow.hexagonBP = (byte)Load("SaveColorBPassive", 255);

                // Update the UI controls with loaded values
                LoadFromRam();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preset: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
    }
}