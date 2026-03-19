using System.Collections.Concurrent;
using System.Diagnostics;
using DiscordTokenChecker.Models;

namespace DiscordTokenChecker.Services;

public class CheckerEngine
{
    private readonly DiscordChecker _checker = new();
    private CancellationTokenSource? _cts;
    private readonly Stopwatch _sw = new();
    private int _checked;

    public event Action<TokenResult>? OnResult;
    public event Action<int, int, int, int, double>? OnStatsUpdate; // valid, invalid, errors, checked, cpm
    public event Action? OnComplete;

    public bool IsRunning { get; private set; }

    public void SetProxies(List<string> proxies) => _checker.SetProxies(proxies);

    public async Task StartAsync(List<string> tokens, int threads)
    {
        if (IsRunning) return;
        IsRunning = true;
        _checked = 0;
        _cts = new CancellationTokenSource();
        _sw.Restart();

        int valid = 0, invalid = 0, errors = 0;
        var semaphore = new SemaphoreSlim(threads, threads);
        var total = tokens.Count;
        var tasks = new List<Task>();

        foreach (var token in tokens)
        {
            if (_cts.IsCancellationRequested) break;
            await semaphore.WaitAsync(_cts.Token);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await _checker.CheckTokenAsync(token.Trim(), _cts.Token);
                    var c = Interlocked.Increment(ref _checked);

                    switch (result.Status)
                    {
                        case CheckStatus.Valid:
                            Interlocked.Increment(ref valid);
                            break;
                        case CheckStatus.Invalid:
                            Interlocked.Increment(ref invalid);
                            break;
                        default:
                            Interlocked.Increment(ref errors);
                            break;
                    }

                    OnResult?.Invoke(result);

                    var elapsed = _sw.Elapsed.TotalMinutes;
                    var cpm = elapsed > 0 ? c / elapsed : 0;
                    OnStatsUpdate?.Invoke(valid, invalid, errors, c, cpm);
                }
                catch (OperationCanceledException) { }
                catch
                {
                    Interlocked.Increment(ref _checked);
                    Interlocked.Increment(ref errors);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        try { await Task.WhenAll(tasks); } catch { }

        _sw.Stop();
        IsRunning = false;
        OnComplete?.Invoke();
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
