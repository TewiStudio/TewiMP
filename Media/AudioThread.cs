namespace TewiMP.Media;

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TewiMP.Background;
using static Vanara.PInvoke.User32;

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

    // 线程的主循环
    private void ThreadLoop()
    {
        // 设置线程为音频优先级
        uint taskIndex;
        IntPtr handle = AvSetMmThreadCharacteristics("Audio", out taskIndex);

        if (handle == IntPtr.Zero)
            LogManager.Warning("AudioThread", "AvSetMmThreadCharacteristics failed.");
        else
            LogManager.Info("AudioThread", "Audio thread boosted.");

        try
        {
            // GetConsumingEnumerable 会阻塞等待，直到有新任务进来
            foreach (var action in _actionQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    // 这里记录日志，不要让音频线程崩溃
                    LogManager.Error("AudioThread", $"Audio Thread Error: {ex}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 线程被取消，正常退出
            LogManager.Info("AudioThread", "Audio thread canceled.");
            AvRevertMmThreadCharacteristics(handle);
        }
    }

    /// <summary>
    /// 将操作发送到音频线程执行
    /// </summary>
    public void Invoke(Action action)
    {
        if (!_cts.IsCancellationRequested)
        {
            _actionQueue.Add(action);
        }
    }

    /// <summary>
    /// 异步等待操作在音频线程完成 (可选)
    /// </summary>
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
