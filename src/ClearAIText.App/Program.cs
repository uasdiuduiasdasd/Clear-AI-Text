using ClearAIText.Windows.Process;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;
using WinRT;

namespace ClearAIText.App;

public static class Program
{
    private const string AppMutexName = @"Global\ClearAIText_SingleInstance_App_Mutex";

    [STAThread]
    public static void Main(string[] args)
    {
        using var mutex = new Mutex(true, AppMutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            // Activate existing window if already running
            _ = SingleInstanceHelper.TryActivateExistingWindow("Clear AI Text");
            return;
        }

        ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            var context = new DispatcherQueueSynchronizationContext(dispatcherQueue);
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });

        GC.KeepAlive(mutex);
    }
}

