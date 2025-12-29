using System.Collections.Generic;
using System.Linq;
using Godot;
using SmartFormat;

namespace HavenMusic.Library;

public static class GLogger
{
    public enum Level
    {
        NONE,
        ERROR,
        INFO,
        WARN,
        DEBUG
    }

    public static Level DebugLevel = Level.DEBUG;

    public record LogMessageStruct(
        string level_color,
        string level,
        string path,
        string file,
        string function,
        int line_no,
        string message);

    private static Dictionary<Level, string> _colorTable = new Dictionary<Level, string> {
        [Level.NONE] = "white",
        [Level.ERROR] = "firebrick",
        [Level.INFO] = "white",
        [Level.WARN] = "gold",
        [Level.DEBUG] = "green",
    };

    private const string DefaultFormat =
        "[ [color={level_color}]{level}[/color] ] [color=cyan]({file}:{function}:{line_no})[/color]: {message}"; 
    
    private static string _msgFormat = DefaultFormat;

    public static void StaticInit()
    {
        DebugLevel = ProjectSettings.GetSetting("application/glogger/log_level", (int)Level.DEBUG).As<Level>();
        _msgFormat = ProjectSettings.GetSetting("application/glogger/log_message_format", DefaultFormat).As<string>();
    }

    public static void LogMessage(Level level, string message, string? func)
    {
        if (level > DebugLevel) return;

        Godot.Collections.Array<ScriptBacktrace>? stack = Engine.CaptureScriptBacktraces();
        if (stack == null)
            GD.PushError("Stack is Empty!");
        else
        {
            if (func == null)
                func = stack[1].GetFrameFunction(4);
            
            var msg = new LogMessageStruct
            (
                level_color: _colorTable[level],
                level: level.ToString(),
                path: stack[1].GetFrameFile(4).GetBaseDir(),
                file: stack[1].GetFrameFile(4).GetFile(),
                function: func,
                line_no: stack[1].GetFrameLine(4),
                message: message
            );
            GD.PrintRich(Smart.Format(_msgFormat, msg));
        }
    }

    public static void Info(string message, [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null) => LogMessage(Level.INFO, message, methodName);
    public static void Warning(string message, [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null) => LogMessage(Level.WARN, message, methodName);
    public static void Error(string message, [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null) => LogMessage(Level.ERROR, message, methodName);
    public static void Debug(string message, [System.Runtime.CompilerServices.CallerMemberName] string? methodName = null) => LogMessage(Level.DEBUG, message, methodName);
}