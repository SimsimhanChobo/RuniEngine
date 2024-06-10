#nullable enable
using System;
using System.Diagnostics;
using System.Reflection;

public static class Debug
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, LogText(className, message));
    }

    public static void ForceLog(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(ForceLogText(className, message));
    }

    public static void ForceLogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(ForceLogText(className, message));
    }

    public static void ForceLogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(ForceLogText(className, message));
    }

    public static void ForceAssert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, ForceLogText(className, message));
    }

    static string LogText(string className, object? message) => "[" + className + "] " + message;
    static string ForceLogText(string className, object? message) => "<b>[" + className + "]</b> " + message;


    public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

    public static string NameOfCallingClass(int skipFrames = 0)
    {
        string name;

        Type declaringType;

        skipFrames += 2;
        do
        {
            MethodBase method = new StackFrame(skipFrames, false).GetMethod();
            declaringType = method.DeclaringType;

            if (declaringType == null)
                return method.Name;

            name = declaringType.Name;
            skipFrames++;
        }
        while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

        return name;
    }

    public static StackFrame GetMethodCallerStackFrame()
    {
        StackFrame stackFrame;
        Type declaringType;
        int skipFrames = 2;
        do
        {
            stackFrame = new StackFrame(skipFrames, true);
            declaringType = stackFrame.GetMethod().DeclaringType;

            if (declaringType == null)
                return stackFrame;

            skipFrames++;
        }
        while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

        return stackFrame;
    }
}