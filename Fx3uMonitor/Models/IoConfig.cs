using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fx3uMonitor.Models;

/// <summary>
/// PLC I/O 接點配置資訊模型
/// </summary>
public class IoPointInfo
{
    public string Address { get; set; } = string.Empty;       // 例如 "X0", "Y0"
    public string Code { get; set; } = string.Empty;          // 例如 "a1", "A"
    public string Description { get; set; } = string.Empty;   // 例如 "推料缸前端點"
    public string DeviceType { get; set; } = string.Empty;    // 例如 "復歸式按鈕開關"
    public bool IsControlPanel { get; set; } = false;         // 是否屬於控制面板元件
    public Color? CustomOnColor { get; set; } = null;         // 自訂點亮時 LED 顏色

    public bool IsAssigned => !string.IsNullOrEmpty(Code) && Code != "-";

    public string DisplayTitle => IsAssigned ? $"{Address} [{Code}]" : $"{Address}";
}

/// <summary>
/// 控制面板元件說明資訊
/// </summary>
public class ControlPanelItem
{
    public string Name { get; set; } = string.Empty;          // 例如 "ST/RST"
    public string Description { get; set; } = string.Empty;   // 說明
    public string DeviceType { get; set; } = string.Empty;    // 類型
    public string IoAddress { get; set; } = string.Empty;     // 對應 I/O (例如 "X16")
}

/// <summary>
/// 系統 I/O 配置與控制面板全域定義庫
/// </summary>
public static class IoConfig
{
    /// <summary>
    /// 控制面板元件清單
    /// </summary>
    public static readonly List<ControlPanelItem> ControlPanelItems = new()
    {
        new ControlPanelItem
        {
            Name = "ST/RST",
            Description = "在工作模式做為啟動用，而在復歸模式時做為復歸用",
            DeviceType = "復歸式按鈕開關",
            IoAddress = "X16"
        },
        new ControlPanelItem
        {
            Name = "EMS",
            Description = "作為緊急停止之用 (需使用 NC 接點)",
            DeviceType = "壓扣式按鈕開關",
            IoAddress = "X17"
        },
        new ControlPanelItem
        {
            Name = "COS1",
            Description = "作為選擇「復歸模式」/「工作模式」之用",
            DeviceType = "二段切換式選擇開關",
            IoAddress = "X13"
        },
        new ControlPanelItem
        {
            Name = "COS2",
            Description = "作為選擇「指定功能」/「單一循環」/「連續循環」之用",
            DeviceType = "三段切換式選擇開關",
            IoAddress = "X14(L), X15(R)"
        },
        new ControlPanelItem
        {
            Name = "RL",
            Description = "紅色指示燈，運轉時亮，待機時滅，代表運轉燈",
            DeviceType = "指示燈",
            IoAddress = "Y15"
        },
        new ControlPanelItem
        {
            Name = "GL",
            Description = "綠色指示燈，待機時亮，運轉時滅，代表待機燈",
            DeviceType = "指示燈",
            IoAddress = "Y17"
        },
        new ControlPanelItem
        {
            Name = "YL",
            Description = "黃色指示燈，復歸中 0.5 秒亮/0.5 秒滅閃爍，代表復歸燈",
            DeviceType = "指示燈",
            IoAddress = "Y16"
        }
    };

    /// <summary>
    /// 輸入接點 X0 ~ X17 配置清單
    /// </summary>
    public static readonly Dictionary<string, IoPointInfo> Inputs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["X0"] = new IoPointInfo { Address = "X0", Code = "a1", Description = "推料缸前端點", DeviceType = "磁簧/極限感測器" },
        ["X1"] = new IoPointInfo { Address = "X1", Code = "a0", Description = "推料缸後端點", DeviceType = "磁簧/極限感測器" },
        ["X2"] = new IoPointInfo { Address = "X2", Code = "b0", Description = "迴轉缸 0°端點", DeviceType = "磁簧/極限感測器" },
        ["X3"] = new IoPointInfo { Address = "X3", Code = "b1", Description = "迴轉缸＋90° 端點", DeviceType = "磁簧/極限感測器" },
        ["X4"] = new IoPointInfo { Address = "X4", Code = "b2", Description = "迴轉缸－90°端點", DeviceType = "磁簧/極限感測器" },
        ["X5"] = new IoPointInfo { Address = "X5", Code = "ps1", Description = "真空壓力開關", DeviceType = "壓力感測器" },
        ["X6"] = new IoPointInfo { Address = "X6", Code = "d0", Description = "升降模組上端點", DeviceType = "磁簧/極限感測器" },
        ["X7"] = new IoPointInfo { Address = "X7", Code = "d1", Description = "升降模組下端點", DeviceType = "磁簧/極限感測器" },
        ["X10"] = new IoPointInfo { Address = "X10", Code = "s1", Description = "料件方向感測器", DeviceType = "光電/近接感測器" },
        ["X11"] = new IoPointInfo { Address = "X11", Code = "-", Description = "(未配置 / 備用)" },
        ["X12"] = new IoPointInfo { Address = "X12", Code = "-", Description = "(未配置 / 備用)" },
        ["X13"] = new IoPointInfo
        {
            Address = "X13",
            Code = "COS1",
            Description = "*復歸模式/工作模式",
            DeviceType = "二段切換式選擇開關",
            IsControlPanel = true
        },
        ["X14"] = new IoPointInfo
        {
            Address = "X14",
            Code = "COS2_L",
            Description = "*單一循環/指定功能",
            DeviceType = "三段切換式選擇開關(左)",
            IsControlPanel = true
        },
        ["X15"] = new IoPointInfo
        {
            Address = "X15",
            Code = "COS2_R",
            Description = "*單一循環/連續循環",
            DeviceType = "三段切換式選擇開關(右)",
            IsControlPanel = true
        },
        ["X16"] = new IoPointInfo
        {
            Address = "X16",
            Code = "ST/RST",
            Description = "啟動鈕/復歸鈕",
            DeviceType = "在工作模式做為啟動用，而在復歸模式時做為復歸用 (復歸式按鈕開關)",
            IsControlPanel = true
        },
        ["X17"] = new IoPointInfo
        {
            Address = "X17",
            Code = "EMS",
            Description = "緊急停止開關(NC)",
            DeviceType = "作為緊急停止之用 (需使用 NC 接點) 壓扣式按鈕開關",
            IsControlPanel = true,
            CustomOnColor = Color.FromArgb(255, 64, 64) // NC 接點/急停使用醒目紅
        }
    };

    /// <summary>
    /// 輸出接點 Y0 ~ Y17 配置清單
    /// </summary>
    public static readonly Dictionary<string, IoPointInfo> Outputs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Y0"] = new IoPointInfo { Address = "Y0", Code = "A", Description = "推料缸伸出", DeviceType = "電磁閥致動器" },
        ["Y1"] = new IoPointInfo { Address = "Y1", Code = "-", Description = "(未配置 / 備用)" },
        ["Y2"] = new IoPointInfo { Address = "Y2", Code = "-", Description = "(未配置 / 備用)" },
        ["Y3"] = new IoPointInfo { Address = "Y3", Code = "B1", Description = "迴轉缸轉至 0°/＋90°", DeviceType = "電磁閥致動器" },
        ["Y4"] = new IoPointInfo { Address = "Y4", Code = "B2", Description = "迴轉缸轉至 0°/－90°", DeviceType = "電磁閥致動器" },
        ["Y5"] = new IoPointInfo { Address = "Y5", Code = "C＋", Description = "真空吸盤吸", DeviceType = "真空產生/電磁閥" },
        ["Y6"] = new IoPointInfo { Address = "Y6", Code = "C－", Description = "真空吸盤放", DeviceType = "真空破壞/電磁閥" },
        ["Y7"] = new IoPointInfo { Address = "Y7", Code = "R1", Description = "升降模組馬達旋轉(M＋)", DeviceType = "馬達驅動器" },
        ["Y10"] = new IoPointInfo { Address = "Y10", Code = "-", Description = "(未配置 / 備用)" },
        ["Y11"] = new IoPointInfo { Address = "Y11", Code = "-", Description = "(未配置 / 備用)" },
        ["Y12"] = new IoPointInfo { Address = "Y12", Code = "-", Description = "(未配置 / 備用)" },
        ["Y13"] = new IoPointInfo { Address = "Y13", Code = "-", Description = "(未配置 / 備用)" },
        ["Y14"] = new IoPointInfo { Address = "Y14", Code = "-", Description = "(未配置 / 備用)" },
        ["Y15"] = new IoPointInfo
        {
            Address = "Y15",
            Code = "RL",
            Description = "紅燈 (運轉燈)",
            DeviceType = "紅色指示燈，運轉時亮，待機時滅，代表運轉燈",
            IsControlPanel = true,
            CustomOnColor = Color.FromArgb(255, 50, 50) // 紅色指示燈
        },
        ["Y16"] = new IoPointInfo
        {
            Address = "Y16",
            Code = "YL",
            Description = "黃燈 (復歸燈)",
            DeviceType = "黃色指示燈，復歸中 0.5 秒亮/0.5 秒滅閃爍，代表復歸燈",
            IsControlPanel = true,
            CustomOnColor = Color.FromArgb(255, 215, 0) // 黃色指示燈
        },
        ["Y17"] = new IoPointInfo
        {
            Address = "Y17",
            Code = "GL",
            Description = "綠燈 (待機燈)",
            DeviceType = "綠色指示燈，待機時亮，運轉時滅，代表待機燈",
            IsControlPanel = true,
            CustomOnColor = Color.FromArgb(0, 230, 118) // 綠色指示燈
        }
    };
}
