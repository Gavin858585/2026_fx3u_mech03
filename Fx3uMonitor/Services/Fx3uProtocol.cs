using System;
using System.Collections.Generic;

namespace Fx3uMonitor.Services;

/// <summary>
/// 三菱 FX3U 序列埠通訊協定處理器 (Programming Port Direct Protocol)
/// </summary>
public static class Fx3uProtocol
{
    // 定數定義 (STX, ETX, ACK, NAK)
    public const byte STX = 0x02;
    public const byte ETX = 0x03;
    public const byte ACK = 0x06;
    public const byte NAK = 0x15;

    // 讀取 Y0~Y17 命令 (位址 0x00A0, 長度 2 Byte, 檢查碼 66)
    // 02 30 30 30 41 30 30 32 03 36 36
    public static readonly byte[] ReadYCommand = new byte[]
    {
        0x02, 0x30, 0x30, 0x30, 0x41, 0x30, 0x30, 0x32, 0x03, 0x36, 0x36
    };

    // 讀取 X0~X17 命令 (位址 0x0080, 長度 2 Byte, 檢查碼 5D)
    // 02 30 30 30 38 30 30 32 03 35 44
    public static readonly byte[] ReadXCommand = new byte[]
    {
        0x02, 0x30, 0x30, 0x30, 0x38, 0x30, 0x30, 0x32, 0x03, 0x35, 0x44
    };

    // 點位名稱列表 (八進位表示法)
    public static readonly string[] PointNames = new string[]
    {
        "0", "1", "2", "3", "4", "5", "6", "7",
        "10", "11", "12", "13", "14", "15", "16", "17"
    };

    // 強制 Y 輸出 ON / OFF 指令對照表
    private static readonly Dictionary<string, (byte[] OnCmd, byte[] OffCmd)> ForceYCommands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Y0
            ["Y0"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x30, 0x30, 0x35, 0x03, 0x46, 0x46 },
                new byte[] { 0x02, 0x38, 0x30, 0x30, 0x30, 0x35, 0x03, 0x30, 0x30 }
            ),
            // Y1
            ["Y1"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x31, 0x30, 0x35, 0x03, 0x30, 0x30 },
                new byte[] { 0x02, 0x38, 0x30, 0x31, 0x30, 0x35, 0x03, 0x30, 0x31 }
            ),
            // Y2
            ["Y2"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x32, 0x30, 0x35, 0x03, 0x30, 0x31 },
                new byte[] { 0x02, 0x38, 0x30, 0x32, 0x30, 0x35, 0x03, 0x30, 0x32 }
            ),
            // Y3
            ["Y3"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x33, 0x30, 0x35, 0x03, 0x30, 0x32 },
                new byte[] { 0x02, 0x38, 0x30, 0x33, 0x30, 0x35, 0x03, 0x30, 0x33 }
            ),
            // Y4
            ["Y4"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x34, 0x30, 0x35, 0x03, 0x30, 0x33 },
                new byte[] { 0x02, 0x38, 0x30, 0x34, 0x30, 0x35, 0x03, 0x30, 0x34 }
            ),
            // Y5
            ["Y5"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x35, 0x30, 0x35, 0x03, 0x30, 0x34 },
                new byte[] { 0x02, 0x38, 0x30, 0x35, 0x30, 0x35, 0x03, 0x30, 0x35 }
            ),
            // Y6
            ["Y6"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x36, 0x30, 0x35, 0x03, 0x30, 0x35 },
                new byte[] { 0x02, 0x38, 0x30, 0x36, 0x30, 0x35, 0x03, 0x30, 0x36 }
            ),
            // Y7
            ["Y7"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x37, 0x30, 0x35, 0x03, 0x30, 0x36 },
                new byte[] { 0x02, 0x38, 0x30, 0x37, 0x30, 0x35, 0x03, 0x30, 0x37 }
            ),
            // Y10
            ["Y10"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x38, 0x30, 0x35, 0x03, 0x30, 0x37 },
                new byte[] { 0x02, 0x38, 0x30, 0x38, 0x30, 0x35, 0x03, 0x30, 0x38 }
            ),
            // Y11
            ["Y11"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x39, 0x30, 0x35, 0x03, 0x30, 0x38 },
                new byte[] { 0x02, 0x38, 0x30, 0x39, 0x30, 0x35, 0x03, 0x30, 0x39 }
            ),
            // Y12
            ["Y12"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x41, 0x30, 0x35, 0x03, 0x31, 0x30 },
                new byte[] { 0x02, 0x38, 0x30, 0x41, 0x30, 0x35, 0x03, 0x31, 0x31 }
            ),
            // Y13
            ["Y13"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x42, 0x30, 0x35, 0x03, 0x31, 0x31 },
                new byte[] { 0x02, 0x38, 0x30, 0x42, 0x30, 0x35, 0x03, 0x31, 0x32 }
            ),
            // Y14
            ["Y14"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x43, 0x30, 0x35, 0x03, 0x31, 0x32 },
                new byte[] { 0x02, 0x38, 0x30, 0x43, 0x30, 0x35, 0x03, 0x31, 0x33 }
            ),
            // Y15
            ["Y15"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x44, 0x30, 0x35, 0x03, 0x31, 0x33 },
                new byte[] { 0x02, 0x38, 0x30, 0x44, 0x30, 0x35, 0x03, 0x31, 0x34 }
            ),
            // Y16
            ["Y16"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x45, 0x30, 0x35, 0x03, 0x31, 0x34 },
                new byte[] { 0x02, 0x38, 0x30, 0x45, 0x30, 0x35, 0x03, 0x31, 0x35 }
            ),
            // Y17
            ["Y17"] = (
                new byte[] { 0x02, 0x37, 0x30, 0x46, 0x30, 0x35, 0x03, 0x31, 0x35 },
                new byte[] { 0x02, 0x38, 0x30, 0x46, 0x30, 0x35, 0x03, 0x31, 0x36 }
            )
        };

    /// <summary>
    /// 取得指定 Y 點位的強制 ON 或 OFF 指令 Byte 陣列
    /// </summary>
    public static byte[] GetForceYCommand(string point, bool state)
    {
        string normalized = point.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("Y"))
            normalized = "Y" + normalized;

        if (ForceYCommands.TryGetValue(normalized, out var pair))
        {
            return state ? pair.OnCmd : pair.OffCmd;
        }

        throw excitingException($"未知的 Y 點位: {point}");

        static ArgumentException excitingException(string msg) => new(msg);
    }

    /// <summary>
    /// 解析 PLC 回傳的 16 個接點狀態 (適用於 X0~X17 與 Y0~Y17)
    /// 回傳格式: 02 [B0] [B1] [B2] [B3] 03 [SUM1] [SUM2]
    /// B0: 4~7 (高 nibble)
    /// B1: 0~3 (低 nibble)
    /// B2: 14~17 (高 nibble)
    /// B3: 10~13 (低 nibble)
    /// </summary>
    /// <param name="response">PLC 回傳的原始 Byte 陣列</param>
    /// <param name="prefix">'X' 或 'Y'</param>
    /// <returns>點位名稱與布林狀態對照表</returns>
    public static Dictionary<string, bool> ParseBitPointsResponse(byte[] response, char prefix)
    {
        var result = new Dictionary<string, bool>(16, StringComparer.OrdinalIgnoreCase);

        // 預設全為 false
        foreach (var p in PointNames)
        {
            result[$"{prefix}{p}"] = false;
        }

        if (response == null || response.Length < 6)
        {
            return result;
        }

        // 尋找 STX (0x02) 的位置
        int stxIndex = Array.IndexOf(response, STX);
        if (stxIndex < 0 || stxIndex + 5 >= response.Length)
        {
            return result;
        }

        // 取出 4 個資料 Byte: B0, B1, B2, B3
        byte b0 = response[stxIndex + 1];
        byte b1 = response[stxIndex + 2];
        byte b2 = response[stxIndex + 3];
        byte b3 = response[stxIndex + 4];

        int val0 = HexCharToNibble((char)b0); // 4 ~ 7
        int val1 = HexCharToNibble((char)b1); // 0 ~ 3
        int val2 = HexCharToNibble((char)b2); // 14 ~ 17
        int val3 = HexCharToNibble((char)b3); // 10 ~ 13

        // B1: 0 ~ 3
        result[$"{prefix}0"] = (val1 & 0x01) != 0;
        result[$"{prefix}1"] = (val1 & 0x02) != 0;
        result[$"{prefix}2"] = (val1 & 0x04) != 0;
        result[$"{prefix}3"] = (val1 & 0x08) != 0;

        // B0: 4 ~ 7
        result[$"{prefix}4"] = (val0 & 0x01) != 0;
        result[$"{prefix}5"] = (val0 & 0x02) != 0;
        result[$"{prefix}6"] = (val0 & 0x04) != 0;
        result[$"{prefix}7"] = (val0 & 0x08) != 0;

        // B3: 10 ~ 13
        result[$"{prefix}10"] = (val3 & 0x01) != 0;
        result[$"{prefix}11"] = (val3 & 0x02) != 0;
        result[$"{prefix}12"] = (val3 & 0x04) != 0;
        result[$"{prefix}13"] = (val3 & 0x08) != 0;

        // B2: 14 ~ 17
        result[$"{prefix}14"] = (val2 & 0x01) != 0;
        result[$"{prefix}15"] = (val2 & 0x02) != 0;
        result[$"{prefix}16"] = (val2 & 0x04) != 0;
        result[$"{prefix}17"] = (val2 & 0x08) != 0;

        return result;
    }

    /// <summary>
    /// 將狀態字典編碼為 FX3U 回傳格式的 Byte 陣列 (供模擬模式使用)
    /// </summary>
    public static byte[] EncodeBitPointsResponse(Dictionary<string, bool> points, char prefix)
    {
        int val0 = 0; // 4~7
        if (points.TryGetValue($"{prefix}4", out bool p4) && p4) val0 |= 0x01;
        if (points.TryGetValue($"{prefix}5", out bool p5) && p5) val0 |= 0x02;
        if (points.TryGetValue($"{prefix}6", out bool p6) && p6) val0 |= 0x04;
        if (points.TryGetValue($"{prefix}7", out bool p7) && p7) val0 |= 0x08;

        int val1 = 0; // 0~3
        if (points.TryGetValue($"{prefix}0", out bool p0) && p0) val1 |= 0x01;
        if (points.TryGetValue($"{prefix}1", out bool p1) && p1) val1 |= 0x02;
        if (points.TryGetValue($"{prefix}2", out bool p2) && p2) val1 |= 0x04;
        if (points.TryGetValue($"{prefix}3", out bool p3) && p3) val1 |= 0x08;

        int val2 = 0; // 14~17
        if (points.TryGetValue($"{prefix}14", out bool p14) && p14) val2 |= 0x01;
        if (points.TryGetValue($"{prefix}15", out bool p15) && p15) val2 |= 0x02;
        if (points.TryGetValue($"{prefix}16", out bool p16) && p16) val2 |= 0x04;
        if (points.TryGetValue($"{prefix}17", out bool p17) && p17) val2 |= 0x08;

        int val3 = 0; // 10~13
        if (points.TryGetValue($"{prefix}10", out bool p10) && p10) val3 |= 0x01;
        if (points.TryGetValue($"{prefix}11", out bool p11) && p11) val3 |= 0x02;
        if (points.TryGetValue($"{prefix}12", out bool p12) && p12) val3 |= 0x04;
        if (points.TryGetValue($"{prefix}13", out bool p13) && p13) val3 |= 0x08;

        byte b0 = (byte)NibbleToHexChar(val0);
        byte b1 = (byte)NibbleToHexChar(val1);
        byte b2 = (byte)NibbleToHexChar(val2);
        byte b3 = (byte)NibbleToHexChar(val3);

        // Checksum = (b0 + b1 + b2 + b3 + ETX) & 0xFF
        int sum = (b0 + b1 + b2 + b3 + ETX) & 0xFF;
        string sumHex = sum.ToString("X2");
        byte sum1 = (byte)sumHex[0];
        byte sum2 = (byte)sumHex[1];

        return new byte[] { STX, b0, b1, b2, b3, ETX, sum1, sum2 };
    }

    /// <summary>
    /// 十六進位字元轉 0~15 數值
    /// </summary>
    public static int HexCharToNibble(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        if (c >= 'A' && c <= 'F') return c - 'A' + 10;
        if (c >= 'a' && c <= 'f') return c - 'a' + 10;
        return 0;
    }

    /// <summary>
    /// 0~15 數值轉十六進位大寫字元
    /// </summary>
    public static char NibbleToHexChar(int n)
    {
        n &= 0x0F;
        if (n < 10) return (char)('0' + n);
        return (char)('A' + (n - 10));
    }

    /// <summary>
    /// 將 Byte 陣列格式化為 Hex 字串 (例如: "02 30 30 03 36")
    /// </summary>
    public static string ToHexString(byte[] bytes, int length = -1)
    {
        if (bytes == null || bytes.Length == 0) return string.Empty;
        int len = length < 0 ? bytes.Length : Math.Min(length, bytes.Length);
        return BitConverter.ToString(bytes, 0, len).Replace("-", " ");
    }
}
