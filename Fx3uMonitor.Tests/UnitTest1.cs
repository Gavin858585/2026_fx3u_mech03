using System.Collections.Generic;
using Fx3uMonitor.Models;
using Fx3uMonitor.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fx3uMonitor.Tests;

[TestClass]
public class ProtocolTests
{
    [TestMethod]
    public void TestReadCommands_MatchExactBytes()
    {
        // 讀取 Y0~Y17
        // 02 30 30 30 41 30 30 32 03 36 36
        byte[] expectedReadY = new byte[] { 0x02, 0x30, 0x30, 0x30, 0x41, 0x30, 0x30, 0x32, 0x03, 0x36, 0x36 };
        CollectionAssert.AreEqual(expectedReadY, Fx3uProtocol.ReadYCommand);

        // 讀取 X0~X17
        // 02 30 30 30 38 30 30 32 03 35 44
        byte[] expectedReadX = new byte[] { 0x02, 0x30, 0x30, 0x30, 0x38, 0x30, 0x30, 0x32, 0x03, 0x35, 0x44 };
        CollectionAssert.AreEqual(expectedReadX, Fx3uProtocol.ReadXCommand);
    }

    [TestMethod]
    public void TestUserExample_Y0ToY17_ResponseParsing()
    {
        // 使用者規格範例:
        // PLC 回傳: 02 35 35 38 34 03 43 33
        // 第一個數值 35 (ASCII '5' -> 0b0101) => Y4:ON, Y6:ON
        // 第二個數值 35 (ASCII '5' -> 0b0101) => Y0:ON, Y2:ON
        // 第三個數值 38 (ASCII '8' -> 0b1000) => Y17:ON
        // 第四個數值 34 (ASCII '4' -> 0b0100) => Y12:ON
        byte[] resp = new byte[] { 0x02, 0x35, 0x35, 0x38, 0x34, 0x03, 0x43, 0x33 };

        var yPoints = Fx3uProtocol.ParseBitPointsResponse(resp, 'Y');

        // 驗證低位 Y0 ~ Y3 (來自第二個數值 35 -> '5' -> 0b0101)
        Assert.IsTrue(yPoints["Y0"], "Y0 應為 ON");
        Assert.IsFalse(yPoints["Y1"], "Y1 應為 OFF");
        Assert.IsTrue(yPoints["Y2"], "Y2 應為 ON");
        Assert.IsFalse(yPoints["Y3"], "Y3 應為 OFF");

        // 驗證中低位 Y4 ~ Y7 (來自第一個數值 35 -> '5' -> 0b0101)
        Assert.IsTrue(yPoints["Y4"], "Y4 應為 ON");
        Assert.IsFalse(yPoints["Y5"], "Y5 應為 OFF");
        Assert.IsTrue(yPoints["Y6"], "Y6 應為 ON");
        Assert.IsFalse(yPoints["Y7"], "Y7 應為 OFF");

        // 驗證中高位 Y10 ~ Y13 (來自第四個數值 34 -> '4' -> 0b0100)
        Assert.IsFalse(yPoints["Y10"], "Y10 應為 OFF");
        Assert.IsFalse(yPoints["Y11"], "Y11 應為 OFF");
        Assert.IsTrue(yPoints["Y12"], "Y12 應為 ON");
        Assert.IsFalse(yPoints["Y13"], "Y13 應為 OFF");

        // 驗證高位 Y14 ~ Y17 (來自第三個數值 38 -> '8' -> 0b1000)
        Assert.IsFalse(yPoints["Y14"], "Y14 應為 OFF");
        Assert.IsFalse(yPoints["Y15"], "Y15 應為 OFF");
        Assert.IsFalse(yPoints["Y16"], "Y16 應為 OFF");
        Assert.IsTrue(yPoints["Y17"], "Y17 應為 ON");
    }

    [TestMethod]
    public void TestUserExample_X0ToX17_ResponseParsing()
    {
        // 讀取 X0~X17 相同解碼結構驗證
        byte[] resp = new byte[] { 0x02, 0x35, 0x35, 0x38, 0x34, 0x03, 0x43, 0x33 };
        var xPoints = Fx3uProtocol.ParseBitPointsResponse(resp, 'X');

        Assert.IsTrue(xPoints["X0"]);
        Assert.IsFalse(xPoints["X1"]);
        Assert.IsTrue(xPoints["X2"]);
        Assert.IsFalse(xPoints["X3"]);

        Assert.IsTrue(xPoints["X4"]);
        Assert.IsFalse(xPoints["X5"]);
        Assert.IsTrue(xPoints["X6"]);
        Assert.IsFalse(xPoints["X7"]);

        Assert.IsFalse(xPoints["X10"]);
        Assert.IsFalse(xPoints["X11"]);
        Assert.IsTrue(xPoints["X12"]);
        Assert.IsFalse(xPoints["X13"]);

        Assert.IsFalse(xPoints["X14"]);
        Assert.IsFalse(xPoints["X15"]);
        Assert.IsFalse(xPoints["X16"]);
        Assert.IsTrue(xPoints["X17"]);
    }

    [TestMethod]
    public void TestForceY_All16Points_MatchExactUserProvidedHex()
    {
        var expectedCommands = new Dictionary<string, (byte[] On, byte[] Off)>
        {
            ["Y0"]  = (new byte[] { 0x02, 0x37, 0x30, 0x30, 0x30, 0x35, 0x03, 0x46, 0x46 }, new byte[] { 0x02, 0x38, 0x30, 0x30, 0x30, 0x35, 0x03, 0x30, 0x30 }),
            ["Y1"]  = (new byte[] { 0x02, 0x37, 0x30, 0x31, 0x30, 0x35, 0x03, 0x30, 0x30 }, new byte[] { 0x02, 0x38, 0x30, 0x31, 0x30, 0x35, 0x03, 0x30, 0x31 }),
            ["Y2"]  = (new byte[] { 0x02, 0x37, 0x30, 0x32, 0x30, 0x35, 0x03, 0x30, 0x31 }, new byte[] { 0x02, 0x38, 0x30, 0x32, 0x30, 0x35, 0x03, 0x30, 0x32 }),
            ["Y3"]  = (new byte[] { 0x02, 0x37, 0x30, 0x33, 0x30, 0x35, 0x03, 0x30, 0x32 }, new byte[] { 0x02, 0x38, 0x30, 0x33, 0x30, 0x35, 0x03, 0x30, 0x33 }),
            ["Y4"]  = (new byte[] { 0x02, 0x37, 0x30, 0x34, 0x30, 0x35, 0x03, 0x30, 0x33 }, new byte[] { 0x02, 0x38, 0x30, 0x34, 0x30, 0x35, 0x03, 0x30, 0x34 }),
            ["Y5"]  = (new byte[] { 0x02, 0x37, 0x30, 0x35, 0x30, 0x35, 0x03, 0x30, 0x34 }, new byte[] { 0x02, 0x38, 0x30, 0x35, 0x30, 0x35, 0x03, 0x30, 0x35 }),
            ["Y6"]  = (new byte[] { 0x02, 0x37, 0x30, 0x36, 0x30, 0x35, 0x03, 0x30, 0x35 }, new byte[] { 0x02, 0x38, 0x30, 0x36, 0x30, 0x35, 0x03, 0x30, 0x36 }),
            ["Y7"]  = (new byte[] { 0x02, 0x37, 0x30, 0x37, 0x30, 0x35, 0x03, 0x30, 0x36 }, new byte[] { 0x02, 0x38, 0x30, 0x37, 0x30, 0x35, 0x03, 0x30, 0x37 }),
            ["Y10"] = (new byte[] { 0x02, 0x37, 0x30, 0x38, 0x30, 0x35, 0x03, 0x30, 0x37 }, new byte[] { 0x02, 0x38, 0x30, 0x38, 0x30, 0x35, 0x03, 0x30, 0x38 }),
            ["Y11"] = (new byte[] { 0x02, 0x37, 0x30, 0x39, 0x30, 0x35, 0x03, 0x30, 0x38 }, new byte[] { 0x02, 0x38, 0x30, 0x39, 0x30, 0x35, 0x03, 0x30, 0x39 }),
            ["Y12"] = (new byte[] { 0x02, 0x37, 0x30, 0x41, 0x30, 0x35, 0x03, 0x31, 0x30 }, new byte[] { 0x02, 0x38, 0x30, 0x41, 0x30, 0x35, 0x03, 0x31, 0x31 }),
            ["Y13"] = (new byte[] { 0x02, 0x37, 0x30, 0x42, 0x30, 0x35, 0x03, 0x31, 0x31 }, new byte[] { 0x02, 0x38, 0x30, 0x42, 0x30, 0x35, 0x03, 0x31, 0x32 }),
            ["Y14"] = (new byte[] { 0x02, 0x37, 0x30, 0x43, 0x30, 0x35, 0x03, 0x31, 0x32 }, new byte[] { 0x02, 0x38, 0x30, 0x43, 0x30, 0x35, 0x03, 0x31, 0x33 }),
            ["Y15"] = (new byte[] { 0x02, 0x37, 0x30, 0x44, 0x30, 0x35, 0x03, 0x31, 0x33 }, new byte[] { 0x02, 0x38, 0x30, 0x44, 0x30, 0x35, 0x03, 0x31, 0x34 }),
            ["Y16"] = (new byte[] { 0x02, 0x37, 0x30, 0x45, 0x30, 0x35, 0x03, 0x31, 0x34 }, new byte[] { 0x02, 0x38, 0x30, 0x45, 0x30, 0x35, 0x03, 0x31, 0x35 }),
            ["Y17"] = (new byte[] { 0x02, 0x37, 0x30, 0x46, 0x30, 0x35, 0x03, 0x31, 0x35 }, new byte[] { 0x02, 0x38, 0x30, 0x46, 0x30, 0x35, 0x03, 0x31, 0x36 }),
        };

        foreach (var (pt, pair) in expectedCommands)
        {
            byte[] actualOn = Fx3uProtocol.GetForceYCommand(pt, true);
            CollectionAssert.AreEqual(pair.On, actualOn, $"{pt} ON 命令不一致");

            byte[] actualOff = Fx3uProtocol.GetForceYCommand(pt, false);
            CollectionAssert.AreEqual(pair.Off, actualOff, $"{pt} OFF 命令不一致");
        }
    }

    [TestMethod]
    public void TestSimulation_EncodeAndDecodeRoundtrip()
    {
        var original = new Dictionary<string, bool>();
        foreach (var p in Fx3uProtocol.PointNames)
        {
            original[$"Y{p}"] = (int.Parse(p) % 2 == 0); // 偶數為 ON
        }

        byte[] encoded = Fx3uProtocol.EncodeBitPointsResponse(original, 'Y');
        var decoded = Fx3uProtocol.ParseBitPointsResponse(encoded, 'Y');

        foreach (var p in Fx3uProtocol.PointNames)
        {
            Assert.AreEqual(original[$"Y{p}"], decoded[$"Y{p}"], $"點位 Y{p} 編解碼往返不相符");
        }
    }

    [TestMethod]
    public void TestIoConfig_MatchesUserSpecification()
    {
        // 驗證 X 輸入配置
        Assert.AreEqual("a1", IoConfig.Inputs["X0"].Code);
        Assert.AreEqual("推料缸前端點", IoConfig.Inputs["X0"].Description);

        Assert.AreEqual("a0", IoConfig.Inputs["X1"].Code);
        Assert.AreEqual("推料缸後端點", IoConfig.Inputs["X1"].Description);

        Assert.AreEqual("b0", IoConfig.Inputs["X2"].Code);
        Assert.AreEqual("迴轉缸 0°端點", IoConfig.Inputs["X2"].Description);

        Assert.AreEqual("b1", IoConfig.Inputs["X3"].Code);
        Assert.AreEqual("迴轉缸＋90° 端點", IoConfig.Inputs["X3"].Description);

        Assert.AreEqual("b2", IoConfig.Inputs["X4"].Code);
        Assert.AreEqual("迴轉缸－90°端點", IoConfig.Inputs["X4"].Description);

        Assert.AreEqual("ps1", IoConfig.Inputs["X5"].Code);
        Assert.AreEqual("真空壓力開關", IoConfig.Inputs["X5"].Description);

        Assert.AreEqual("d0", IoConfig.Inputs["X6"].Code);
        Assert.AreEqual("升降模組上端點", IoConfig.Inputs["X6"].Description);

        Assert.AreEqual("d1", IoConfig.Inputs["X7"].Code);
        Assert.AreEqual("升降模組下端點", IoConfig.Inputs["X7"].Description);

        Assert.AreEqual("s1", IoConfig.Inputs["X10"].Code);
        Assert.AreEqual("料件方向感測器", IoConfig.Inputs["X10"].Description);

        Assert.AreEqual("COS1", IoConfig.Inputs["X13"].Code);
        Assert.AreEqual("*復歸模式/工作模式", IoConfig.Inputs["X13"].Description);

        Assert.AreEqual("COS2_L", IoConfig.Inputs["X14"].Code);
        Assert.AreEqual("*單一循環/指定功能", IoConfig.Inputs["X14"].Description);

        Assert.AreEqual("COS2_R", IoConfig.Inputs["X15"].Code);
        Assert.AreEqual("*單一循環/連續循環", IoConfig.Inputs["X15"].Description);

        Assert.AreEqual("ST/RST", IoConfig.Inputs["X16"].Code);
        Assert.AreEqual("啟動鈕/復歸鈕", IoConfig.Inputs["X16"].Description);

        Assert.AreEqual("EMS", IoConfig.Inputs["X17"].Code);
        Assert.AreEqual("緊急停止開關(NC)", IoConfig.Inputs["X17"].Description);

        // 驗證 Y 輸出配置
        Assert.AreEqual("A", IoConfig.Outputs["Y0"].Code);
        Assert.AreEqual("推料缸伸出", IoConfig.Outputs["Y0"].Description);

        Assert.AreEqual("B1", IoConfig.Outputs["Y3"].Code);
        Assert.AreEqual("迴轉缸轉至 0°/＋90°", IoConfig.Outputs["Y3"].Description);

        Assert.AreEqual("B2", IoConfig.Outputs["Y4"].Code);
        Assert.AreEqual("迴轉缸轉至 0°/－90°", IoConfig.Outputs["Y4"].Description);

        Assert.AreEqual("C＋", IoConfig.Outputs["Y5"].Code);
        Assert.AreEqual("真空吸盤吸", IoConfig.Outputs["Y5"].Description);

        Assert.AreEqual("C－", IoConfig.Outputs["Y6"].Code);
        Assert.AreEqual("真空吸盤放", IoConfig.Outputs["Y6"].Description);

        Assert.AreEqual("R1", IoConfig.Outputs["Y7"].Code);
        Assert.AreEqual("升降模組馬達旋轉(M＋)", IoConfig.Outputs["Y7"].Description);

        Assert.AreEqual("RL", IoConfig.Outputs["Y15"].Code);
        StringAssert.Contains(IoConfig.Outputs["Y15"].Description, "紅燈");
        StringAssert.Contains(IoConfig.Outputs["Y16"].Description, "黃燈");
        StringAssert.Contains(IoConfig.Outputs["Y17"].Description, "綠燈");

        // 驗證控制面板元件
        Assert.AreEqual(7, IoConfig.ControlPanelItems.Count);
    }

    [TestMethod]
    public void TestMechatronicsLevel3_FullStation3IoMapping()
    {
        // 驗證丙級第三站全部關鍵 I/O 對應關係
        Assert.AreEqual("a1", IoConfig.Inputs["X0"].Code);
        Assert.AreEqual("a0", IoConfig.Inputs["X1"].Code);
        Assert.AreEqual("b0", IoConfig.Inputs["X2"].Code);
        Assert.AreEqual("b1", IoConfig.Inputs["X3"].Code);
        Assert.AreEqual("b2", IoConfig.Inputs["X4"].Code);
        Assert.AreEqual("ps1", IoConfig.Inputs["X5"].Code);
        Assert.AreEqual("d0", IoConfig.Inputs["X6"].Code);
        Assert.AreEqual("d1", IoConfig.Inputs["X7"].Code);
        Assert.AreEqual("s1", IoConfig.Inputs["X10"].Code);
        Assert.AreEqual("COS1", IoConfig.Inputs["X13"].Code);
        Assert.AreEqual("COS2_L", IoConfig.Inputs["X14"].Code);
        Assert.AreEqual("COS2_R", IoConfig.Inputs["X15"].Code);
        Assert.AreEqual("ST/RST", IoConfig.Inputs["X16"].Code);
        Assert.AreEqual("EMS", IoConfig.Inputs["X17"].Code);

        Assert.AreEqual("A", IoConfig.Outputs["Y0"].Code);
        Assert.AreEqual("B1", IoConfig.Outputs["Y3"].Code);
        Assert.AreEqual("B2", IoConfig.Outputs["Y4"].Code);
        Assert.AreEqual("C＋", IoConfig.Outputs["Y5"].Code);
        Assert.AreEqual("C－", IoConfig.Outputs["Y6"].Code);
        Assert.AreEqual("R1", IoConfig.Outputs["Y7"].Code);
        Assert.AreEqual("RL", IoConfig.Outputs["Y15"].Code);
        Assert.AreEqual("YL", IoConfig.Outputs["Y16"].Code);
        Assert.AreEqual("GL", IoConfig.Outputs["Y17"].Code);
    }

    [TestMethod]
    public void TestPlcService_FilterPollingLogDefaultIsTrue()
    {
        var plc = new PlcService();
        Assert.IsTrue(plc.FilterPollingLog, "預設必須啟用輪詢日誌精簡過濾，避免通訊封包刷頻");
        
        plc.FilterPollingLog = false;
        Assert.IsFalse(plc.FilterPollingLog);
    }

    [TestMethod]
    public void TestMechatronicsLevel3Service_InitializationState()
    {
        var plc = new PlcService();
        var service = new MechatronicsLevel3Service(plc);

        Assert.IsFalse(service.IsRunning);
        Assert.IsFalse(service.IsMarqueeRunning);
        Assert.AreEqual(0, service.PieceCount);
        Assert.AreEqual(4, service.TargetPieces);
        Assert.AreEqual(10, service.TotalSteps);
        Assert.AreEqual(0, service.CurrentStepNumber);
        StringAssert.Contains(service.CurrentStepDescription, "待機");
    }

    [TestMethod]
    public async System.Threading.Tasks.Task TestMechatronicsLevel3_RotateToZeroDegrees_ExecutesSuccessfully()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        var service = new MechatronicsLevel3Service(plc);
        await service.RotateToZeroDegreesAsync(System.Threading.CancellationToken.None);

        // 旋轉定位完成後，X2 (b0) 應為 true，且 Y3, Y4 電磁閥已復位為 false
        Assert.IsTrue(plc.GetSimulatedX("X2"), "迴轉缸 0° 端點 b0 (X2) 應到位");
        Assert.IsFalse(plc.GetSimulatedY("Y3"), "Y3 電磁閥在定位完成後應復歸");
        Assert.IsFalse(plc.GetSimulatedY("Y4"), "Y4 電磁閥在定位完成後應復歸");
    }

    [TestMethod]
    public void TestMechatronicsLevel3_MarqueeTest_StartAndStop()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        var service = new MechatronicsLevel3Service(plc);
        Assert.IsFalse(service.IsMarqueeRunning);

        service.StartMarqueeTest();
        Assert.IsTrue(service.IsMarqueeRunning);

        service.StopMarqueeTest();
        Assert.IsFalse(service.IsMarqueeRunning);

        // 停止後，依待機狀況恢復燈號：待機中 GL (Y17) 點亮，RL 與 YL 熄滅
        Assert.IsFalse(plc.GetSimulatedY("Y15"), "RL (Y15) 應熄滅");
        Assert.IsFalse(plc.GetSimulatedY("Y16"), "YL (Y16) 應熄滅");
        Assert.IsTrue(plc.GetSimulatedY("Y17"), "GL (Y17) 待機燈應點亮恢復");
    }

    [TestMethod]
    public async Task TestMechatronicsLevel3_MotorRunToLimit_AutoStop()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        var service = new MechatronicsLevel3Service(plc);

        // 初始升降模組在上位 d0 (X6 = true, X7 = false)
        plc.SetSimulatedX("X6", true);
        plc.SetSimulatedX("X7", false);

        // 測試運轉下降至 d1 (X7)
        var cts = new CancellationTokenSource(8000);
        await service.RunMotorToLimitAsync("X7", "升降模組下端點 d1", 4000, cts.Token);

        // 馬達 Y7 在到位後應立即切斷為 false，且停留 1 秒確認穩定
        Assert.IsFalse(plc.GetSimulatedY("Y7"), "馬達 Y7 在抵達端點後應自動切斷電源");
        // 端點 X7 應已到位
        Assert.IsTrue(plc.GetSimulatedX("X7"), "升降模組下端點 d1 (X7) 應到位");
        // 原端點 X6 應離開
        Assert.IsFalse(plc.GetSimulatedX("X6"), "離開上位端點 d0 (X6) 應為 false");
    }

    [TestMethod]
    public async Task TestMechatronicsLevel3_RotateToZeroDegrees_HoldSignalUntilRelease()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        var service = new MechatronicsLevel3Service(plc);

        // 旋轉至 0°，要求維持 Y3、Y4 持續輸出 (避免無氣壓支撐偏移)
        var cts = new CancellationTokenSource(3000);
        await service.RotateToZeroDegreesAsync(keepEnergized: true, cts.Token);

        // 到位後 X2 應為 true，且 Y3、Y4 應持續為 true
        Assert.IsTrue(plc.GetSimulatedX("X2"), "迴轉缸 0° 端點 b0 (X2) 應到位");
        Assert.IsTrue(plc.GetSimulatedY("Y3"), "Y3 在 keepEnergized 模式下應持續輸出");
        Assert.IsTrue(plc.GetSimulatedY("Y4"), "Y4 在 keepEnergized 模式下應持續輸出");

        // 料件釋放後手動關閉
        plc.ForceY("Y3", false);
        plc.ForceY("Y4", false);
        Assert.IsFalse(plc.GetSimulatedY("Y3"), "料件排出後 Y3 應關閉");
        Assert.IsFalse(plc.GetSimulatedY("Y4"), "料件排出後 Y4 應關閉");
    }

    [TestMethod]
    public async Task TestMechatronicsLevel3_X10_SingleCycle_NoMaterial_ReturnsToStandby()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        // 初始機械原點狀態
        plc.SetSimulatedX("X0", false);
        plc.SetSimulatedX("X1", true);  // 推料缸後端點 a0
        plc.SetSimulatedX("X2", true);  // 迴轉缸 0° b0
        plc.SetSimulatedX("X6", true);  // 升降模組上端點 d0
        plc.SetSimulatedX("X7", false);
        plc.SetSimulatedX("X10", false);
        plc.SetSimulatedX("X13", true); // 工作模式
        plc.SetSimulatedX("X14", false);
        plc.SetSimulatedX("X15", false);// 單一循環
        plc.SetSimulatedX("X16", false);
        plc.SetSimulatedX("X17", true); // EMS 正常

        var service = new MechatronicsLevel3Service(plc)
        {
            SimulatedX10Pulses = 0 // 模擬料匣無料件 (X10 脈衝為 0)
        };

        service.Start();

        // 等待推料缸推出並檢知缺料退回
        var deadline = DateTime.UtcNow.AddSeconds(6);
        while (service.IsRunning && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        // 單一模式下缺料應退料缸回到待機狀態 (GL 亮，RL 滅，PieceCount = 0)
        Assert.IsFalse(service.IsRunning, "缺料後單一循環應結束運轉回到待機");
        Assert.AreEqual(0, service.PieceCount, "缺料時不計入已做料件數");
        Assert.IsTrue(plc.GetSimulatedY("Y17"), "待機燈 GL (Y17) 應點亮");
        Assert.IsFalse(plc.GetSimulatedY("Y15"), "運轉燈 RL (Y15) 應熄滅");
        Assert.IsFalse(plc.GetSimulatedY("Y0"), "推料缸應縮回後端點 (Y0 OFF)");
    }

    [TestMethod]
    public async Task TestMechatronicsLevel3_X10_Continuous_NoMaterial_PausesAndResumes()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        // 初始機械原點狀態
        plc.SetSimulatedX("X0", false);
        plc.SetSimulatedX("X1", true);  // a0
        plc.SetSimulatedX("X2", true);  // b0
        plc.SetSimulatedX("X6", true);  // d0
        plc.SetSimulatedX("X7", false);
        plc.SetSimulatedX("X10", false);
        plc.SetSimulatedX("X13", true); // 工作模式
        plc.SetSimulatedX("X14", false);
        plc.SetSimulatedX("X15", true); // 連續循環
        plc.SetSimulatedX("X16", false);
        plc.SetSimulatedX("X17", true); // EMS 正常

        var service = new MechatronicsLevel3Service(plc)
        {
            TargetPieces = 4,
            SimulatedX10Pulses = 0 // 先模擬第 1 件缺料
        };

        service.Start();

        // 等待推料缸推出並觸發缺料暫停
        var deadline = DateTime.UtcNow.AddSeconds(6);
        while (!service.IsWaitingForMaterialReload && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.IsTrue(service.IsWaitingForMaterialReload, "連續模式缺料時應進入缺料暫停等待狀態");
        Assert.AreEqual(0, service.PieceCount, "已做料件數應保留為 0");
        Assert.IsFalse(plc.GetSimulatedY("Y0"), "推料缸應後退縮回 a0 (Y0 OFF)");

        // 模擬人工放入料件並按下 ST 鈕恢復
        service.SimulatedX10Pulses = 2; // 人工補料後有料件 (X10 感測 2 次)
        service.ResumeFromMaterialReload();

        // 驗證已解除暫停
        Assert.IsFalse(service.IsWaitingForMaterialReload, "按下 ST 鈕後應解除缺料暫停繼續運轉");

        // 停止服務以利清理
        service.Stop();
    }

    [TestMethod]
    public async Task TestMechatronicsLevel3_X10_SingleCycle_2Pulses_FullSequenceCompletes()
    {
        var plc = new PlcService();
        plc.Connect("COM1", simulation: true);

        // 初始機械原點狀態
        plc.SetSimulatedX("X0", false);
        plc.SetSimulatedX("X1", true);  // a0 (推料缸後端點)
        plc.SetSimulatedX("X2", true);  // b0 (迴轉缸 0°)
        plc.SetSimulatedX("X6", true);  // d0 (升降模組上位)
        plc.SetSimulatedX("X7", false);
        plc.SetSimulatedX("X13", true); // 工作模式
        plc.SetSimulatedX("X14", false);
        plc.SetSimulatedX("X15", false);// 單一循環
        plc.SetSimulatedX("X16", false);
        plc.SetSimulatedX("X17", true); // EMS 正常

        var service = new MechatronicsLevel3Service(plc)
        {
            SimulatedX10Pulses = 2 // 2 次脈衝 -> 轉 -90°
        };

        service.Start();

        // 等待單一循環完整 10 步驟執行完畢
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (service.IsRunning && DateTime.UtcNow < deadline)
        {
            await Task.Delay(200);
        }

        // 單一循環應順利完成 1 件並回到待機狀態 (GL 亮，RL 滅，Y3/Y4 滅，推料缸在 a0，升降在上位 d0)
        Assert.IsFalse(service.IsRunning, "單一循環應執行完畢回到待機");
        Assert.AreEqual(1, service.PieceCount, "應成功完成 1 個料件");
        Assert.IsTrue(plc.GetSimulatedY("Y17"), "待機燈 GL (Y17) 應點亮");
        Assert.IsFalse(plc.GetSimulatedY("Y15"), "運轉燈 RL (Y15) 應熄滅");
        Assert.IsFalse(plc.GetSimulatedY("Y3"), "料件排出後 Y3 應關閉");
        Assert.IsFalse(plc.GetSimulatedY("Y4"), "料件排出後 Y4 應關閉");
        Assert.IsFalse(plc.GetSimulatedY("Y0"), "推料缸應在後端點 a0 (Y0 OFF)");
        Assert.IsTrue(plc.GetSimulatedX("X6"), "機構應回到上位 d0 (X6)");
    }
}


