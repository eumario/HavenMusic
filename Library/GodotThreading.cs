using System;
using System.Threading;
using Godot;

namespace HavenMusic.Library;

public static class GodotThreading
{
    private static int _mainThreadId;
    
    public static void EstablishMainThread() => _mainThreadId = Thread.CurrentThread.ManagedThreadId;
    
    public static bool IsMainThread => _mainThreadId == Thread.CurrentThread.ManagedThreadId;

    public static void RunInMainThread(Action action)
    {
        if (IsMainThread)
            action.Invoke();
        else
            Callable.From(action.Invoke).CallDeferred();
    }
}