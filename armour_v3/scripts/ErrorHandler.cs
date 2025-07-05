using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.Json;

public class ErrorHandler
{
    private static ErrorHandler _instance;
    private Queue<ErrorLog> _errorQueue = new Queue<ErrorLog>();
    private string _logPath = "user://error_log.txt";
    private bool _debugMode = false;

    public static ErrorHandler Instance
    {
        get
        {
            if (_instance == null)
                _instance = new ErrorHandler();
            return _instance;
        }
    }

    public string HandleError(Exception ex, string context = "Unknown", bool showToPlayer = true)
    {
        // Log the error
        LogError(ex, context);

        // Generate user-friendly message
        string userMessage = GenerateUserMessage(ex, context);

        // In debug mode, show full error
        if (_debugMode)
        {
            userMessage += $"\n\nDebug Info:\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
        }

        return userMessage;
    }

    public string HandleGameError(GameErrorType errorType, string details = "")
    {
        string message = errorType switch
        {
            GameErrorType.SaveFailed => "Unable to save game. Please check disk space and try again.",
            GameErrorType.LoadFailed => "Unable to load save file. It may be corrupted or from an incompatible version.",
            GameErrorType.ItemNotFound => $"The requested item could not be found{(string.IsNullOrEmpty(details) ? "." : $": {details}")}",
            GameErrorType.LocationNotFound => "You've somehow ended up nowhere. Returning to starting location...",
            GameErrorType.InvalidCommand => "I don't understand that command. Type 'help' for available commands.",
            GameErrorType.CombatError => "An error occurred during combat. Combat has been reset.",
            GameErrorType.QuestError => "Quest system encountered an error. Progress may not have been saved.",
            GameErrorType.NetworkError => "Network connection failed. Cloud saves are temporarily unavailable.",
            _ => "An unexpected error occurred. The game will attempt to continue."
        };

        LogGameError(errorType, details);
        return message;
    }

    private void LogError(Exception ex, string context)
    {
        var errorLog = new ErrorLog
        {
            Timestamp = DateTime.Now,
            Context = context,
            ErrorType = ex.GetType().Name,
            Message = ex.Message,
            StackTrace = ex.StackTrace,
            InnerException = ex.InnerException?.Message
        };

        _errorQueue.Enqueue(errorLog);

        // Keep only last 100 errors in memory
        while (_errorQueue.Count > 100)
        {
            _errorQueue.Dequeue();
        }

        // Write to log file
        WriteToLogFile(errorLog);
    }

    private void LogGameError(GameErrorType errorType, string details)
    {
        var errorLog = new ErrorLog
        {
            Timestamp = DateTime.Now,
            Context = "GameError",
            ErrorType = errorType.ToString(),
            Message = details,
            StackTrace = System.Environment.StackTrace,
            InnerException = null
        };

        _errorQueue.Enqueue(errorLog);
        WriteToLogFile(errorLog);
    }

    private void WriteToLogFile(ErrorLog error)
    {
        try
        {
            // Use Godot's FileAccess for file operations
            using (var file = Godot.FileAccess.Open(_logPath, Godot.FileAccess.ModeFlags.Write))
            {
                if (file != null)
                {
                    // Seek to end for appending
                    file.SeekEnd();
                    
                    file.StoreLine($"[{error.Timestamp:yyyy-MM-dd HH:mm:ss}] {error.Context} - {error.ErrorType}: {error.Message}");
                    if (!string.IsNullOrEmpty(error.StackTrace))
                    {
                        file.StoreLine($"Stack Trace: {error.StackTrace}");
                    }
                    if (!string.IsNullOrEmpty(error.InnerException))
                    {
                        file.StoreLine($"Inner Exception: {error.InnerException}");
                    }
                    file.StoreLine("---");
                    file.Flush();
                }
            }
        }
        catch
        {
            // Silently fail if we can't write to log
        }
    }

    private string GenerateUserMessage(Exception ex, string context)
    {
        // Generate friendly messages based on exception type and context
        if (ex is System.IO.FileNotFoundException || ex is System.IO.DirectoryNotFoundException)
        {
            return "A required file could not be found. Please verify your game installation.";
        }
        else if (ex is UnauthorizedAccessException)
        {
            return "Permission denied. Please check that the game has write access to its folders.";
        }
        else if (ex is OutOfMemoryException)
        {
            return "The game is running low on memory. Try closing other applications.";
        }
        else if (ex is InvalidOperationException)
        {
            switch (context)
            {
                case "Combat":
                    return "Combat system error. The battle has ended.";
                case "Inventory":
                    return "Inventory operation failed. Please try again.";
                case "Save":
                    return "Save operation failed. Your progress may not have been saved.";
                default:
                    return "An operation could not be completed. Please try again.";
            }
        }
        else if (ex is NullReferenceException)
        {
            return $"A required game element is missing. Context: {context}";
        }
        else if (ex is JsonException)
        {
            return "Data format error. Save file may be corrupted.";
        }
        else
        {
            return "An unexpected error occurred. The game will try to continue.";
        }
    }

    public void SetDebugMode(bool enabled)
    {
        _debugMode = enabled;
    }

    public List<ErrorLog> GetRecentErrors(int count = 10)
    {
        var allErrors = _errorQueue.ToArray();
        var startIndex = Math.Max(0, allErrors.Length - count);
        return allErrors.Skip(startIndex).ToList();
    }

    public void ClearErrorLog()
    {
        _errorQueue.Clear();
        try
        {
            if (Godot.FileAccess.FileExists(_logPath))
            {
                DirAccess.RemoveAbsolute(_logPath);
            }
        }
        catch
        {
            // Ignore
        }
    }
}

public class ErrorLog
{
    public DateTime Timestamp { get; set; }
    public string Context { get; set; }
    public string ErrorType { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public string InnerException { get; set; }
}

public enum GameErrorType
{
    SaveFailed,
    LoadFailed,
    ItemNotFound,
    LocationNotFound,
    InvalidCommand,
    CombatError,
    QuestError,
    NetworkError,
    Unknown
}

public static class SafeExecutor
{
    public static T Execute<T>(Func<T> action, T defaultValue, string context = "Unknown")
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            ErrorHandler.Instance.HandleError(ex, context, false);
            return defaultValue;
        }
    }
    
    public static void Execute(Action action, string context = "Unknown")
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ErrorHandler.Instance.HandleError(ex, context, false);
        }
    }
}