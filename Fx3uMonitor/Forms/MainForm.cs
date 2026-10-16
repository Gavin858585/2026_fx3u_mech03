using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using Fx3uMonitor.Controls;
using Fx3uMonitor.Models;
using Fx3uMonitor.Services;

namespace Fx3uMonitor.Forms;

public partial class MainForm : Form
{
    private readonly PlcService _plcService;
    private readonly MechatronicsLevel3Service _station3Service;
    private readonly Dictionary<string, LedIndicator> _xLeds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LedIndicator> _yLeds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _yOnButtons = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Button> _yOffButtons = new(StringComparer.OrdinalIgnoreCase);

    // HMI 控制面板虛擬元件引用
    private LedIndicator _hmiLedRl = null!;
    private LedIndicator _hmiLedYl = null!;
    private LedIndicator _hmiLedGl = null!;
    private Button _hmiBtnEms = null!;
    private Button _hmiBtnStRst = null!;
    private Label _hmiLblCos1 = null!;
    private Label _hmiLblCos2 = null!;
    private Label _hmiLblMachineState = null!;
    private Button _hmiBtnMarquee = null!;

    public MainForm()
    {
        InitializeComponent();

        _plcService = new PlcService();
        _plcService.OnConnectionStateChanged += PlcService_OnConnectionStateChanged;
        _plcService.OnCommunicationLog += PlcService_OnCommunicationLog;
        _plcService.OnInputsUpdated += PlcService_OnInputsUpdated;
        _plcService.OnOutputsUpdated += PlcService_OnOutputsUpdated;
        _plcService.OnCycleTimeUpdated += PlcService_OnCycleTimeUpdated;

        _station3Service = new MechatronicsLevel3Service(_plcService);
        _station3Service.OnStepProgress += Station3Service_OnStepProgress;
        _station3Service.OnExecutionStateChanged += Station3Service_OnExecutionStateChanged;
        _station3Service.OnPieceCountUpdated += Station3Service_OnPieceCountUpdated;
        _station3Service.OnLogAction += Station3Service_OnLogAction;
        _station3Service.OnMarqueeStateChanged += Station3Service_OnMarqueeStateChanged;

        BuildPointControls();
        BuildHmiPanel();
        BuildSpecTables();
        RefreshSerialPorts();

        cboInterval.SelectedIndex = 1; // 預設 200 ms
    }

    /// <summary>
    /// 動態建立 X0~X17 與 Y0~Y17 監控與控制元件
    /// </summary>
    private void BuildPointControls()
    {
        pnlInputsBank1.SuspendLayout();
        pnlInputsBank2.SuspendLayout();
        pnlOutputsBank1.SuspendLayout();
        pnlOutputsBank2.SuspendLayout();

        string[] bank1 = { "0", "1", "2", "3", "4", "5", "6", "7" };
        string[] bank2 = { "10", "11", "12", "13", "14", "15", "16", "17" };

        // 建立輸入 X 卡片
        foreach (var p in bank1)
        {
            var led = CreateInputCard($"X{p}", pnlInputsBank1);
            _xLeds[$"X{p}"] = led;
        }

        foreach (var p in bank2)
        {
            var led = CreateInputCard($"X{p}", pnlInputsBank2);
            _xLeds[$"X{p}"] = led;
        }

        // 建立輸出 Y 卡片
        foreach (var p in bank1)
        {
            CreateOutputCard($"Y{p}", pnlOutputsBank1);
        }

        foreach (var p in bank2)
        {
            CreateOutputCard($"Y{p}", pnlOutputsBank2);
        }

        pnlInputsBank1.ResumeLayout(true);
        pnlInputsBank2.ResumeLayout(true);
        pnlOutputsBank1.ResumeLayout(true);
        pnlOutputsBank2.ResumeLayout(true);
    }

    private LedIndicator CreateInputCard(string address, Panel parent)
    {
        IoConfig.Inputs.TryGetValue(address, out var info);
        info ??= new IoPointInfo { Address = address, Code = "-", Description = "(未配置)" };

        var card = new Panel
        {
            Size = new Size(310, 52),
            BackColor = Color.FromArgb(32, 38, 48),
            Margin = new Padding(3),
            Padding = new Padding(3)
        };

        Color onColor = info.CustomOnColor ?? Color.FromArgb(0, 230, 118);

        var led = new LedIndicator
        {
            IsOn = false,
            OnColor = onColor,
            OffColor = Color.FromArgb(50, 60, 72),
            LedSize = 22,
            Location = new Point(6, 14),
            Size = new Size(26, 26),
            Cursor = Cursors.Hand
        };

        // 標籤 1：位址與編號代碼
        var lblTitle = new Label
        {
            Location = new Point(34, 5),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = info.IsAssigned ? Color.FromArgb(240, 245, 250) : Color.FromArgb(120, 130, 145),
            Text = info.IsAssigned ? $"{address} [{info.Code}]" : address
        };

        // 若屬於控制面板，加上金色標籤提示 (左移緊鄰標題右側，避免遮擋)
        if (info.IsControlPanel)
        {
            int titleWidth = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Width;
            var lblCp = new Label
            {
                Location = new Point(lblTitle.Left + titleWidth + 6, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Text = "【面板】"
            };
            card.Controls.Add(lblCp);
        }

        // 標籤 2：中文註釋與功能說明
        var lblDesc = new Label
        {
            Location = new Point(34, 27),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = info.IsAssigned ? Color.FromArgb(160, 175, 195) : Color.FromArgb(90, 100, 115),
            Text = info.Description
        };

        // 提示說明
        var tip = new ToolTip();
        string tipText = $"[{address}] {info.Code} - {info.Description}\n" +
                         $"類型: {(string.IsNullOrEmpty(info.DeviceType) ? "數位輸入" : info.DeviceType)}\n" +
                         $"(模擬模式下點擊可切換 ON/OFF)";
        tip.SetToolTip(card, tipText);
        tip.SetToolTip(led, tipText);
        tip.SetToolTip(lblTitle, tipText);
        tip.SetToolTip(lblDesc, tipText);

        led.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                bool newState = !led.IsOn;
                _plcService.SetSimulatedX(address, newState);
            }
        };

        card.Controls.Add(led);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblDesc);

        parent.Controls.Add(card);
        return led;
    }

    private void CreateOutputCard(string address, Panel parent)
    {
        IoConfig.Outputs.TryGetValue(address, out var info);
        info ??= new IoPointInfo { Address = address, Code = "-", Description = "(未配置)" };

        var card = new Panel
        {
            Size = new Size(335, 52),
            BackColor = Color.FromArgb(32, 38, 48),
            Margin = new Padding(3),
            Padding = new Padding(3)
        };

        Color onColor = info.CustomOnColor ?? Color.FromArgb(255, 145, 0);

        var led = new LedIndicator
        {
            IsOn = false,
            OnColor = onColor,
            OffColor = Color.FromArgb(50, 60, 72),
            LedSize = 22,
            Location = new Point(6, 14),
            Size = new Size(26, 26)
        };

        var lblTitle = new Label
        {
            Location = new Point(34, 5),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = info.IsAssigned ? Color.FromArgb(240, 245, 250) : Color.FromArgb(120, 130, 145),
            Text = info.IsAssigned ? $"{address} [{info.Code}]" : address
        };

        if (info.IsControlPanel)
        {
            int titleWidth = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Width;
            var lblCp = new Label
            {
                Location = new Point(lblTitle.Left + titleWidth + 6, 6),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 215, 0),
                Text = "【面板】"
            };
            card.Controls.Add(lblCp);
        }

        var lblDesc = new Label
        {
            Location = new Point(34, 27),
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = info.IsAssigned ? Color.FromArgb(160, 175, 195) : Color.FromArgb(90, 100, 115),
            Text = info.Description
        };

        var btnOn = new Button
        {
            Text = "ON",
            Size = new Size(46, 30),
            Location = new Point(234, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(16, 120, 65),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOn.FlatAppearance.BorderSize = 0;

        var btnOff = new Button
        {
            Text = "OFF",
            Size = new Size(46, 30),
            Location = new Point(283, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(140, 40, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOff.FlatAppearance.BorderSize = 0;

        btnOn.Click += (s, e) =>
        {
            if (!_plcService.IsConnected)
            {
                MessageBox.Show("請先連線至 PLC 或勾選模擬模式！", "未連線提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _plcService.ForceY(address, true);
        };

        btnOff.Click += (s, e) =>
        {
            if (!_plcService.IsConnected)
            {
                MessageBox.Show("請先連線至 PLC 或勾選模擬模式！", "未連線提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _plcService.ForceY(address, false);
        };

        var tip = new ToolTip();
        string tipText = $"[{address}] {info.Code} - {info.Description}\n" +
                         $"類型: {(string.IsNullOrEmpty(info.DeviceType) ? "數位輸出" : info.DeviceType)}";
        tip.SetToolTip(card, tipText);
        tip.SetToolTip(lblTitle, tipText);
        tip.SetToolTip(lblDesc, tipText);

        card.Controls.Add(led);
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblDesc);
        card.Controls.Add(btnOn);
        card.Controls.Add(btnOff);

        parent.Controls.Add(card);

        _yLeds[address] = led;
        _yOnButtons[address] = btnOn;
        _yOffButtons[address] = btnOff;
    }

    /// <summary>
    /// 建立機台實體控制面板虛擬介面 (HMI)
    /// </summary>
    private void BuildHmiPanel()
    {
        pnlHmiContent.SuspendLayout();

        // 1. 三色指示燈塔卡片
        var grpTower = new GroupBox
        {
            Text = "【三色指示燈號 (Tower Lamps)】",
            Size = new Size(800, 110),
            Location = new Point(10, 10),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 215, 0),
            BackColor = Color.FromArgb(32, 38, 48)
        };

        _hmiLedRl = new LedIndicator
        {
            Location = new Point(20, 32),
            Size = new Size(180, 50),
            LedSize = 32,
            OnColor = Color.FromArgb(255, 50, 50),
            Text = "RL 運轉燈 (Y15)\n運轉時亮 / 待機滅",
            ForeColor = Color.FromArgb(240, 245, 250),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _hmiLedYl = new LedIndicator
        {
            Location = new Point(210, 32),
            Size = new Size(180, 50),
            LedSize = 32,
            OnColor = Color.FromArgb(255, 215, 0),
            Text = "YL 復歸燈 (Y16)\n復歸中 0.5s 閃爍",
            ForeColor = Color.FromArgb(240, 245, 250),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _hmiLedGl = new LedIndicator
        {
            Location = new Point(400, 32),
            Size = new Size(180, 50),
            LedSize = 32,
            OnColor = Color.FromArgb(0, 230, 118),
            Text = "GL 待機燈 (Y17)\n待機時亮 / 運轉滅",
            ForeColor = Color.FromArgb(240, 245, 250),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _hmiBtnMarquee = new Button
        {
            Location = new Point(595, 32),
            Size = new Size(185, 50),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(217, 119, 6),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Text = "★ 跑馬燈測試\n(三燈 3 循環)",
            Cursor = Cursors.Hand
        };
        _hmiBtnMarquee.FlatAppearance.BorderSize = 0;
        _hmiBtnMarquee.Click += btnMarqueeTest_Click;

        grpTower.Controls.Add(_hmiLedRl);
        grpTower.Controls.Add(_hmiLedYl);
        grpTower.Controls.Add(_hmiLedGl);
        grpTower.Controls.Add(_hmiBtnMarquee);

        // 2. 按鈕與選擇開關面板卡片
        var grpSwitches = new GroupBox
        {
            Text = "【操作開關與按鈕 (Switches & Buttons)】",
            Size = new Size(800, 220),
            Location = new Point(10, 130),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 210, 255),
            BackColor = Color.FromArgb(32, 38, 48)
        };

        // EMS 緊急停止開關 (NC 接點 X17)
        _hmiBtnEms = new Button
        {
            Location = new Point(30, 35),
            Size = new Size(160, 70),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(180, 30, 30),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = "EMS 緊急停止\n(X17 - NC 接點)\n[正常導通]",
            Cursor = Cursors.Hand
        };
        _hmiBtnEms.FlatAppearance.BorderSize = 2;
        _hmiBtnEms.FlatAppearance.BorderColor = Color.FromArgb(255, 100, 100);

        _hmiBtnEms.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                // NC 接點：正常為 ON，按下去為 OFF (斷路)
                bool currentX17 = _xLeds["X17"].IsOn;
                _plcService.SetSimulatedX("X17", !currentX17);
            }
        };

        // ST/RST 啟動/復歸按鈕 (X16)
        _hmiBtnStRst = new Button
        {
            Location = new Point(210, 35),
            Size = new Size(160, 70),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(25, 110, 180),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Text = "ST / RST 鈕\n(X16)\n啟動 / 復歸",
            Cursor = Cursors.Hand
        };
        _hmiBtnStRst.FlatAppearance.BorderSize = 1;

        _hmiBtnStRst.MouseDown += (s, e) =>
        {
            if (_plcService.IsSimulationMode) _plcService.SetSimulatedX("X16", true);
            _station3Service.ResumeJog();
            _station3Service.ResumeFromMaterialReload();
        };
        _hmiBtnStRst.MouseUp += (s, e) =>
        {
            if (_plcService.IsSimulationMode) _plcService.SetSimulatedX("X16", false);
        };

        // COS1 選擇開關 (X13)
        var pnlCos1 = new Panel
        {
            Location = new Point(400, 35),
            Size = new Size(180, 160),
            BackColor = Color.FromArgb(25, 30, 38),
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblCos1Title = new Label
        {
            Location = new Point(6, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 215, 235),
            Text = "COS1 二段選擇 (X13)"
        };
        _hmiLblCos1 = new Label
        {
            Location = new Point(6, 32),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 230, 118),
            Text = "目前: 復歸模式 (OFF)"
        };
        var btnToggleCos1 = new Button
        {
            Location = new Point(10, 75),
            Size = new Size(158, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 56, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Text = "切換 復歸 / 工作",
            Cursor = Cursors.Hand
        };
        btnToggleCos1.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                bool cur = _xLeds["X13"].IsOn;
                _plcService.SetSimulatedX("X13", !cur);
            }
        };
        pnlCos1.Controls.Add(lblCos1Title);
        pnlCos1.Controls.Add(_hmiLblCos1);
        pnlCos1.Controls.Add(btnToggleCos1);

        // COS2 選擇開關 (X14, X15)
        var pnlCos2 = new Panel
        {
            Location = new Point(595, 35),
            Size = new Size(190, 160),
            BackColor = Color.FromArgb(25, 30, 38),
            BorderStyle = BorderStyle.FixedSingle
        };
        var lblCos2Title = new Label
        {
            Location = new Point(6, 8),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(200, 215, 235),
            Text = "COS2 三段選擇 (X14, X15)"
        };
        _hmiLblCos2 = new Label
        {
            Location = new Point(6, 32),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 210, 255),
            Text = "目前: 單一循環 (中)"
        };
        var btnCos2L = new Button
        {
            Location = new Point(8, 70),
            Size = new Size(84, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 56, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Text = "指定功能(L)",
            Cursor = Cursors.Hand
        };
        var btnCos2Mid = new Button
        {
            Location = new Point(96, 70),
            Size = new Size(84, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 56, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Text = "單一循環",
            Cursor = Cursors.Hand
        };
        var btnCos2R = new Button
        {
            Location = new Point(8, 110),
            Size = new Size(172, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 56, 70),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
            Text = "連續循環(R)",
            Cursor = Cursors.Hand
        };

        btnCos2L.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                _plcService.SetSimulatedX("X14", true);
                _plcService.SetSimulatedX("X15", false);
            }
        };
        btnCos2Mid.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                _plcService.SetSimulatedX("X14", false);
                _plcService.SetSimulatedX("X15", false);
            }
        };
        btnCos2R.Click += (s, e) =>
        {
            if (_plcService.IsSimulationMode)
            {
                _plcService.SetSimulatedX("X14", false);
                _plcService.SetSimulatedX("X15", true);
            }
        };

        pnlCos2.Controls.Add(lblCos2Title);
        pnlCos2.Controls.Add(_hmiLblCos2);
        pnlCos2.Controls.Add(btnCos2L);
        pnlCos2.Controls.Add(btnCos2Mid);
        pnlCos2.Controls.Add(btnCos2R);

        grpSwitches.Controls.Add(_hmiBtnEms);
        grpSwitches.Controls.Add(_hmiBtnStRst);
        grpSwitches.Controls.Add(pnlCos1);
        grpSwitches.Controls.Add(pnlCos2);

        // 3. 機台狀態綜合資訊卡片
        var grpState = new GroupBox
        {
            Text = "【機台即時運轉狀態診斷】",
            Size = new Size(800, 120),
            Location = new Point(10, 360),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 230, 118),
            BackColor = Color.FromArgb(32, 38, 48)
        };

        _hmiLblMachineState = new Label
        {
            Location = new Point(20, 28),
            Size = new Size(760, 75),
            Font = new Font("Cascadia Mono", 10.5F),
            ForeColor = Color.FromArgb(230, 240, 255),
            Text = "模式狀態: 尚未讀取 PLC 狀態\n" +
                   "安全連鎖: EMS 正常\n" +
                   "指示狀態: 待機中"
        };
        grpState.Controls.Add(_hmiLblMachineState);

        pnlHmiContent.Controls.Add(grpTower);
        pnlHmiContent.Controls.Add(grpSwitches);
        pnlHmiContent.Controls.Add(grpState);

        pnlHmiContent.ResumeLayout(true);
    }

    /// <summary>
    /// 填入 I/O 配置與控制面板規格表
    /// </summary>
    private void BuildSpecTables()
    {
        // 1. 控制面板說明表
        dgvControlPanel.Columns.Add("Name", "名稱");
        dgvControlPanel.Columns.Add("Desc", "說明");
        dgvControlPanel.Columns.Add("Type", "類型");
        dgvControlPanel.Columns.Add("Io", "對應 I/O");

        dgvControlPanel.Columns["Name"].Width = 100;
        dgvControlPanel.Columns["Type"].Width = 160;
        dgvControlPanel.Columns["Io"].Width = 100;

        foreach (var item in IoConfig.ControlPanelItems)
        {
            dgvControlPanel.Rows.Add(item.Name, item.Description, item.DeviceType, item.IoAddress);
        }

        // 2. 輸入 I/O 配置表
        dgvInputs.Columns.Add("Address", "位址");
        dgvInputs.Columns.Add("Code", "編號");
        dgvInputs.Columns.Add("Desc", "註釋");
        dgvInputs.Columns.Add("Status", "目前狀態");

        dgvInputs.Columns["Address"].Width = 80;
        dgvInputs.Columns["Code"].Width = 90;
        dgvInputs.Columns["Status"].Width = 100;

        foreach (var p in Fx3uProtocol.PointNames)
        {
            string addr = $"X{p}";
            IoConfig.Inputs.TryGetValue(addr, out var info);
            dgvInputs.Rows.Add(addr, info?.Code ?? "-", info?.Description ?? "(未配置)", "OFF");
        }

        // 3. 輸出 I/O 配置表
        dgvOutputs.Columns.Add("Address", "位址");
        dgvOutputs.Columns.Add("Code", "編號");
        dgvOutputs.Columns.Add("Desc", "註釋");
        dgvOutputs.Columns.Add("Status", "目前狀態");

        dgvOutputs.Columns["Address"].Width = 80;
        dgvOutputs.Columns["Code"].Width = 90;
        dgvOutputs.Columns["Status"].Width = 100;

        foreach (var p in Fx3uProtocol.PointNames)
        {
            string addr = $"Y{p}";
            IoConfig.Outputs.TryGetValue(addr, out var info);
            dgvOutputs.Rows.Add(addr, info?.Code ?? "-", info?.Description ?? "(未配置)", "OFF");
        }

        // 設定暗黑主題表格樣式
        StyleDataGridView(dgvControlPanel);
        StyleDataGridView(dgvInputs);
        StyleDataGridView(dgvOutputs);
    }

    private static void StyleDataGridView(DataGridView dgv)
    {
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 48, 62);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(230, 240, 255);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dgv.DefaultCellStyle.BackColor = Color.FromArgb(28, 33, 43);
        dgv.DefaultCellStyle.ForeColor = Color.FromArgb(220, 225, 235);
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(45, 65, 95);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.GridColor = Color.FromArgb(50, 60, 75);
    }

    private void RefreshSerialPorts()
    {
        cboPorts.Items.Clear();
        string[] ports = SerialPort.GetPortNames();

        foreach (var port in ports.OrderBy(p => p))
        {
            cboPorts.Items.Add(port);
        }

        if (cboPorts.Items.Count > 0)
        {
            cboPorts.SelectedIndex = 0;
        }
    }

    private void btnRefreshPorts_Click(object sender, EventArgs e)
    {
        RefreshSerialPorts();
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        if (_plcService.IsConnected)
        {
            _plcService.Disconnect();
            return;
        }

        bool isSimulation = chkSimulation.Checked;
        string selectedPort = cboPorts.SelectedItem?.ToString() ?? string.Empty;

        if (!isSimulation && string.IsNullOrWhiteSpace(selectedPort))
        {
            MessageBox.Show("請先選擇通訊序列埠 (COM Port) 或勾選「模擬模式」！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 若啟動模擬模式，預設將 EMS(X17) 設為 NC 導通狀態
        if (isSimulation)
        {
            _plcService.SetSimulatedX("X17", true);
        }

        _plcService.Connect(selectedPort, isSimulation);
    }

    private void cboInterval_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (int.TryParse(cboInterval.SelectedItem?.ToString()?.Replace("ms", "").Trim(), out int ms))
        {
            _plcService.PollIntervalMs = ms;
        }
    }

    private void PlcService_OnConnectionStateChanged(bool isConnected, string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PlcService_OnConnectionStateChanged(isConnected, message)));
            return;
        }

        btnConnect.Text = isConnected ? "中斷連線 (Disconnect)" : "開始連線 (Connect)";
        btnConnect.BackColor = isConnected ? Color.FromArgb(190, 40, 40) : Color.FromArgb(16, 120, 65);
        cboPorts.Enabled = !isConnected;
        chkSimulation.Enabled = !isConnected;
        btnRefreshPorts.Enabled = !isConnected;

        if (isConnected)
        {
            lblConnectionStatus.Text = _plcService.IsSimulationMode ? "● 模擬連線中" : $"● 已連線 ({_plcService.CurrentPortName})";
            lblConnectionStatus.ForeColor = _plcService.IsSimulationMode ? Color.FromArgb(180, 120, 255) : Color.FromArgb(0, 230, 118);
        }
        else
        {
            lblConnectionStatus.Text = "● 未連線";
            lblConnectionStatus.ForeColor = Color.FromArgb(160, 170, 185);

            foreach (var led in _xLeds.Values) led.IsOn = false;
            foreach (var led in _yLeds.Values) led.IsOn = false;

            if (_hmiLedRl != null) _hmiLedRl.IsOn = false;
            if (_hmiLedYl != null) _hmiLedYl.IsOn = false;
            if (_hmiLedGl != null) _hmiLedGl.IsOn = false;
        }

        statusConnection.Text = $"連線狀態: {message}";
    }

    private void PlcService_OnInputsUpdated(Dictionary<string, bool> inputs)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PlcService_OnInputsUpdated(inputs)));
            return;
        }

        // 饋送最新輸入狀態給丙三動作引擎，並檢查調機 ST/RST 恢復
        _station3Service.ProvideCurrentInputs(inputs);
        if (inputs.TryGetValue("X16", out bool stRst) && stRst)
        {
            _station3Service.ResumeJog();
            _station3Service.ResumeFromMaterialReload();
        }

        // 更新卡片 LED
        foreach (var kv in inputs)
        {
            if (_xLeds.TryGetValue(kv.Key, out var led))
            {
                led.IsOn = kv.Value;
            }
        }

        // 更新表格即時狀態
        for (int i = 0; i < dgvInputs.Rows.Count; i++)
        {
            string addr = dgvInputs.Rows[i].Cells["Address"].Value?.ToString() ?? "";
            if (inputs.TryGetValue(addr, out bool val))
            {
                dgvInputs.Rows[i].Cells["Status"].Value = val ? "ON" : "OFF";
                dgvInputs.Rows[i].Cells["Status"].Style.ForeColor = val ? Color.FromArgb(0, 230, 118) : Color.FromArgb(160, 170, 185);
            }
        }

        // 更新 HMI 控制面板狀態
        UpdateHmiControlPanel(inputs, null);
    }

    private void PlcService_OnOutputsUpdated(Dictionary<string, bool> outputs)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PlcService_OnOutputsUpdated(outputs)));
            return;
        }

        foreach (var kv in outputs)
        {
            if (_yLeds.TryGetValue(kv.Key, out var led))
            {
                led.IsOn = kv.Value;
            }
        }

        for (int i = 0; i < dgvOutputs.Rows.Count; i++)
        {
            string addr = dgvOutputs.Rows[i].Cells["Address"].Value?.ToString() ?? "";
            if (outputs.TryGetValue(addr, out bool val))
            {
                dgvOutputs.Rows[i].Cells["Status"].Value = val ? "ON" : "OFF";
                dgvOutputs.Rows[i].Cells["Status"].Style.ForeColor = val ? Color.FromArgb(255, 165, 0) : Color.FromArgb(160, 170, 185);
            }
        }

        // 更新 HMI 燈號
        if (outputs.TryGetValue("Y15", out bool rl)) _hmiLedRl.IsOn = rl;
        if (outputs.TryGetValue("Y16", out bool yl)) _hmiLedYl.IsOn = yl;
        if (outputs.TryGetValue("Y17", out bool gl)) _hmiLedGl.IsOn = gl;

        UpdateHmiControlPanel(null, outputs);
    }

    private void UpdateHmiControlPanel(Dictionary<string, bool>? inputs, Dictionary<string, bool>? outputs)
    {
        bool emsClosed = _xLeds["X17"].IsOn;  // NC 接點
        bool stRst = _xLeds["X16"].IsOn;
        bool cos1 = _xLeds["X13"].IsOn;
        bool cos2L = _xLeds["X14"].IsOn;
        bool cos2R = _xLeds["X15"].IsOn;

        bool rl = _yLeds["Y15"].IsOn;
        bool yl = _yLeds["Y16"].IsOn;
        bool gl = _yLeds["Y17"].IsOn;

        // EMS 按鈕視覺更新 (NC: 導通為正常，斷路為急停觸發)
        if (emsClosed)
        {
            _hmiBtnEms.BackColor = Color.FromArgb(160, 30, 30);
            _hmiBtnEms.Text = "EMS 緊急停止\n(X17 - NC 接點)\n[正常導通]";
        }
        else
        {
            _hmiBtnEms.BackColor = Color.FromArgb(255, 0, 0);
            _hmiBtnEms.Text = "EMS 急停已動作！\n(X17 - 斷路)\n[按下解除/復歸]";
        }

        // COS1 說明
        _hmiLblCos1.Text = cos1 ? "目前: 工作模式 (ON)" : "目前: 復歸模式 (OFF)";
        _hmiLblCos1.ForeColor = cos1 ? Color.FromArgb(0, 230, 118) : Color.FromArgb(255, 215, 0);

        // COS2 說明
        string cos2Mode = (cos2L, cos2R) switch
        {
            (true, false) => "指定功能 (L)",
            (false, true) => "連續循環 (R)",
            _ => "單一循環 (中)"
        };
        _hmiLblCos2.Text = $"目前: {cos2Mode}";

        // 綜合運轉判定文字
        string emsDesc = emsClosed ? "安全迴路: 正常 (NC 導通)" : "安全迴路: 【急停動作中】(斷路)";
        string modeDesc = cos1 ? "運轉主模式: 工作模式" : "運轉主模式: 復歸模式";
        string lightDesc = rl ? "指示燈: 運轉中 (RL 亮)" :
                           yl ? "指示燈: 復歸中 (YL 閃爍)" :
                           gl ? "指示燈: 待機中 (GL 亮)" : "指示燈: 全滅";

        _hmiLblMachineState.Text = $"{modeDesc} | 循環設定: {cos2Mode}\n" +
                                   $"{emsDesc} | ST/RST 鈕: {(stRst ? "按下 (ON)" : "放開 (OFF)")}\n" +
                                   $"{lightDesc}";
    }

    private void PlcService_OnCycleTimeUpdated(long cycleMs)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PlcService_OnCycleTimeUpdated(cycleMs)));
            return;
        }

        statusCycleTime.Text = $"輪詢延遲: {cycleMs} ms";
        statusTxRx.Text = $"TX: {_plcService.TxCount} | RX: {_plcService.RxCount} | 錯誤: {_plcService.ErrorCount}";
    }

    private void PlcService_OnCommunicationLog(string direction, byte[] bytes, string description)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => PlcService_OnCommunicationLog(direction, bytes, description)));
            return;
        }

        if (chkPauseLog.Checked) return;

        // 當勾選「僅顯示動作步驟 (精簡)」時，過濾所有底層通訊封包，只呈現重大步序 (STEP/ACTION) 與通訊錯誤 (ERR)
        if (chkFilterLog.Checked && direction != "STEP" && direction != "ACTION" && direction != "ERR")
        {
            return;
        }

        string time = DateTime.Now.ToString("HH:mm:ss.fff");
        string hex = bytes.Length > 0 ? Fx3uProtocol.ToHexString(bytes) : string.Empty;

        Color dirColor = direction switch
        {
            "TX" => Color.FromArgb(70, 160, 245),
            "RX" => Color.FromArgb(0, 230, 118),
            "STEP" => Color.FromArgb(0, 229, 255),
            "ACTION" => Color.FromArgb(255, 215, 0),
            "ERR" => Color.FromArgb(255, 82, 82),
            _ => Color.FromArgb(160, 175, 195)
        };

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;

        rtbLog.SelectionColor = Color.FromArgb(120, 135, 155);
        rtbLog.AppendText($"[{time}] ");

        rtbLog.SelectionColor = dirColor;
        rtbLog.AppendText($"[{direction}] ");

        rtbLog.SelectionColor = Color.FromArgb(230, 235, 240);
        if (!string.IsNullOrEmpty(hex))
        {
            rtbLog.AppendText($"{hex,-26} | ");
        }
        rtbLog.AppendText($"{description}\r\n");

        if (rtbLog.Lines.Length > 800)
        {
            rtbLog.Select(0, rtbLog.GetFirstCharIndexFromLine(150));
            rtbLog.SelectedText = string.Empty;
        }

        if (chkAutoScroll.Checked)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }
    }

    private void btnClearLog_Click(object sender, EventArgs e)
    {
        rtbLog.Clear();
        _plcService.ResetCounters();
        statusTxRx.Text = "TX: 0 | RX: 0 | 錯誤: 0";
    }

    private void btnStartStation3_Click(object sender, EventArgs e)
    {
        if (_station3Service.IsRunning)
        {
            var res = MessageBox.Show("機電丙三流程正在運轉中，確定要手動停止嗎？", "確認停止", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _station3Service.Stop();
            }
            return;
        }

        if (!_plcService.IsConnected)
        {
            var res = MessageBox.Show("尚未連線至 PLC。是否立即啟用「模擬模式」並啟動機電丙三流程？", "啟動提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes)
            {
                chkSimulation.Checked = true;
                btnConnect_Click(sender, e);
            }
            else
            {
                return;
            }
        }

        try
        {
            _station3Service.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法啟動機電丙三：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void chkFilterLog_CheckedChanged(object sender, EventArgs e)
    {
        _plcService.FilterPollingLog = chkFilterLog.Checked;
    }

    private void Station3Service_OnStepProgress(int current, int total, string desc)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Station3Service_OnStepProgress(current, total, desc)));
            return;
        }

        statusCycleTime.Text = $"步序: [{current}/{total}] {desc}";
    }

    private void Station3Service_OnExecutionStateChanged(bool isRunning, string stateText)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Station3Service_OnExecutionStateChanged(isRunning, stateText)));
            return;
        }

        if (isRunning)
        {
            btnStartStation3.Text = "⏹ 機電丙三停止";
            btnStartStation3.BackColor = Color.FromArgb(220, 38, 38);
        }
        else
        {
            btnStartStation3.Text = "▶ 機電丙三啟動";
            btnStartStation3.BackColor = Color.FromArgb(124, 58, 237);
        }
    }

    private void Station3Service_OnPieceCountUpdated(int current, int target)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Station3Service_OnPieceCountUpdated(current, target)));
            return;
        }

        statusTxRx.Text = $"料件計數: {current} / {target} | TX: {_plcService.TxCount} | RX: {_plcService.RxCount}";
    }

    private void Station3Service_OnLogAction(string msg)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Station3Service_OnLogAction(msg)));
            return;
        }

        if (chkPauseLog.Checked) return;

        string time = DateTime.Now.ToString("HH:mm:ss.fff");

        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.SelectionLength = 0;

        rtbLog.SelectionColor = Color.FromArgb(120, 135, 155);
        rtbLog.AppendText($"[{time}] ");

        rtbLog.SelectionColor = Color.FromArgb(0, 229, 255);
        rtbLog.AppendText("[動作] ");

        rtbLog.SelectionColor = Color.FromArgb(255, 255, 255);
        rtbLog.AppendText($"{msg}\r\n");

        if (rtbLog.Lines.Length > 800)
        {
            rtbLog.Select(0, rtbLog.GetFirstCharIndexFromLine(150));
            rtbLog.SelectedText = string.Empty;
        }

        if (chkAutoScroll.Checked)
        {
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }
    }

    private void btnMarqueeTest_Click(object? sender, EventArgs e)
    {
        if (_station3Service.IsMarqueeRunning)
        {
            _station3Service.StopMarqueeTest();
            return;
        }

        if (_station3Service.IsRunning)
        {
            MessageBox.Show("機電丙三流程正在運轉中，請先停止後再執行跑馬燈測試！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_plcService.IsConnected)
        {
            var res = MessageBox.Show("尚未連線至 PLC。是否立即啟用「模擬模式」並執行跑馬燈測試？", "連線提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes)
            {
                chkSimulation.Checked = true;
                btnConnect_Click(sender ?? this, e);
            }
            else
            {
                return;
            }
        }

        try
        {
            _station3Service.StartMarqueeTest();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"無法啟動跑馬燈測試：{ex.Message}", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Station3Service_OnMarqueeStateChanged(bool isRunning)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Station3Service_OnMarqueeStateChanged(isRunning)));
            return;
        }

        if (isRunning)
        {
            btnMarqueeTest.Text = "⏹ 停止跑馬燈";
            btnMarqueeTest.BackColor = Color.FromArgb(220, 38, 38);
            if (_hmiBtnMarquee != null)
            {
                _hmiBtnMarquee.Text = "⏹ 停止跑馬燈\n(測試中...)";
                _hmiBtnMarquee.BackColor = Color.FromArgb(220, 38, 38);
            }
        }
        else
        {
            btnMarqueeTest.Text = "★ 跑馬燈測試";
            btnMarqueeTest.BackColor = Color.FromArgb(217, 119, 6);
            if (_hmiBtnMarquee != null)
            {
                _hmiBtnMarquee.Text = "★ 跑馬燈測試\n(三燈 3 循環)";
                _hmiBtnMarquee.BackColor = Color.FromArgb(217, 119, 6);
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (_station3Service.IsRunning)
        {
            _station3Service.Stop();
        }
        if (_station3Service.IsMarqueeRunning)
        {
            _station3Service.StopMarqueeTest();
        }
        _plcService.Dispose();
    }
}
