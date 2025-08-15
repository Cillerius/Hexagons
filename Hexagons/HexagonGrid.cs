using Hexagons;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

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
            // Get the total bounds of all monitors
            var totalBounds = MultiMonitorHelper.GetTotalScreenBounds();

            Debug.WriteLine($"Drawing grid - Total bounds: {totalBounds.Width}x{totalBounds.Height} at ({totalBounds.Left}, {totalBounds.Top})");
            Debug.WriteLine($"Canvas size: {_canvas.Width}x{_canvas.Height}");

            // Ensure canvas size matches total bounds
            _canvas.Width = totalBounds.Width;
            _canvas.Height = totalBounds.Height;

            ClearGrid(hexagons, hexagonColumns);

            var spacing = CalculateHexagonSpacing();
            CreateHexagonColumns(totalBounds.Width, spacing.horizontal, hexagonColumns);
            PopulateHexagonGrid(totalBounds, spacing, hexagons, hexagonColumns);

            Debug.WriteLine($"Created {hexagons.Count} hexagons in {hexagonColumns.Count} columns");

            // Debug: Print monitor information
            var monitorBounds = MultiMonitorHelper.GetAllMonitorBounds();
            for (int i = 0; i < monitorBounds.Count; i++)
            {
                Debug.WriteLine($"Monitor {i}: {monitorBounds[i]}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error drawing grid: {ex.Message}");
        }
    }

    private void PopulateHexagonGrid(Rect totalBounds,
        (double horizontal, double vertical) spacing,
        System.Collections.Generic.List<Polygon> hexagons,
        System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
    {
        // Extend the bounds to ensure complete coverage across all monitors
        // Add extra padding to account for hexagon size
        double padding = _config.Radius * 2;
        double startX = totalBounds.Left - padding;
        double endX = totalBounds.Right + padding;
        double startY = totalBounds.Top - padding;
        double endY = totalBounds.Bottom + padding;

        Debug.WriteLine($"Hexagon grid bounds: X({startX} to {endX}), Y({startY} to {endY})");
        Debug.WriteLine($"Grid size: {endX - startX} x {endY - startY}");

        int rowIndex = 0;

        // Iterate through rows (Y positions)
        for (double y = startY; y < endY; y += spacing.vertical)
        {
            bool isOddRow = rowIndex % 2 == 1;
            double rowXOffset = isOddRow ? spacing.horizontal * 0.5 : 0;

            // Iterate through columns in this row (X positions)
            for (double x = startX; x < endX; x += spacing.horizontal)
            {
                double actualX = x + rowXOffset;

                // Convert to canvas coordinates (relative to the total bounds origin)
                double canvasX = actualX - totalBounds.Left;
                double canvasY = y - totalBounds.Top;

                // Skip hexagons that would be outside the canvas bounds
                if (canvasX < -_config.Radius || canvasX > _canvas.Width + _config.Radius ||
                    canvasY < -_config.Radius || canvasY > _canvas.Height + _config.Radius)
                {
                    continue;
                }

                var hex = CreateHexagon(canvasX, canvasY);

                hexagons.Add(hex);
                _canvas.Children.Add(hex);

                // Add to column list
                int actualColumnIndex = (int)((actualX - totalBounds.Left + _config.Radius) / spacing.horizontal);

                // Ensure we have enough columns
                while (hexagonColumns.Count <= actualColumnIndex)
                {
                    hexagonColumns.Add(new System.Collections.Generic.List<Polygon>());
                }

                if (actualColumnIndex >= 0 && actualColumnIndex < hexagonColumns.Count)
                {
                    hexagonColumns[actualColumnIndex].Add(hex);
                }
            }
            rowIndex++;
        }

        Debug.WriteLine($"Populated grid with {hexagons.Count} hexagons across {hexagonColumns.Count} columns");
    }

    // Rest of the methods remain the same...
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

    private void CreateHexagonColumns(double totalWidth, double horizontalSpacing,
        System.Collections.Generic.List<System.Collections.Generic.List<Polygon>> hexagonColumns)
    {
        int numColumns = (int)Math.Ceiling((totalWidth + 4 * _config.Radius) / horizontalSpacing) + 2;
        for (int i = 0; i < numColumns; i++)
        {
            hexagonColumns.Add(new System.Collections.Generic.List<Polygon>());
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