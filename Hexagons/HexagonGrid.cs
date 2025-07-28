using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Hexagons
{
    public class HexagonGrid
    {
        private readonly HexagonConfig _config;
        private readonly System.Windows.Controls.Canvas _canvas;

        public HexagonGrid(HexagonConfig config, System.Windows.Controls.Canvas canvas)
        {
            _config = config;
            _canvas = canvas;
        }

        public void DrawHexagonGrid(System.Collections.Generic.List<Polygon> hexagons, 
            System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
        {
            try
            {
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                Debug.WriteLine($"Drawing grid - Screen: {screenWidth}x{screenHeight}");

                ClearGrid(hexagons, hexagonColumns);

                var spacing = CalculateHexagonSpacing();
                CreateHexagonColumns(screenWidth, spacing.horizontal, hexagonColumns);
                PopulateHexagonGrid(screenWidth, screenHeight, spacing, hexagons, hexagonColumns);

                Debug.WriteLine($"Created {hexagons.Count} hexagons in {hexagonColumns.Count} columns");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error drawing grid: {ex.Message}");
            }
        }

        private void ClearGrid(System.Collections.Generic.List<Polygon> hexagons, 
            System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
        {
            _canvas.Children.Clear();
            hexagons.Clear();
            hexagonColumns.Clear();
        }

        private (double horizontal, double vertical) CalculateHexagonSpacing()
        {
            return (
                horizontal: _config.Radius * 1.5,
                vertical: _config.Height * 0.75
            );
        }

        private void CreateHexagonColumns(double screenWidth, double horizontalSpacing, 
            System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
        {
            int numColumns = (int)Math.Ceiling((screenWidth + 2 * _config.Radius) / horizontalSpacing) + 1;
            for (int i = 0; i < numColumns; i++)
            {
                hexagonColumns.Add(new System.Collections.Generic.List<Polygon>());
            }
        }

        private void PopulateHexagonGrid(double screenWidth, double screenHeight,
            (double horizontal, double vertical) spacing,
            System.Collections.Generic.List<Polygon> hexagons,
            System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
        {
            // Start from outside screen bounds to ensure full coverage
            double startX = -_config.Radius * 2;
            double endX = screenWidth + _config.Radius * 2;
            double startY = -_config.Height;
            double endY = screenHeight + _config.Height;

            int rowIndex = 0;

            // Iterate through rows (Y positions)
            for (double y = startY; y < endY; y += spacing.vertical)
            {
                int columnIndex = 0;
                bool isOddRow = rowIndex % 2 == 1;

                // Calculate X offset for odd rows to create hexagonal pattern
                double rowXOffset = isOddRow ? spacing.horizontal * 0.5 : 0;

                // Iterate through columns in this row (X positions)
                for (double x = startX; x < endX; x += spacing.horizontal)
                {
                    double actualX = x + rowXOffset;
                    var hex = CreateHexagon(actualX, y);

                    hexagons.Add(hex);
                    _canvas.Children.Add(hex);

                    // Add to column list (use column index based on actual position)
                    int actualColumnIndex = (int)((actualX + _config.Radius) / spacing.horizontal);

                    // Ensure we have enough columns
                    while (hexagonColumns.Count <= actualColumnIndex)
                    {
                        hexagonColumns.Add(new System.Collections.Generic.List<Polygon>());
                    }

                    if (actualColumnIndex >= 0 && actualColumnIndex < hexagonColumns.Count)
                    {
                        hexagonColumns[actualColumnIndex].Add(hex);
                    }

                    columnIndex++;
                }
                rowIndex++;
            }
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

            if (_config.RotationEnabled)
            {
                var rotateTransform = new RotateTransform(0, centerX, centerY);
                hex.RenderTransform = rotateTransform;
            }

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
    }
}