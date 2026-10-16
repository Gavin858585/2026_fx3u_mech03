# 三菱 PLC (FX3U) 序列埠監控與控制系統 - 實作成果記錄 (Walkthrough)

本專案已完成 **技術士技能檢定機電整合丙級術科測試第三站** 完整控制邏輯、通訊控制、防跑過頭定位校驗、料件方向辨識取放、缺料處理機制與 HMI 虛擬控制面板。專案相容於 **Visual Studio 2026 / 2022** 與 **.NET 8.0-windows**。

---

## 一、系統核心升級與最新功能項目

### 1. 垂直升降模組馬達防跑過頭校驗機制 (Anti-Overshoot Protection)
- **硬體通訊延遲對策**：升降馬達 R1 (Y7) 運轉遇到上端點 d0 (X6) 或下端點 d1 (X7) 時，軟體立即斷開 Y7 輸出煞停馬達。
- **1 秒煞停等待與位置校驗**：馬達煞停後停留等待 1 秒鐘，確認接點穩定度：
  - 若 1 秒後確認仍停留在正確端點位置，則判定到位就緒，進行下一動作步序。
  - 若 1 秒後因慣性偏離或未在正確端點位置，則自動再次啟動馬達運轉調整，直到正確到位。

### 2. 料件方向感測器 X10 脈衝計數與 10 大標準取放步序
- **X10 脈衝檢測**：推料缸 A 推出料件至前端治具時，透過 X10 (s1) 感測器檢測料件凹槽特徵：
  - **感測到 2 次脈衝**：判定凹槽朝向，迴轉缸轉至 **－90°** 取料方向 (啟動 Y4: B2)。
  - **僅感測到 1 次脈衝**：判定凹槽朝向，迴轉缸轉至 **＋90°** 取料方向 (啟動 Y3: B1)。
- **10 大標準動作流程**：
  1. 推料氣壓缸 A 伸出 (Y0 ON)，料件推至前端治具並即時感測 X10。
  2. 迴轉缸轉至取料方向 (2次脈衝轉 -90° [Y4]，1次脈衝轉 +90° [Y3])。
  3. 升降模組馬達旋轉 R1 (Y7 ON) 下降至下端點 d1 (遇到端點煞停 1 秒校驗)。【調機模式第 1 暫停點】
  4. 真空吸盤吸 C+ (Y5 ON)，真空壓力開關 ps1 建立負壓 (X5 ON)。
  5. 推料氣壓缸 A 縮回後端點 a0 (Y0 OFF)。
  6. 升降模組馬達旋轉 R1 (Y7 ON) 上升回上端點 d0 (吸持料件，煞停 1 秒校驗)。
  7. 迴轉缸轉至 0° 排料位 (同時啟動 Y3 與 Y4，訊號持續輸出，等待 1 秒確保定位)。
  8. 升降模組馬達旋轉 R1 (Y7 ON) 下降至排料位 d1 (此時 Y3/Y4 維持氣壓支撐)。【調機模式第 2 暫停點】
  9. 真空吸盤放料 C- (Y6 ON)，料件由出料斜坡滑下排料，放料完成後停止輸出 Y3、Y4。
  10. 升降模組馬達旋轉 R1 (Y7 ON) 上升回上端點 d0，完成單一料件流程，回到原點。

### 3. 智慧缺料檢知與分模處理機制
- 若推料缸推出料件後，X10 未感測到料件 (0 次)：
  - **單一循環 / 指定功能 (調機模式)**：推料缸縮回後位 a0，流程結束回到待機狀態 (待機燈 GL 點亮，RL 熄滅)。
  - **連續循環模式**：推料缸縮回後位 a0 並暫停運轉，**保留已做料件計數 (不歸零)**。系統提示「缺料暫停，請手動補料並按下 ST/RST 鈕繼續」，待補料並按下 ST 鈕後無縫接續作動，直至完成 4 個料件。

### 4. 迴轉缸 0° 氣壓支撐與時序維持
- 因機台無 0° 輔助機械定位機構，轉至 0° 時同時啟動 Y3 與 Y4，並計時等待 1 秒鐘以確保精準轉至 0°。
- **維持氣壓支撐**：Y3、Y4 電磁閥訊號持續輸出保持通電，跨越升降模組下降過程，直到吸盤破真空放料 (C-) 完成後才關閉 Y3 與 Y4，徹底避免吸盤下降過程中因重力或振動發生偏移。

### 5. 跑馬燈測試與機台狀態燈號自動恢復
- 新增「跑馬燈測試」按鈕，執行三色指示燈依序循環（紅燈 1s -> 黃燈 1s -> 綠燈 1s）共 3 次。
- 測試結束或手動中斷時，三燈先全滅，隨後依當前機台狀態（運轉中或待機中）自動恢復相應指示燈（待機亮 GL，運轉亮 RL）。

### 6. UI 排版最佳化
- 輸入/輸出接點監控採左右並列排版，並配置 ScrollBar 便於完整瀏覽。
- 通訊日誌移至下方並預設精簡顯示步序日誌，避免封包刷頻。
- 調整序列埠文字方塊、連線狀態、控制面板標籤與按鈕間距，解決文字重疊遮擋問題。

---

## 二、單元測試驗證結果 (17 項全數通過)

執行 `dotnet test` 結果：
```
已通過! - 失敗: 0，通過: 17，略過: 0，總計: 17，持續時間: 9 s - Fx3uMonitor.Tests.dll (net8.0)
```

| 測試案例 | 驗證項目 | 結果 |
| :--- | :--- | :---: |
| `TestReadCommands_MatchExactBytes` | 讀取 Y0~Y17 與 X0~X17 之 11-byte 命令逐位元組一致性 | **通過** |
| `TestUserExample_Y0ToY17_ResponseParsing` | `02 35 35 38 34 03 43 33` 解碼正確性 (Y4/Y6/Y0/Y2/Y17/Y12=ON) | **通過** |
| `TestUserExample_X0ToX17_ResponseParsing` | X 接點解碼邏輯逐點驗證 | **通過** |
| `TestForceY_All16Points_MatchExactUserProvidedHex` | 32 組強制命令逐 Byte 驗證完全吻合 | **通過** |
| `TestSimulation_EncodeAndDecodeRoundtrip` | 模擬器狀態編解碼往返一致性 | **通過** |
| `TestIoConfig_MatchesUserSpecification` | 完整驗證 X0~X17、Y0~Y17 與控制面板對照規格 | **通過** |
| `TestMechatronicsLevel3_FullStation3IoMapping` | 機電丙三專案全接點標籤與型態對應 | **通過** |
| `TestPlcService_FilterPollingLogDefaultIsTrue` | 預設通訊輪詢封包精簡過濾防刷頻 | **通過** |
| `TestMechatronicsLevel3Service_InitializationState` | 服務初始狀態 (TotalSteps=10, PieceCount=0) | **通過** |
| `TestMechatronicsLevel3_RotateToZeroDegrees_ExecutesSuccessfully` | 迴轉缸轉 0° 雙閥驅動與復位 | **通過** |
| `TestMechatronicsLevel3_MarqueeTest_StartAndStop` | 跑馬燈測試啟動、停止與燈號狀態恢復 | **通過** |
| `TestMechatronicsLevel3_MotorRunToLimit_AutoStop` | 升降馬達極限煞停 1 秒校驗保護 | **通過** |
| `TestMechatronicsLevel3_RotateToZeroDegrees_HoldSignalUntilRelease` | 轉 0° 訊號保持輸出直到料件排出才關閉 | **通過** |
| `TestMechatronicsLevel3_X10_SingleCycle_NoMaterial_ReturnsToStandby` | 單一循環缺料檢知後退料缸回待機 (GL亮) | **通過** |
| `TestMechatronicsLevel3_X10_Continuous_NoMaterial_PausesAndResumes` | 連續循環缺料暫停保留計數，ST復歸續行 | **通過** |
| `TestMechatronicsLevel3_X10_SingleCycle_2Pulses_FullSequenceCompletes` | X10 檢知 2 次脈衝完整 10 步驟取放驗證 | **通過** |

---

## 三、如何啟動與驗證

1. **在 Visual Studio 2026 / 2022 中開啟**：
   雙擊開啟根目錄下的 [Fx3uMonitor.sln](file:///c:/Users/黃建綱/Desktop/機電整合/練習檔/Antigravity/FX3U/Fx3uMonitor.sln)，按下 `F5` 啟動。
2. **直接執行 Release 執行檔**：
   執行檔位於：[Fx3uMonitor.exe](file:///c:/Users/黃建綱/Desktop/機電整合/練習檔/Antigravity/FX3U/Fx3uMonitor/bin/Release/net8.0-windows/Fx3uMonitor.exe)。
3. **模擬模式測試驗證步驟**：
   - 勾選上方「**模擬模式 (免實體 PLC)**」，點擊「**開始連線 (Connect)**」。
   - 點擊「**機電丙三啟動**」：
     - 觀察推料缸推出並依 X10 感測次數自動選擇旋轉方向。
     - 觀察升降馬達遇到端點時立即停止 1 秒進行校驗。
     - 觀察迴轉缸轉至 0° 時持續維持 Y3/Y4 氣壓輸出，直至料件排出後才解除。
     - 觀察單一循環、指定功能或連續循環完整 10 步序執行。
   - 點擊「**跑馬燈測試**」：
     - 觀察紅、黃、綠三燈循環閃爍 3 次，停止後依當前機台狀態自動恢復相應燈號。
