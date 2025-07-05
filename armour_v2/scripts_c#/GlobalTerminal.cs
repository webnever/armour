using Godot;

public partial class GlobalTerminal : Node
{
    private static TerminalConsole _instance;
    
    public static TerminalConsole Instance
    {
        get
        {
            if (_instance == null)
            {
                GD.PushError("Terminal Console instance not set! Make sure the scene is added to the tree.");
            }
            return _instance;
        }
        set => _instance = value;
    }
    
    // Convenience methods that forward to the instance
    public static void Print(string message, string type = "normal")
    {
        Instance?.PrintMessage(message, type);
    }
    
    public static void PrintTyping(string message, string type = "normal", float typingSpeed = 0.05f)
    {
        Instance?.PrintMessageWithTyping(message, type, typingSpeed);
    }
    
    public static void Toggle()
    {
        Instance?.ToggleConsole();
    }

    public static void TurnOn()
    {
        if (Instance != null && !Instance._isVisible)
        {
            Instance.ToggleConsole();
        }
    }

    public static void TurnOff()
    {
        if (Instance != null && Instance._isVisible)
        {
            Instance.ToggleConsole();
        }
    }
}