using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fx3uMonitor.Services;

/// <summary>
/// 技術士技能檢定機電整合丙級術科測試 - 第三站：凸輪升降與旋轉取放料機構自動化動作引擎
/// </summary>
public class MechatronicsLevel3Service
{
    private readonly PlcService _plcService;
    private CancellationTokenSource? _cts;
    private Task? _runningTask;

    // 跑馬燈測試任務
    private CancellationTokenSource? _marqueeCts;
    private Task? _marqueeTask;

    public bool IsRunning { get; private set; }
    public bool IsMarqueeRunning { get; private set; }
    public int PieceCount { get; private set; }
    public int TargetPieces { get; set; } = 4;
    public string CurrentStepDescription { get; private set; } = "待機狀態 (原點)";
    public int CurrentStepNumber { get; private set; } = 0;
    public int TotalSteps => 10;

    // 調機功能暫停旗標
    private bool _isPausedAtHash = false;

    // 缺料暫停等待人工補料旗標 (連續模式)
    private bool _isWaitingForMaterialReload = false;
    public bool IsWaitingForMaterialReload => _isWaitingForMaterialReload;

    // X10 脈衝監控計數 (推料缸推出過程中感測次數)
    private int _x10PulseCount = 0;
    private bool _lastX10Val = false;
    private bool _isMonitoringX10 = false;

    // 供測試或手動覆寫模擬模式下的 X10 脈衝數 (-1 代表依料件自動交替 2 次與 1 次)
    public int SimulatedX10Pulses { get; set; } = -1;

    // 事件
    public event Action<int, int, string>? OnStepProgress;    // (step, total, desc)
    public event Action<int, int>? OnPieceCountUpdated;        // (current, target)
    public event Action<bool, string>? OnExecutionStateChanged;// (isRunning, stateText)
    public event Action<bool>? OnMarqueeStateChanged;          // (isMarqueeRunning)
    public event Action<string>? OnLogAction;                  // 動作日誌字串

    public MechatronicsLevel3Service(PlcService plcService)
    {
        _plcService = plcService;
        _plcService.OnInputsUpdated += ProvideCurrentInputs;
    }

    /// <summary>
    /// 解除連續模式下的缺料暫停 (人工補料並按下 ST/RST 鈕後繼續)
    /// </summary>
    public void ResumeFromMaterialReload()
    {
        if (_isWaitingForMaterialReload)
        {
            _isWaitingForMaterialReload = false;
            OnLogAction?.Invoke("[人工補料] 收到 ST/RST 信號，解除缺料暫停並繼續執行連續循環");
        }
    }

    /// <summary>
    /// 啟動機電丙三動作流程
    /// </summary>
    public void Start()
    {
        if (IsRunning) return;

        if (!_plcService.IsConnected)
        {
            throw new InvalidOperationException("請先連線至 PLC 或啟動模擬模式！");
        }

        if (IsMarqueeRunning)
        {
            StopMarqueeTest();
        }

        _cts = new CancellationTokenSource();
        IsRunning = true;
        _isWaitingForMaterialReload = false;
        PieceCount = 0;

        OnExecutionStateChanged?.Invoke(true, "機電丙三運轉中");
        _runningTask = Task.Run(() => SequenceLoopAsync(_cts.Token));
    }

    /// <summary>
    /// 停止機電丙三運轉
    /// </summary>
    public void Stop()
    {
        if (!IsRunning) return;

        _isWaitingForMaterialReload = false;
        _cts?.Cancel();
        try
        {
            _runningTask?.Wait(1000);
        }
        catch
        {
            // 忽略取消例外
        }

        IsRunning = false;
        OnExecutionStateChanged?.Invoke(false, "已停止");
        OnLogAction?.Invoke("[停止] 機電丙三動作流程已手動停止");
    }

    /// <summary>
    /// 啟動跑馬燈測試：執行 (黃綠滅紅亮1s -> 綠紅滅黃亮1s -> 紅黃滅綠亮1s) 3個循環，之後三燈全滅
    /// </summary>
    public void StartMarqueeTest()
    {
        if (IsMarqueeRunning) return;

        if (!_plcService.IsConnected)
        {
            throw new InvalidOperationException("請先連線至 PLC 或啟動模擬模式！");
        }

        if (IsRunning)
        {
            throw new InvalidOperationException("機電丙三流程正在運轉中，請先停止後再執行跑馬燈測試！");
        }

        _marqueeCts = new CancellationTokenSource();
        IsMarqueeRunning = true;
        OnMarqueeStateChanged?.Invoke(true);

        _marqueeTask = Task.Run(() => ExecuteMarqueeTestAsync(_marqueeCts.Token));
    }

    /// <summary>
    /// 停止跑馬燈測試
    /// </summary>
    public void StopMarqueeTest()
    {
        if (!IsMarqueeRunning) return;

        _marqueeCts?.Cancel();
        try
        {
            _marqueeTask?.Wait(500);
        }
        catch
        {
            // 忽略取消例外
        }

        IsMarqueeRunning = false;

        // 確保三燈全滅
        _plcService.ForceY("Y15", false);
        _plcService.ForceY("Y16", false);
        _plcService.ForceY("Y17", false);

        OnMarqueeStateChanged?.Invoke(false);
        OnLogAction?.Invoke("[跑馬燈] 測試已手動停止，三燈全滅");

        // 依當時機台狀態恢復燈號
        RestoreIndicatorLights();
    }

    /// <summary>
    /// 執行跑馬燈測試 3 個循環
    /// </summary>
    public async Task ExecuteMarqueeTestAsync(CancellationToken ct)
    {
        OnLogAction?.Invoke("[跑馬燈] 測試開始：執行 3 次循環（紅燈1s -> 黃燈1s -> 綠燈1s）");

        try
        {
            for (int cycle = 1; cycle <= 3; cycle++)
            {
                if (ct.IsCancellationRequested) break;

                // 1. 黃、綠燈滅同時紅燈亮 1 秒
                _plcService.ForceY("Y16", false); // YL 滅
                _plcService.ForceY("Y17", false); // GL 滅
                _plcService.ForceY("Y15", true);  // RL 亮
                OnLogAction?.Invoke($"[跑馬燈] 循環 {cycle}/3：黃/綠燈滅，紅燈亮 (計時 1 秒)");
                await Task.Delay(1000, ct);

                // 2. 綠、紅燈滅同時黃燈亮 1 秒
                _plcService.ForceY("Y17", false); // GL 滅
                _plcService.ForceY("Y15", false); // RL 滅
                _plcService.ForceY("Y16", true);  // YL 亮
                OnLogAction?.Invoke($"[跑馬燈] 循環 {cycle}/3：綠/紅燈滅，黃燈亮 (計時 1 秒)");
                await Task.Delay(1000, ct);

                // 3. 紅、黃燈滅同時綠燈亮 1 秒
                _plcService.ForceY("Y15", false); // RL 滅
                _plcService.ForceY("Y16", false); // YL 滅
                _plcService.ForceY("Y17", true);  // GL 亮
                OnLogAction?.Invoke($"[跑馬燈] 循環 {cycle}/3：紅/黃燈滅，綠燈亮 (計時 1 秒)");
                await Task.Delay(1000, ct);
            }

            // 3 個循環結束後，3 個燈全滅
            _plcService.ForceY("Y15", false);
            _plcService.ForceY("Y16", false);
            _plcService.ForceY("Y17", false);
            OnLogAction?.Invoke("[跑馬燈] 3 個循環測試完成，三燈全滅");

            // 依當時程式與機台狀況，點亮或熄滅相應的燈
            await Task.Delay(600, ct);
            RestoreIndicatorLights();
        }
        catch (OperationCanceledException)
        {
            _plcService.ForceY("Y15", false);
            _plcService.ForceY("Y16", false);
            _plcService.ForceY("Y17", false);
            OnLogAction?.Invoke("[跑馬燈] 測試已中斷，三燈全滅");
        }
        catch (Exception ex)
        {
            _plcService.ForceY("Y15", false);
            _plcService.ForceY("Y16", false);
            _plcService.ForceY("Y17", false);
            OnLogAction?.Invoke($"[跑馬燈異常] {ex.Message}");
            RestoreIndicatorLights();
        }
        finally
        {
            IsMarqueeRunning = false;
            OnMarqueeStateChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// 調機功能模式下，使用者按下 ST/RST 繼續下一步序
    /// </summary>
    public void ResumeJog()
    {
        if (_isPausedAtHash)
        {
            _isPausedAtHash = false;
            OnLogAction?.Invoke("[調機繼續] 按下 ST/RST 鈕，接續執行下一步序");
        }
    }

    /// <summary>
    /// 主序列循環任務
    /// </summary>
    private async Task SequenceLoopAsync(CancellationToken ct)
    {
        try
        {
            // (一) 檢查機械原點與待機狀態
            await CheckAndPrepareOriginAsync(ct);

            // 判斷 COS1 (工作模式 vs 復歸模式)
            bool isWorkMode = _plcService.IsSimulationMode || GetInputState("X13");
            if (!isWorkMode)
            {
                OnLogAction?.Invoke("[復歸模式] 選擇開關 COS1 為復歸模式，執行自動復歸程序...");
                await ExecuteHomingAsync(ct);
                IsRunning = false;
                OnExecutionStateChanged?.Invoke(false, "復歸完成，進入待機");
                return;
            }

            // 判斷 COS2 (單一循環 vs 連續循環 vs 調機功能)
            bool isJogMode = GetInputState("X14");       // COS2_L (指定功能 / 調機)
            bool isContinuous = GetInputState("X15");    // COS2_R (連續循環)

            string cycleModeName = isJogMode ? "調機功能 (指定步進)" :
                                   isContinuous ? "連續循環 (4 個工件)" :
                                   "單一循環 (1 個工件)";

            OnLogAction?.Invoke($"[機電丙三啟動] 運轉模式：{cycleModeName}");

            // 運轉指示燈：RL 亮 (Y15 ON)，GL 滅 (Y17 OFF)
            _plcService.ForceY("Y15", true);  // RL 運轉燈 ON
            _plcService.ForceY("Y17", false); // GL 待機燈 OFF
            await Task.Delay(200, ct);

            int maxPieces = isContinuous ? TargetPieces : 1;

            while (!ct.IsCancellationRequested && PieceCount < maxPieces)
            {
                // 檢查 EMS 急停 (X17 為 NC 接點，導通為正常，斷路為急停)
                if (!_plcService.IsSimulationMode && !GetInputState("X17"))
                {
                    await HandleEmergencyStopAsync(ct);
                    return;
                }

                if (isContinuous)
                {
                    OnLogAction?.Invoke($"[連續循環] 開始處理第 {PieceCount + 1} / {maxPieces} 個料件");
                }

                // 執行單一料件取放完整步驟
                var result = await ExecuteSingleWorkpieceProcessAsync(isJogMode, isContinuous, maxPieces, ct);

                if (result == WorkpieceProcessResult.Completed)
                {
                    PieceCount++;
                    OnPieceCountUpdated?.Invoke(PieceCount, maxPieces);

                    if (isContinuous && PieceCount < maxPieces)
                    {
                        OnLogAction?.Invoke($"[連續循環] 第 {PieceCount} 個料件完成，接續下一個料件...");
                        await Task.Delay(400, ct);
                    }
                }
                else if (result == WorkpieceProcessResult.NoMaterialExit)
                {
                    // 單一或指定功能下，推料缸後退，回到待機狀態
                    OnLogAction?.Invoke($"[缺料結束] {cycleModeName} 因未檢測到料件，結束運轉回到待機狀態 (GL 亮)");
                    break;
                }
                else if (result == WorkpieceProcessResult.NoMaterialReloaded)
                {
                    // 連續功能下，已補充料件並按下 ST，PieceCount 保留，接續處理下一個料件
                    OnLogAction?.Invoke($"[缺料續行] 人工補料完成 (目前已完成 {PieceCount} / {maxPieces} 件)，接續執行連續循環推料");
                    await Task.Delay(400, ct);
                }
            }

            // 循環完成或單一結束，回到待機
            _plcService.ForceY("Y15", false); // RL 運轉燈 OFF
            _plcService.ForceY("Y17", true);  // GL 待機燈 ON

            CurrentStepDescription = "流程結束，機構待機";
            CurrentStepNumber = TotalSteps;
            OnStepProgress?.Invoke(TotalSteps, TotalSteps, CurrentStepDescription);

            OnLogAction?.Invoke($"[完成] {cycleModeName} 執行完畢，回到待機狀態 (GL 亮)");
        }
        catch (OperationCanceledException)
        {
            OnLogAction?.Invoke("[中斷] 動作序列已取消");
        }
        catch (Exception ex)
        {
            OnLogAction?.Invoke($"[異常] {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            OnExecutionStateChanged?.Invoke(false, "待機中");
        }
    }

    private enum WorkpieceProcessResult
    {
        Completed,          // 取放正常完成
        NoMaterialExit,     // 缺料：單一/指定模式，退料缸回待機
        NoMaterialReloaded  // 缺料：連續模式，退料缸暫停，補料按 ST 後接續
    }

    /// <summary>
    /// 執行單一料件取放的 10 大標準動作步序
    /// </summary>
    private async Task<WorkpieceProcessResult> ExecuteSingleWorkpieceProcessAsync(bool isJogMode, bool isContinuous, int maxPieces, CancellationToken ct)
    {
        // ----------------------------------------------------
        // Step 1: 推料缸 A 推出料件至前端治具，並監控 X10 感測器次數
        // ----------------------------------------------------
        UpdateStep(1, "推料氣壓缸 A 伸出 (Y0 ON)，推送料件至前端治具並感測 X10");
        _x10PulseCount = 0;
        _lastX10Val = GetInputState("X10");
        _isMonitoringX10 = true;
        _plcService.ForceY("Y0", true);

        // 模擬模式下的脈衝產生
        if (_plcService.IsSimulationMode)
        {
            int targetSimPulses = SimulatedX10Pulses >= 0 ? SimulatedX10Pulses : ((PieceCount % 2 == 0) ? 2 : 1);
            _x10PulseCount = targetSimPulses;
            if (targetSimPulses > 0)
            {
                OnLogAction?.Invoke($"[模擬感測] X10 料件方向感測器檢知脈衝次數: {targetSimPulses} 次 (方向判定: {(targetSimPulses == 2 ? "-90°" : "+90°")})");
            }
            else
            {
                OnLogAction?.Invoke("[模擬感測] X10 料件方向感測器未檢知脈衝 (0 次，判定缺料)");
            }
        }

        await WaitForInputAsync("X0", true, "推料缸前端點 a1", 3000, ct);
        await Task.Delay(400, ct);
        _isMonitoringX10 = false;

        int detectedPulses = _x10PulseCount;
        if (detectedPulses == 0 && GetInputState("X10"))
        {
            detectedPulses = 1;
        }

        // ----------------------------------------------------
        // 判別是否缺料 (X10 未感應到料件)
        // ----------------------------------------------------
        if (detectedPulses == 0)
        {
            UpdateStep(1, "缺料檢知：X10 未感應到料件，推料缸縮回後端點 a0");
            _plcService.ForceY("Y0", false);
            await WaitForInputAsync("X1", true, "推料缸後端點 a0", 3000, ct);
            await Task.Delay(200, ct);

            if (!isContinuous)
            {
                // 單一或指定功能下，推料缸後退，回到待機狀態
                OnLogAction?.Invoke("[缺料處理] X10 未感應到料件，單一/指定模式下推料缸後退回後位，結束流程回到待機狀態 (GL 亮)");
                return WorkpieceProcessResult.NoMaterialExit;
            }
            else
            {
                // 連續功能下，推料缸後退並暫停動作(不清除已做料件數)，等待人工放入料件並按下ST後，才可繼續作動
                OnLogAction?.Invoke($"[缺料暫停] X10 未感應到料件，連續模式暫停運轉 (目前已完成 {PieceCount} / {maxPieces} 件，計數保留)。請手動補充料件並按下 ST/RST 鈕繼續！");
                _isWaitingForMaterialReload = true;
                OnExecutionStateChanged?.Invoke(true, $"缺料暫停 (已完成 {PieceCount}/{maxPieces} 件，請補料按 ST)");

                while (_isWaitingForMaterialReload && !ct.IsCancellationRequested)
                {
                    if (GetInputState("X16"))
                    {
                        _isWaitingForMaterialReload = false;
                        break;
                    }
                    await Task.Delay(100, ct);
                }

                OnExecutionStateChanged?.Invoke(true, "機電丙三運轉中");
                return WorkpieceProcessResult.NoMaterialReloaded;
            }
        }

        // ----------------------------------------------------
        // Step 2: 迴轉缸轉至取料方向
        // X10 感測到 2 次 -> 轉 -90° (Y4 ON)；X10 僅感測到 1 次 -> 轉 +90° (Y3 ON)
        // ----------------------------------------------------
        bool isPulse2 = (detectedPulses >= 2);
        string rotatePoint = isPulse2 ? "Y4" : "Y3";
        string rotateSensor = isPulse2 ? "X4" : "X3";
        string rotateDesc = isPulse2
            ? "感測器 X10 感測到 2 次 -> 迴轉缸轉至 -90° 取料方向 (Y4: B2 ON)"
            : "感測器 X10 僅感測到 1 次 -> 迴轉缸轉至 +90° 取料方向 (Y3: B1 ON)";

        UpdateStep(2, rotateDesc);
        _plcService.ForceY(rotatePoint, true);
        await WaitForInputAsync(rotateSensor, true, isPulse2 ? "迴轉缸 -90° 端點 b2" : "迴轉缸 +90° 端點 b1", 3000, ct);
        await Task.Delay(200, ct);

        // ----------------------------------------------------
        // Step 3: 垂直升降模組馬達旋轉 R1 下降至下端點 d1
        // ----------------------------------------------------
        UpdateStep(3, "升降模組馬達旋轉 R1 (Y7 ON) 下降至下端點 d1 取料");
        await RunMotorToLimitAsync("X7", "升降模組下端點 d1", 4000, ct);

        // 【調機功能第 1 個 '#' 暫停點】：升降馬達旋轉下降 與 真空吸盤吸 之間
        if (isJogMode)
        {
            await PauseForJogAsync("#1 升降模組下降完成，暫停於吸盤吸取前 (按 ST/RST 繼續)", ct);
        }

        // ----------------------------------------------------
        // Step 4: 真空吸盤吸 C+，真空壓力開關 ps1 建立負壓
        // ----------------------------------------------------
        UpdateStep(4, "真空吸盤吸 C+ (Y5 ON)，負壓建立 (X5: ps1)");
        _plcService.ForceY("Y6", false); // 確保放料電磁閥關閉
        _plcService.ForceY("Y5", true);  // 吸盤吸 C+
        await WaitForInputAsync("X5", true, "真空壓力開關 ps1 負壓建立", 2500, ct);
        await Task.Delay(200, ct);

        // ----------------------------------------------------
        // Step 5: 之後推料缸後退
        // ----------------------------------------------------
        UpdateStep(5, "推料氣壓缸 A 縮回後端點 a0 (Y0 OFF)");
        _plcService.ForceY("Y0", false);
        await WaitForInputAsync("X1", true, "推料缸後端點 a0", 3000, ct);
        await Task.Delay(200, ct);

        // ----------------------------------------------------
        // Step 6: 垂直升降模組馬達旋轉 R1 上升回上端點 d0
        // ----------------------------------------------------
        UpdateStep(6, "升降模組馬達旋轉 R1 (Y7 ON) 上升回上端點 d0 (吸持料件)");
        await RunMotorToLimitAsync("X6", "升降模組上端點 d0", 4000, ct);

        // ----------------------------------------------------
        // Step 7: 迴轉缸轉至 0° 排料位置 (Y3、Y4 訊號持續輸出)
        // 轉到 0° 需等待 1 秒後才可進行下一動作，且 Y3、Y4 訊號持續輸出直到吸盤放料完成
        // ----------------------------------------------------
        UpdateStep(7, "迴轉缸轉至 0° 排料位 (同時啟動 Y3 與 Y4，訊號持續輸出，等待 1 秒確保定位)");
        await RotateToZeroDegreesAsync(keepEnergized: true, ct);

        // ----------------------------------------------------
        // Step 8: 升降模組馬達旋轉 R1 下降至排料位 (此時 Y3、Y4 持續維持氣壓支撐)
        // ----------------------------------------------------
        UpdateStep(8, "升降模組馬達旋轉 R1 (Y7 ON) 下降至排料位 d1 (Y3/Y4 維持氣壓支撐)");
        await RunMotorToLimitAsync("X7", "升降模組下端點 d1 (排料位)", 4000, ct);

        // 【調機功能第 2 個 '#' 暫停點】：升降馬達下降排料 與 吸盤放 之間
        if (isJogMode)
        {
            await PauseForJogAsync("#2 升降模組下降至出料位，暫停於吸盤釋放前 (按 ST/RST 繼續)", ct);
        }

        // ----------------------------------------------------
        // Step 9: 真空吸盤放料 C-，料件由出料斜坡滑下，放料完成後停止輸出 Y3、Y4
        // ----------------------------------------------------
        UpdateStep(9, "真空吸盤放 C- (Y6 ON)，料件由出料斜坡滑下排料，完成後關閉 Y3、Y4");
        _plcService.ForceY("Y5", false); // 吸盤停止吸
        _plcService.ForceY("Y6", true);  // 破真空放料 C-
        if (_plcService.IsSimulationMode) _plcService.SetSimulatedX("X5", false);
        await Task.Delay(600, ct);
        _plcService.ForceY("Y6", false);

        // 吸盤放料完成後，停止輸出 Y3 與 Y4
        _plcService.ForceY("Y3", false);
        _plcService.ForceY("Y4", false);
        OnLogAction?.Invoke("[氣壓解除] 料件已排出，停止輸出迴轉缸 Y3、Y4 支撐訊號");
        await Task.Delay(200, ct);

        // ----------------------------------------------------
        // Step 10: 升降模組馬達旋轉 R1 上升回上端點 d0
        // ----------------------------------------------------
        UpdateStep(10, "升降模組馬達旋轉 R1 (Y7 ON) 上升回上端點 d0");
        await RunMotorToLimitAsync("X6", "升降模組上端點 d0", 4000, ct);

        OnLogAction?.Invoke($"[排料完成] 第 {PieceCount + 1} 個料件成功排料，機構回到機械原點待機");
        return WorkpieceProcessResult.Completed;
    }

    /// <summary>
    /// 迴轉缸轉至 0°：因機台無轉至 0° 輔助定位機構，故同時啟動 Y3、Y4 並計時 1 秒鐘，以確保迴轉缸轉至 0°。
    /// 若 keepEnergized 為 true，則訊號持續輸出直到料件排出後才關閉。
    /// </summary>
    public async Task RotateToZeroDegreesAsync(bool keepEnergized, CancellationToken ct)
    {
        // 同時啟動 Y3 (B1: 0°/+90°) 與 Y4 (B2: 0°/-90°)
        _plcService.ForceY("Y3", true);
        _plcService.ForceY("Y4", true);

        // 轉到 0° 需等待 1 秒後，才可進行下一動作 (此時 Y3、Y4 訊號持續)
        await Task.Delay(1000, ct);

        // 等待 0° 端點感測器 b0 (X2)
        await WaitForInputAsync("X2", true, "迴轉缸 0° 端點 b0", 3000, ct);

        if (!keepEnergized)
        {
            _plcService.ForceY("Y3", false);
            _plcService.ForceY("Y4", false);
        }
        await Task.Delay(100, ct);
    }

    /// <summary>
    /// 迴轉缸轉至 0° (不保持通電)
    /// </summary>
    public async Task RotateToZeroDegreesAsync(CancellationToken ct)
    {
        await RotateToZeroDegreesAsync(false, ct);
    }

    /// <summary>
    /// 自動復歸程序 (Homing)
    /// 復歸燈 YL 以 0.5s 亮 / 0.5s 滅閃爍 3 秒以上，依序回原點
    /// </summary>
    public async Task ExecuteHomingAsync(CancellationToken ct)
    {
        OnLogAction?.Invoke("[復歸模式] 復歸燈 YL (Y16) 閃爍中 (0.5s 亮/滅，至少 3 秒)，機構依序回原點...");

        // 啟動 YL 閃爍背景工作
        using var flashCts = new CancellationTokenSource();
        var flashTask = Task.Run(async () =>
        {
            while (!flashCts.Token.IsCancellationRequested)
            {
                _plcService.ForceY("Y16", true);
                await Task.Delay(500, flashCts.Token);
                _plcService.ForceY("Y16", false);
                await Task.Delay(500, flashCts.Token);
            }
        });

        try
        {
            // 1. 吸盤放料停止
            _plcService.ForceY("Y5", false);
            _plcService.ForceY("Y6", false);

            // 2. 升降模組上升至上位 d0 (X6)
            OnLogAction?.Invoke("[復歸步驟 1/3] 垂直升降模組馬達上升回上位 d0 (X6)...");
            await RunMotorToLimitAsync("X6", "升降模組上端點 d0", 5000, ct);

            // 3. 迴轉缸轉回 0° (同時啟動 Y3 與 Y4，計時 1 秒)
            OnLogAction?.Invoke("[復歸步驟 2/3] 迴轉缸轉回 0° (同時啟動 Y3 與 Y4，計時 1 秒)...");
            await RotateToZeroDegreesAsync(ct);

            // 4. 推料缸縮回後端點 a0 (X1)
            OnLogAction?.Invoke("[復歸步驟 3/3] 推料氣壓缸 A 縮回後端點 a0 (X1)...");
            _plcService.ForceY("Y0", false);
            await WaitForInputAsync("X1", true, "推料缸後端點 a0", 4000, ct);

            // 確保閃爍滿 3 秒
            await Task.Delay(1000, ct);
        }
        finally
        {
            flashCts.Cancel();
            await Task.Delay(100, ct);
            _plcService.ForceY("Y16", false); // 復歸燈滅
        }

        // 待機燈 GL (Y17) 點亮
        _plcService.ForceY("Y17", true);
        _plcService.ForceY("Y15", false);

        OnLogAction?.Invoke("[復歸完成] 機構已安全回到機械原點，待機燈 GL 點亮");
    }

    /// <summary>
    /// 準備並檢查機械原點
    /// </summary>
    private async Task CheckAndPrepareOriginAsync(CancellationToken ct)
    {
        // 若推料缸未在後位，先縮回
        if (!GetInputState("X1"))
        {
            _plcService.ForceY("Y0", false);
            await WaitForInputAsync("X1", true, "推料缸後位 a0", 3000, ct);
        }

        // 若升降模組未在上位，先上升 (防衝保護)
        if (!GetInputState("X6"))
        {
            OnLogAction?.Invoke("[待機原點] 升降模組上升回上位 d0 (X6)...");
            await RunMotorToLimitAsync("X6", "升降模組上位 d0", 4000, ct);
        }

        // 吸盤放料停止
        _plcService.ForceY("Y5", false);
        _plcService.ForceY("Y6", false);

        // 若迴轉缸未在 0° 原點，轉回 0°
        if (!GetInputState("X2"))
        {
            OnLogAction?.Invoke("[待機原點] 迴轉缸轉回 0° (同時啟動 Y3 與 Y4，計時 1 秒)...");
            await RotateToZeroDegreesAsync(ct);
        }

        OnLogAction?.Invoke("[待機狀態] 機構已確認回到機械原點");
        await Task.Delay(100, ct);
    }

    /// <summary>
    /// 處理緊急停止 (EMS NC 斷路)
    /// 電磁閥、馬達皆斷電；吸盤繼續吸持不可掉落！
    /// </summary>
    private async Task HandleEmergencyStopAsync(CancellationToken ct)
    {
        OnLogAction?.Invoke("[緊急停止] EMS 觸發！馬達與閥體全面斷電，真空吸盤保持吸持料件不掉落！");

        _plcService.ForceY("Y0", false);
        _plcService.ForceY("Y3", false);
        _plcService.ForceY("Y4", false);
        _plcService.ForceY("Y7", false);
        _plcService.ForceY("Y15", false);

        IsRunning = false;
        OnExecutionStateChanged?.Invoke(false, "【急停觸發】已斷電保持吸持");
        await Task.CompletedTask;
    }

    private void UpdateStep(int stepNum, string desc)
    {
        CurrentStepNumber = stepNum;
        CurrentStepDescription = desc;
        OnStepProgress?.Invoke(stepNum, TotalSteps, desc);
        OnLogAction?.Invoke($"[步驟 {stepNum}/{TotalSteps}] {desc}");
    }

    private async Task PauseForJogAsync(string jogNotice, CancellationToken ct)
    {
        _isPausedAtHash = true;
        OnLogAction?.Invoke($"[調機暫停] {jogNotice}");

        while (_isPausedAtHash && !ct.IsCancellationRequested)
        {
            if (GetInputState("X16"))
            {
                _isPausedAtHash = false;
                break;
            }
            await Task.Delay(100, ct);
        }
    }

    private async Task WaitForInputAsync(string inputAddress, bool expectedState, string description, int timeoutMs, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            if (_plcService.IsSimulationMode)
            {
                await Task.Delay(250, ct);
                _plcService.SetSimulatedX(inputAddress, expectedState);
                return;
            }

            if (GetInputState(inputAddress) == expectedState)
            {
                return;
            }

            await Task.Delay(50, ct);
        }
    }

    private bool GetInputState(string address)
    {
        if (_plcService.IsSimulationMode)
        {
            return _plcService.GetSimulatedX(address);
        }
        return GetInputStateFromCache(address);
    }

    public void ProvideCurrentInputs(Dictionary<string, bool> inputs)
    {
        _currentInputs = inputs;

        // 監控 X10 脈衝邊緣
        if (inputs.TryGetValue("X10", out bool x10Val))
        {
            if (_isMonitoringX10 && x10Val && !_lastX10Val)
            {
                _x10PulseCount++;
                OnLogAction?.Invoke($"[感測檢知] X10 料件方向感測器觸發脈衝 (累計第 {_x10PulseCount} 次)");
            }
            _lastX10Val = x10Val;
        }

        // 缺料暫停等待人工補料時，若偵測到 ST/RST (X16) 則解除暫停
        if (_isWaitingForMaterialReload && inputs.TryGetValue("X16", out bool stRst) && stRst)
        {
            ResumeFromMaterialReload();
        }
    }

    private Dictionary<string, bool> _currentInputs = new(StringComparer.OrdinalIgnoreCase);

    private bool GetInputStateFromCache(string address)
    {
        if (_currentInputs.TryGetValue(address, out bool val))
            return val;
        return false;
    }

    /// <summary>
    /// 運轉垂直升降馬達 (Y7 ON) 直至到達指定極限端點 (X6 上端點 d0 或 X7 下端點 d1)。
    /// 依使用者規範：遇到端點立即煞停 1 秒鐘；若 1 秒後確認在正確位置則進行後續動作；若未在正確位置則 1 秒後繼續運轉。
    /// </summary>
    public async Task RunMotorToLimitAsync(string limitInput, string limitDesc, int timeoutMs, CancellationToken ct)
    {
        var overallDeadline = DateTime.UtcNow.AddMilliseconds(Math.Max(timeoutMs, 10000));
        int retryCount = 0;

        while (DateTime.UtcNow < overallDeadline && !ct.IsCancellationRequested)
        {
            // 若目前已在目標端點，先確認其穩定性
            if (GetInputState(limitInput))
            {
                _plcService.ForceY("Y7", false);
                OnLogAction?.Invoke($"[端點校驗] 升降馬達已在 {limitDesc} ({limitInput})，煞停 1 秒進行確認...");
                await Task.Delay(1000, ct);
                if (GetInputState(limitInput))
                {
                    OnLogAction?.Invoke($"[定位就緒] 升降馬達確認停於 {limitDesc} 正確位置，進行後續動作");
                    return;
                }
            }

            retryCount++;
            OnLogAction?.Invoke($"[馬達運轉] 啟動升降馬達 R1 (Y7 ON) 往 {limitDesc} 運轉 (嘗試第 {retryCount} 次)...");

            try
            {
                // 啟用極限自停與 25ms 極速輪詢
                _plcService.ArmMotorAutoStop("Y7", limitInput);
                _plcService.ForceY("Y7", true);

                if (_plcService.IsSimulationMode)
                {
                    await Task.Delay(250, ct);
                    if (limitInput == "X6") _plcService.SetSimulatedX("X7", false);
                    if (limitInput == "X7") _plcService.SetSimulatedX("X6", false);
                    _plcService.SetSimulatedX(limitInput, true);
                }
                else
                {
                    var moveDeadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                    while (DateTime.UtcNow < moveDeadline && !ct.IsCancellationRequested)
                    {
                        if (GetInputState(limitInput))
                        {
                            break;
                        }
                        await Task.Delay(10, ct);
                    }
                }
            }
            finally
            {
                // 遇到端點立即煞停馬達
                _plcService.ForceY("Y7", false);
                _plcService.DisarmMotorAutoStop();
            }

            // 遇到端點即停止 1 秒鐘，等待後續動作
            OnLogAction?.Invoke($"[端點煞停] 偵測到 {limitDesc}，升降馬達立即停止 1 秒鐘等待後續校驗...");
            await Task.Delay(1000, ct);

            // 若馬達已在正確位置，則進行後續動作
            if (GetInputState(limitInput))
            {
                OnLogAction?.Invoke($"[定位就緒] 升降馬達確認停於 {limitDesc} 正確位置，進行後續動作");
                return;
            }

            // 若馬達未在正確位置，則 1 秒後繼續運轉
            OnLogAction?.Invoke($"[位置校驗] 升降馬達停止 1 秒後未在 {limitDesc} 正確位置 (跑過頭或偏離)，繼續運轉調整位置...");
        }

        // 超時防護停止
        _plcService.ForceY("Y7", false);
    }

    /// <summary>
    /// 依據當前程式與機台狀況，恢復相應的指示燈狀態 (RL 運轉燈 / YL 復歸燈 / GL 待機燈)
    /// </summary>
    public void RestoreIndicatorLights()
    {
        if (IsRunning)
        {
            // 運轉中：RL 亮，YL 滅，GL 滅
            _plcService.ForceY("Y15", true);
            _plcService.ForceY("Y16", false);
            _plcService.ForceY("Y17", false);
            OnLogAction?.Invoke("[指示燈恢復] 機台處於【運轉狀態】：點亮 RL 運轉燈 (Y15)，熄滅 GL / YL");
        }
        else
        {
            // 待機中：RL 滅，YL 滅，GL 亮
            _plcService.ForceY("Y15", false);
            _plcService.ForceY("Y16", false);
            _plcService.ForceY("Y17", true);
            OnLogAction?.Invoke("[指示燈恢復] 機台處於【待機狀態】：點亮 GL 待機燈 (Y17)，熄滅 RL / YL");
        }
    }
}
