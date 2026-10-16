using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace Fx3uMonitor.Services;

/// <summary>
/// 三菱 FX3U 序列埠通訊管理與輪詢引擎
/// </summary>
public class PlcService : IDisposable
{
    private SerialPort? _serialPort;
    private readonly object _syncLock = new();
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    // 待送出之優先命令佇列 (例如強制 ON/OFF)
    private readonly ConcurrentQueue<(byte[] Cmd, string Point, bool TargetState)> _priorityQueue = new();
    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private string? _autoStopOutputPoint;
    private string? _autoStopTriggerInput;
    private int _savedPollInterval = 200;

    // 模擬模式內部狀態
    private readonly Dictionary<string, bool> _simulatedY = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _simulatedX = new(StringComparer.OrdinalIgnoreCase);

    public bool IsConnected { get; private set; }
    public bool IsSimulationMode { get; set; }
    public int PollIntervalMs { get; set; } = 200;
    public string CurrentPortName { get; private set; } = string.Empty;

    /// <summary>
    /// 是否過濾每 200ms 常態輪詢日誌 (僅顯示操作動作與步驟)
    /// </summary>
    public bool FilterPollingLog { get; set; } = true;

    // 統計數據
    public long TxCount { get; private set; }
    public long RxCount { get; private set; }
    public long ErrorCount { get; private set; }

    /// <summary>
    /// 寫入動作步序日誌
    /// </summary>
    public void LogStep(string message)
    {
        OnCommunicationLog?.Invoke("STEP", Array.Empty<byte>(), message);
    }

    // 事件宣告
    public event Action<Dictionary<string, bool>>? OnInputsUpdated;
    public event Action<Dictionary<string, bool>>? OnOutputsUpdated;
    public event Action<string, byte[], string>? OnCommunicationLog; // "TX" | "RX" | "SYS" | "ERR", bytes, info
    public event Action<bool, string>? OnConnectionStateChanged;
    public event Action<long>? OnCycleTimeUpdated;

    public PlcService()
    {
        // 初始化模擬狀態
        foreach (var p in Fx3uProtocol.PointNames)
        {
            _simulatedY[$"Y{p}"] = false;
            _simulatedX[$"X{p}"] = false;
        }
    }

    /// <summary>
    /// 連線至指定序列埠或啟動模擬模式
    /// </summary>
    public bool Connect(string portName, bool simulation = false)
    {
        Disconnect();

        IsSimulationMode = simulation;
        CurrentPortName = portName;

        if (IsSimulationMode)
        {
            IsConnected = true;
            OnCommunicationLog?.Invoke("SYS", Array.Empty<byte>(), "已啟動 PLC 離線模擬模式 (無需實體硬體)");
            OnConnectionStateChanged?.Invoke(true, "模擬連線中");
            StartPolling();
            return true;
        }

        try
        {
            _serialPort = new SerialPort(portName, 9600, Parity.Even, 7, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 600,
                WriteTimeout = 600,
                DtrEnable = true,
                RtsEnable = true
            };

            _serialPort.Open();
            IsConnected = true;

            OnCommunicationLog?.Invoke("SYS", Array.Empty<byte>(), $"序列埠 {portName} 已開啟 (9600, 7, Even, 1)");
            OnConnectionStateChanged?.Invoke(true, $"已連線至 {portName}");

            StartPolling();
            return true;
        }
        catch (Exception ex)
        {
            ErrorCount++;
            IsConnected = false;
            OnCommunicationLog?.Invoke("ERR", Array.Empty<byte>(), $"開啟序列埠失敗: {ex.Message}");
            OnConnectionStateChanged?.Invoke(false, $"連線失敗: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 斷開連線
    /// </summary>
    public void Disconnect()
    {
        StopPolling();

        lock (_syncLock)
        {
            if (_serialPort != null)
            {
                try
                {
                    if (_serialPort.IsOpen)
                        _serialPort.Close();
                }
                catch
                {
                    // 忽略關閉時可能的例外
                }
                finally
                {
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }

        bool wasConnected = IsConnected;
        IsConnected = false;

        if (wasConnected)
        {
            OnCommunicationLog?.Invoke("SYS", Array.Empty<byte>(), "通訊已中斷");
            OnConnectionStateChanged?.Invoke(false, "未連線");
        }
    }

    /// <summary>
    /// 請求對指定 Y 輸出接點進行強制 ON 或 OFF
    /// </summary>
    public void ForceY(string pointName, bool targetState)
    {
        if (IsSimulationMode)
        {
            _simulatedY[pointName] = targetState;
        }
        byte[] cmd = Fx3uProtocol.GetForceYCommand(pointName, targetState);
        _priorityQueue.Enqueue((cmd, pointName, targetState));

        // 立即喚醒輪詢緒，零延遲送出控制指令
        if (_wakeSignal.CurrentCount == 0)
        {
            try { _wakeSignal.Release(); } catch { }
        }
    }

    /// <summary>
    /// 設定馬達極速聯鎖防衝極限：一旦輸入端點達到指定狀態，在解碼當下立即發送停止命令，防止通訊延遲跑過頭
    /// </summary>
    public void ArmMotorAutoStop(string outputPoint, string triggerInput)
    {
        _autoStopOutputPoint = outputPoint;
        _autoStopTriggerInput = triggerInput;
        _savedPollInterval = PollIntervalMs;
        PollIntervalMs = 25; // 進入極速輪詢 (25ms)，極限縮減端點檢知延遲
        if (_wakeSignal.CurrentCount == 0)
        {
            try { _wakeSignal.Release(); } catch { }
        }
    }

    public void DisarmMotorAutoStop()
    {
        _autoStopOutputPoint = null;
        _autoStopTriggerInput = null;
        PollIntervalMs = _savedPollInterval;
    }

    private async Task CheckAndExecuteAutoStopAsync(Dictionary<string, bool> points, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_autoStopOutputPoint) && !string.IsNullOrEmpty(_autoStopTriggerInput))
        {
            if (points.TryGetValue(_autoStopTriggerInput, out bool isHit) && isHit)
            {
                string outPt = _autoStopOutputPoint;
                string inPt = _autoStopTriggerInput;
                _autoStopOutputPoint = null;
                _autoStopTriggerInput = null;

                // 毫秒級立刻切斷馬達輸出！
                if (IsSimulationMode)
                {
                    _simulatedY[outPt] = false;
                }
                else
                {
                    byte[] stopCmd = Fx3uProtocol.GetForceYCommand(outPt, false);
                    await SendAndReceiveAsync(stopCmd, 1, 300, ct);
                }
                TxCount++;
                OnCommunicationLog?.Invoke("STEP", Array.Empty<byte>(), $"[極限防衝保護] 檢知 {inPt} 到位，瞬間切斷 {outPt} 電源，防止跑過頭");
            }
        }
    }

    /// <summary>
    /// 供測試用：在模擬模式下改變 X 輸入接點
    /// </summary>
    public void SetSimulatedX(string pointName, bool state)
    {
        if (_simulatedX.ContainsKey(pointName))
        {
            _simulatedX[pointName] = state;
        }
    }

    /// <summary>
    /// 供測試用：在模擬模式下取得 X 輸入接點狀態
    /// </summary>
    public bool GetSimulatedX(string pointName)
    {
        return _simulatedX.TryGetValue(pointName, out bool val) && val;
    }

    /// <summary>
    /// 供測試用：在模擬模式下取得 Y 輸出接點狀態
    /// </summary>
    public bool GetSimulatedY(string pointName)
    {
        return _simulatedY.TryGetValue(pointName, out bool val) && val;
    }

    private void StartPolling()
    {
        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(() => PollingLoopAsync(_pollCts.Token));
    }

    private void StopPolling()
    {
        if (_pollCts != null)
        {
            _pollCts.Cancel();
            try
            {
                _pollTask?.Wait(1000);
            }
            catch
            {
                // 忽略取消例外
            }
            _pollCts.Dispose();
            _pollCts = null;
            _pollTask = null;
        }
    }

    private async Task PollingLoopAsync(CancellationToken ct)
    {
        var stopwatch = new Stopwatch();

        while (!ct.IsCancellationRequested && IsConnected)
        {
            stopwatch.Restart();

            try
            {
                // 1. 處理待發送的優先控制指令 (例如 Force Y)
                while (_priorityQueue.TryDequeue(out var req))
                {
                    await ExecuteForceYCommandAsync(req.Cmd, req.Point, req.TargetState, ct);
                    await Task.Delay(20, ct);
                }

                // 2. 讀取 Y0~Y17 輸出狀態
                await QueryOutputsAsync(ct);
                await Task.Delay(20, ct);

                // 3. 讀取 X0~X17 輸入狀態
                await QueryInputsAsync(ct);

                stopwatch.Stop();
                OnCycleTimeUpdated?.Invoke(stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorCount++;
                OnCommunicationLog?.Invoke("ERR", Array.Empty<byte>(), $"輪詢異常: {ex.Message}");
            }

            int delay = Math.Max(10, PollIntervalMs - (int)stopwatch.ElapsedMilliseconds);
            try
            {
                await _wakeSignal.WaitAsync(delay, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ExecuteForceYCommandAsync(byte[] cmd, string point, bool targetState, CancellationToken ct)
    {
        if (IsSimulationMode)
        {
            TxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("TX", cmd, $"[模擬] 強制 {point} -> {(targetState ? "ON" : "OFF")}");
            _simulatedY[point] = targetState;
            RxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("RX", new byte[] { Fx3uProtocol.ACK }, $"[模擬] PLC 回應 ACK (0x06)");
            return;
        }

        byte[]? response = await SendAndReceiveAsync(cmd, 1, 600, ct);
        TxCount++;
        if (!FilterPollingLog)
            OnCommunicationLog?.Invoke("TX", cmd, $"強制 {point} -> {(targetState ? "ON" : "OFF")}");

        if (response != null && response.Length > 0)
        {
            RxCount++;
            if (!FilterPollingLog)
            {
                string respDesc = response[0] == Fx3uProtocol.ACK ? "PLC 回應 ACK (成功)" :
                                  response[0] == Fx3uProtocol.NAK ? "PLC 回應 NAK (失敗)" :
                                  "PLC 回應未知代碼";
                OnCommunicationLog?.Invoke("RX", response, respDesc);
            }
        }
        else
        {
            ErrorCount++;
            OnCommunicationLog?.Invoke("ERR", Array.Empty<byte>(), $"強制 {point} 逾時無回應");
        }
    }

    private async Task QueryOutputsAsync(CancellationToken ct)
    {
        byte[] cmd = Fx3uProtocol.ReadYCommand;

        if (IsSimulationMode)
        {
            TxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("TX", cmd, "讀取 Y0~Y17");
            byte[] simResp = Fx3uProtocol.EncodeBitPointsResponse(_simulatedY, 'Y');
            RxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("RX", simResp, "PLC 回傳 Y0~Y17 狀態");
            var points = Fx3uProtocol.ParseBitPointsResponse(simResp, 'Y');
            OnOutputsUpdated?.Invoke(points);
            return;
        }

        TxCount++;
        if (!FilterPollingLog)
            OnCommunicationLog?.Invoke("TX", cmd, "讀取 Y0~Y17");

        // 期待 8 位元組: 02 [B0] [B1] [B2] [B3] 03 [SUM1] [SUM2]
        byte[]? response = await SendAndReceiveAsync(cmd, 8, 500, ct);

        if (response != null && response.Length >= 6)
        {
            RxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("RX", response, "PLC 回傳 Y0~Y17");
            var points = Fx3uProtocol.ParseBitPointsResponse(response, 'Y');
            OnOutputsUpdated?.Invoke(points);
        }
        else
        {
            ErrorCount++;
            OnCommunicationLog?.Invoke("ERR", Array.Empty<byte>(), "讀取 Y0~Y17 逾時或長度不足");
        }
    }

    private async Task QueryInputsAsync(CancellationToken ct)
    {
        byte[] cmd = Fx3uProtocol.ReadXCommand;

        if (IsSimulationMode)
        {
            TxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("TX", cmd, "讀取 X0~X17");
            byte[] simResp = Fx3uProtocol.EncodeBitPointsResponse(_simulatedX, 'X');
            RxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("RX", simResp, "PLC 回傳 X0~X17 狀態");
            var points = Fx3uProtocol.ParseBitPointsResponse(simResp, 'X');
            await CheckAndExecuteAutoStopAsync(points, ct);
            OnInputsUpdated?.Invoke(points);
            return;
        }

        TxCount++;
        if (!FilterPollingLog)
            OnCommunicationLog?.Invoke("TX", cmd, "讀取 X0~X17");

        byte[]? response = await SendAndReceiveAsync(cmd, 8, 500, ct);

        if (response != null && response.Length >= 6)
        {
            RxCount++;
            if (!FilterPollingLog)
                OnCommunicationLog?.Invoke("RX", response, "PLC 回傳 X0~X17");
            var points = Fx3uProtocol.ParseBitPointsResponse(response, 'X');
            await CheckAndExecuteAutoStopAsync(points, ct);
            OnInputsUpdated?.Invoke(points);
        }
        else
        {
            ErrorCount++;
            OnCommunicationLog?.Invoke("ERR", Array.Empty<byte>(), "讀取 X0~X17 逾時或長度不足");
        }
    }

    /// <summary>
    /// 發送封包並等待接收指定長度的資料
    /// </summary>
    private async Task<byte[]?> SendAndReceiveAsync(byte[] cmd, int expectedBytes, int timeoutMs, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            lock (_syncLock)
            {
                if (_serialPort == null || !_serialPort.IsOpen)
                    return null;

                try
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();
                    _serialPort.Write(cmd, 0, cmd.Length);

                    var buffer = new List<byte>(expectedBytes);
                    var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

                    while (DateTime.UtcNow < deadline && buffer.Count < expectedBytes)
                    {
                        if (ct.IsCancellationRequested) return null;

                        int available = _serialPort.BytesToRead;
                        if (available > 0)
                        {
                            byte[] chunk = new byte[available];
                            int read = _serialPort.Read(chunk, 0, available);
                            for (int i = 0; i < read; i++)
                            {
                                buffer.Add(chunk[i]);
                            }

                            // 如果已讀到 ETX + 2 檢查碼，即可提前結束
                            if (expectedBytes > 1 && buffer.Contains(Fx3uProtocol.ETX))
                            {
                                int etxIdx = buffer.IndexOf(Fx3uProtocol.ETX);
                                if (buffer.Count >= etxIdx + 3)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
                    }

                    return buffer.Count > 0 ? buffer.ToArray() : null;
                }
                catch
                {
                    return null;
                }
            }
        }, ct);
    }

    public void ResetCounters()
    {
        TxCount = 0;
        RxCount = 0;
        ErrorCount = 0;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
