using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Services;

namespace TewiMP.Services.Media.Audio;

public class AudioThread : IDisposable
{
    private readonly Thread _thread;
    private readonly BlockingCollection<Action> _actionQueue;
    private readonly CancellationTokenSource _cts;

    public AudioThread()
    {
        _actionQueue = new BlockingCollection<Action>();
        _cts = new CancellationTokenSource();

        _thread = new Thread(ThreadLoop)
        {
            Name = "NAudio Playback Thread",
            IsBackground = true // 确保程序关闭时线程自动结束
        };

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    private void ThreadLoop()
    {
        uint taskIndex;
        IntPtr handle = AvSetMmThreadCharacteristics("Audio", out taskIndex);

        if (handle == IntPtr.Zero)
            LogService.Warning("AudioThread", "AvSetMmThreadCharacteristics failed.");
        else
            LogService.Info("AudioThread", "Audio thread boosted.");

        try
        {
            foreach (var action in _actionQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    LogService.Error("AudioThread", $"Audio Thread Error: {ex}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogService.Info("AudioThread", "Audio thread canceled.");
            AvRevertMmThreadCharacteristics(handle);
        }
    }

    public void Invoke(Action action)
    {
        if (!_cts.IsCancellationRequested)
        {
            _actionQueue.Add(action);
        }
    }

    public Task InvokeAsync(Action action)
    {
        var tcs = new TaskCompletionSource();
        Invoke(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _actionQueue.CompleteAdding();
        // _thread.Join(1000); // 待线程结束
        _actionQueue.Dispose();
        _cts.Dispose();
    }

    [DllImport("Avrt.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr AvSetMmThreadCharacteristics(string taskName, out uint taskIndex);

    [DllImport("Avrt.dll")]
    public static extern bool AvRevertMmThreadCharacteristics(IntPtr handle);
}
