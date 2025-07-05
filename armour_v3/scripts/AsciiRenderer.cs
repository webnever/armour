using Godot;
using System;
using System.Collections.Generic;

public partial class AsciiRenderer : Node2D
{
    [Export]
    public string FontPath = "res://fonts/ModernDOS9x16.ttf";
    
    [Export]
    public int FontSize = 16;
    
    [Export]
    public Vector2I Resolution = new Vector2I(1280, 720);
    
    [Export]
    public Vector2I CharacterGridSize = new Vector2I(80, 45); // Adjust based on font size
    
    [Export]
    public Color TextColor = Colors.Green;
    
    [Export]
    public Color BackgroundColor = Colors.Black;
    
    [Export]
    public string DefaultChar = " ";
    
    [Export]
    public bool UseDithering = false;
    
    [Export]
    public float ContrastBoost = 1.2f;
    
    [Export]
    public float GammaCorrection = 1.0f;
    
    private Vector2 _cellSize;
    private Label[,] _characterGrid;
    private FontVariation _font;
    
    public override void _Ready()
    {
        _cellSize = new Vector2(
            Resolution.X / (float)CharacterGridSize.X,
            Resolution.Y / (float)CharacterGridSize.Y
        );
        
        // Load the font
        _font = new FontVariation();
        _font.BaseFont = GD.Load<Font>(FontPath);
                
        // Create the background
        var background = new ColorRect();
        background.Color = BackgroundColor;
        background.Size = new Vector2(Resolution.X, Resolution.Y);
        AddChild(background);
        
        // Initialize the character grid
        InitializeCharacterGrid();
        
        // Test with some ASCII art
        DrawTestPattern();
    }
    
    private void InitializeCharacterGrid()
    {
        _characterGrid = new Label[CharacterGridSize.X, CharacterGridSize.Y];
        
        for (int y = 0; y < CharacterGridSize.Y; y++)
        {
            for (int x = 0; x < CharacterGridSize.X; x++)
            {
                var label = new Label();
                label.Position = new Vector2(x * _cellSize.X, y * _cellSize.Y);
                label.Text = DefaultChar;
                label.AddThemeColorOverride("font_color", TextColor);
                label.AddThemeFontOverride("font", _font);
                label.AddThemeFontSizeOverride("font_size", FontSize);
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.CustomMinimumSize = new Vector2(_cellSize.X, _cellSize.Y);
                
                AddChild(label);
                _characterGrid[x, y] = label;
            }
        }
    }
    
    // Draw a character at a specific grid position
    public void DrawChar(int x, int y, string character)
    {
        if (x >= 0 && x < CharacterGridSize.X && y >= 0 && y < CharacterGridSize.Y)
        {
            _characterGrid[x, y].Text = character;
        }
    }
    
    // Draw a string horizontally starting at a specific position
    public void DrawString(int x, int y, string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (x + i < CharacterGridSize.X)
            {
                DrawChar(x + i, y, text[i].ToString());
            }
        }
    }
    
    // Clear the entire grid
    public void Clear()
    {
        for (int y = 0; y < CharacterGridSize.Y; y++)
        {
            for (int x = 0; x < CharacterGridSize.X; x++)
            {
                _characterGrid[x, y].Text = DefaultChar;
            }
        }
    }
    
    // Set the color of a specific character
    public void SetCharColor(int x, int y, Color color)
    {
        if (x >= 0 && x < CharacterGridSize.X && y >= 0 && y < CharacterGridSize.Y)
        {
            _characterGrid[x, y].AddThemeColorOverride("font_color", color);
        }
    }
    
    // Draw a rectangle using ASCII characters
    public void DrawRectangle(int x, int y, int width, int height, string borderChar = "#")
    {
        // Draw top and bottom borders
        for (int i = 0; i < width; i++)
        {
            DrawChar(x + i, y, borderChar);
            DrawChar(x + i, y + height - 1, borderChar);
        }
        
        // Draw left and right borders
        for (int i = 1; i < height - 1; i++)
        {
            DrawChar(x, y + i, borderChar);
            DrawChar(x + width - 1, y + i, borderChar);
        }
    }
    
    // Load ASCII art from a text file
    public void LoadAsciiArtFromFile(string filePath)
    {
        try
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"Could not open file: {filePath}");
                return;
            }
            
            int y = 0;
            while (!file.EofReached() && y < CharacterGridSize.Y)
            {
                string line = file.GetLine();
                DrawString(0, y, line);
                y++;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error reading ASCII art file: {e.Message}");
        }
    }
    
    // Method to load and render an image as ASCII art
    public void LoadImageAsAscii(string imagePath, string asciiChars = "@%#*+=-:. ")
    {
        try
        {
            // Load the image
            Image image = new Image();
            Error err = image.Load(imagePath);
            
            if (err != Error.Ok)
            {
                GD.PrintErr($"Failed to load image: {imagePath}");
                return;
            }
            
            GenerateAsciiFromImage(image, asciiChars);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading image as ASCII: {e.Message}");
        }
    }
    
    // Generate ASCII from image with improved algorithm
    public void GenerateAsciiFromImage(Image image, string asciiChars = "@%#*+=-:. ")
    {
        if (image == null)
        {
            GD.PrintErr("AsciiRenderer: No image provided for ASCII generation");
            return;
        }
        
        Clear();
        
        // Calculate scaling to fit the image to our character grid
        int imageWidth = image.GetWidth();
        int imageHeight = image.GetHeight();
        
        // Account for character aspect ratio (characters are typically taller than wide)
        float charAspectRatio = _cellSize.Y / _cellSize.X;
        
        float scaleX = (float)CharacterGridSize.X / imageWidth;
        float scaleY = (float)CharacterGridSize.Y / imageHeight * charAspectRatio;
        float scale = Mathf.Min(scaleX, scaleY);
        
        int targetWidth = Mathf.RoundToInt(imageWidth * scale);
        int targetHeight = Mathf.RoundToInt(imageHeight * scale / charAspectRatio);
        
        // Calculate offset to center the image
        int offsetX = (CharacterGridSize.X - targetWidth) / 2;
        int offsetY = (CharacterGridSize.Y - targetHeight) / 2;
        
        // Pre-calculate character brightness values for better mapping
        float[] charBrightness = CalculateCharacterBrightness(asciiChars);
        
        // Generate ASCII characters with improved sampling
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                // Use area sampling for better quality
                Color avgColor = SampleImageArea(image, x, y, scale, charAspectRatio, imageWidth, imageHeight);
                
                // Calculate luminance using proper formula
                float luminance = CalculateLuminance(avgColor);
                
                // Apply contrast boost and gamma correction
                luminance = Mathf.Pow(luminance * ContrastBoost, 1.0f / GammaCorrection);
                luminance = Mathf.Clamp(luminance, 0.0f, 1.0f);
                
                // Apply dithering if enabled
                if (UseDithering)
                {
                    float ditherValue = GetDitherValue(x, y) / 255.0f * 0.1f; // 10% dither strength
                    luminance += ditherValue;
                    luminance = Mathf.Clamp(luminance, 0.0f, 1.0f);
                }
                
                // Find best matching character based on brightness
                char asciiChar = FindBestCharacter(luminance, asciiChars, charBrightness);
                
                // Set the character and color
                int gridX = offsetX + x;
                int gridY = offsetY + y;
                
                if (gridX >= 0 && gridX < CharacterGridSize.X && gridY >= 0 && gridY < CharacterGridSize.Y)
                {
                    DrawChar(gridX, gridY, asciiChar.ToString());
                    SetCharColor(gridX, gridY, avgColor);
                }
            }
        }
    }
    
    // Sample image area with averaging for better quality
    private Color SampleImageArea(Image image, int x, int y, float scale, float charAspectRatio, int imageWidth, int imageHeight)
    {
        // Calculate the area in the source image that corresponds to this character
        float srcStartX = x / scale;
        float srcStartY = y / scale * charAspectRatio;
        float srcEndX = (x + 1) / scale;
        float srcEndY = (y + 1) / scale * charAspectRatio;
        
        // Sample multiple points within the area and average them
        int samples = 4; // 2x2 sampling
        float totalR = 0, totalG = 0, totalB = 0, totalA = 0;
        int sampleCount = 0;
        
        for (int sy = 0; sy < samples; sy++)
        {
            for (int sx = 0; sx < samples; sx++)
            {
                float sampleX = srcStartX + (srcEndX - srcStartX) * (sx + 0.5f) / samples;
                float sampleY = srcStartY + (srcEndY - srcStartY) * (sy + 0.5f) / samples;
                
                int pixelX = Mathf.Clamp(Mathf.RoundToInt(sampleX), 0, imageWidth - 1);
                int pixelY = Mathf.Clamp(Mathf.RoundToInt(sampleY), 0, imageHeight - 1);
                
                Color pixel = image.GetPixel(pixelX, pixelY);
                totalR += pixel.R;
                totalG += pixel.G;
                totalB += pixel.B;
                totalA += pixel.A;
                sampleCount++;
            }
        }
        
        return new Color(
            totalR / sampleCount,
            totalG / sampleCount,
            totalB / sampleCount,
            totalA / sampleCount
        );
    }
    
    // Calculate luminance using proper perceptual formula
    private float CalculateLuminance(Color color)
    {
        // Use ITU-R BT.709 luma coefficients for perceptual brightness
        return 0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;
    }
    
    // Pre-calculate brightness values for each character for better mapping
    private float[] CalculateCharacterBrightness(string asciiChars)
    {
        // Approximate brightness values for common ASCII characters
        // These values are based on the visual density of each character
        Dictionary<char, float> charDensity = new Dictionary<char, float>
        {
            { ' ', 0.0f }, { '.', 0.1f }, { ':', 0.15f }, { '-', 0.2f }, { '=', 0.25f },
            { '+', 0.3f }, { '*', 0.4f }, { '#', 0.6f }, { '%', 0.7f }, { '@', 0.9f },
            { '█', 1.0f }, { '▓', 0.8f }, { '▒', 0.6f }, { '░', 0.4f }
        };
        
        float[] brightness = new float[asciiChars.Length];
        for (int i = 0; i < asciiChars.Length; i++)
        {
            char c = asciiChars[i];
            if (charDensity.ContainsKey(c))
            {
                brightness[i] = charDensity[c];
            }
            else
            {
                // Default mapping based on position in string
                brightness[i] = (float)i / (asciiChars.Length - 1);
            }
        }
        
        return brightness;
    }
    
    // Find the best matching character based on luminance
    private char FindBestCharacter(float luminance, string asciiChars, float[] charBrightness)
    {
        int bestIndex = 0;
        float bestDiff = Mathf.Abs(luminance - charBrightness[0]);
        
        for (int i = 1; i < charBrightness.Length; i++)
        {
            float diff = Mathf.Abs(luminance - charBrightness[i]);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestIndex = i;
            }
        }
        
        return asciiChars[bestIndex];
    }
    
    // Get dither value using ordered dithering (Bayer matrix)
    private float GetDitherValue(int x, int y)
    {
        // 4x4 Bayer dithering matrix
        int[,] bayerMatrix = new int[4, 4]
        {
            { 0, 8, 2, 10 },
            { 12, 4, 14, 6 },
            { 3, 11, 1, 9 },
            { 15, 7, 13, 5 }
        };
        
        return bayerMatrix[x % 4, y % 4] - 7.5f; // Center around 0
    }
    
    // Test pattern to verify the ASCII renderer
    private void DrawTestPattern()
    {
        // Draw a border around the screen
        DrawRectangle(0, 0, CharacterGridSize.X, CharacterGridSize.Y);
        
        // Draw a title
        string title = "ASCII RENDERER";
        DrawString(CharacterGridSize.X / 2 - title.Length / 2, 2, title);
        
        // Draw some sample text
        DrawString(5, 5, "This is a test of the ASCII renderer.");
        DrawString(5, 7, "Perfect DOS VGA 437 Font - 1280x720 Resolution");
        
        // Draw a small box
        DrawRectangle(10, 10, 20, 8, "+");
        
        // Draw simple ASCII art face
        DrawString(15, 12, " ^_^ ");
        DrawString(15, 13, "/   \\");
        DrawString(15, 14, "\\___/");
        
        // Example of using color
        for (int i = 0; i < 10; i++)
        {
            SetCharColor(50 + i, 15, new Color(i / 10.0f, 1.0f - i / 10.0f, 0.5f));
            DrawChar(50 + i, 15, "█");
        }
    }
}