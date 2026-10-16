namespace Fx3uMonitor.Forms;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Panel pnlTop;
    private System.Windows.Forms.Label lblLogo;
    private System.Windows.Forms.Label lblPort;
    private System.Windows.Forms.ComboBox cboPorts;
    private System.Windows.Forms.Button btnRefreshPorts;
    private System.Windows.Forms.Label lblParams;
    private System.Windows.Forms.Label lblInterval;
    private System.Windows.Forms.ComboBox cboInterval;
    private System.Windows.Forms.CheckBox chkSimulation;
    private System.Windows.Forms.Button btnConnect;
    private System.Windows.Forms.Label lblConnectionStatus;
    private System.Windows.Forms.Button btnStartStation3; // 機電丙三啟動按鈕
    private System.Windows.Forms.Button btnMarqueeTest;   // 跑馬燈測試按鈕

    // 上下分割：上方為監控與控制區，下方為通訊日誌區
    private System.Windows.Forms.SplitContainer splitMainVertical;

    private System.Windows.Forms.TabControl tabMain;
    private System.Windows.Forms.TabPage tabIoCards;
    private System.Windows.Forms.TabPage tabHmi;
    private System.Windows.Forms.TabPage tabIoTable;

    // --- Tab 1: I/O 左右並列監控 ---
    private System.Windows.Forms.SplitContainer splitIo;

    // 左側：X 輸入接點
    private System.Windows.Forms.GroupBox grpInputs;
    private System.Windows.Forms.Panel pnlInputsScroll;
    private System.Windows.Forms.Label lblXBank1;
    private System.Windows.Forms.FlowLayoutPanel pnlInputsBank1;
    private System.Windows.Forms.Label lblXBank2;
    private System.Windows.Forms.FlowLayoutPanel pnlInputsBank2;

    // 右側：Y 輸出接點
    private System.Windows.Forms.GroupBox grpOutputs;
    private System.Windows.Forms.Panel pnlOutputsScroll;
    private System.Windows.Forms.Label lblYBank1;
    private System.Windows.Forms.FlowLayoutPanel pnlOutputsBank1;
    private System.Windows.Forms.Label lblYBank2;
    private System.Windows.Forms.FlowLayoutPanel pnlOutputsBank2;

    // --- Tab 2: HMI 控制面板 ---
    private System.Windows.Forms.Panel pnlHmiContent;

    // --- Tab 3: I/O 說明表格 ---
    private System.Windows.Forms.Panel pnlTableContent;
    private System.Windows.Forms.DataGridView dgvInputs;
    private System.Windows.Forms.DataGridView dgvOutputs;
    private System.Windows.Forms.DataGridView dgvControlPanel;
    private System.Windows.Forms.Label lblDgvInputs;
    private System.Windows.Forms.Label lblDgvOutputs;
    private System.Windows.Forms.Label lblDgvCp;

    // --- 下方：通訊封包即時日誌 ---
    private System.Windows.Forms.GroupBox grpLog;
    private System.Windows.Forms.RichTextBox rtbLog;
    private System.Windows.Forms.Panel pnlLogTools;
    private System.Windows.Forms.Button btnClearLog;
    private System.Windows.Forms.CheckBox chkAutoScroll;
    private System.Windows.Forms.CheckBox chkPauseLog;
    private System.Windows.Forms.CheckBox chkFilterLog;

    // --- 底部狀態列 ---
    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel statusConnection;
    private System.Windows.Forms.ToolStripStatusLabel statusCycleTime;
    private System.Windows.Forms.ToolStripStatusLabel statusTxRx;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        pnlTop = new System.Windows.Forms.Panel();
        lblLogo = new System.Windows.Forms.Label();
        lblPort = new System.Windows.Forms.Label();
        cboPorts = new System.Windows.Forms.ComboBox();
        btnRefreshPorts = new System.Windows.Forms.Button();
        lblParams = new System.Windows.Forms.Label();
        lblInterval = new System.Windows.Forms.Label();
        cboInterval = new System.Windows.Forms.ComboBox();
        chkSimulation = new System.Windows.Forms.CheckBox();
        btnConnect = new System.Windows.Forms.Button();
        lblConnectionStatus = new System.Windows.Forms.Label();
        btnStartStation3 = new System.Windows.Forms.Button();
        btnMarqueeTest = new System.Windows.Forms.Button();

        splitMainVertical = new System.Windows.Forms.SplitContainer();

        tabMain = new System.Windows.Forms.TabControl();
        tabIoCards = new System.Windows.Forms.TabPage();
        tabHmi = new System.Windows.Forms.TabPage();
        tabIoTable = new System.Windows.Forms.TabPage();

        splitIo = new System.Windows.Forms.SplitContainer();

        grpInputs = new System.Windows.Forms.GroupBox();
        pnlInputsScroll = new System.Windows.Forms.Panel();
        lblXBank1 = new System.Windows.Forms.Label();
        pnlInputsBank1 = new System.Windows.Forms.FlowLayoutPanel();
        lblXBank2 = new System.Windows.Forms.Label();
        pnlInputsBank2 = new System.Windows.Forms.FlowLayoutPanel();

        grpOutputs = new System.Windows.Forms.GroupBox();
        pnlOutputsScroll = new System.Windows.Forms.Panel();
        lblYBank1 = new System.Windows.Forms.Label();
        pnlOutputsBank1 = new System.Windows.Forms.FlowLayoutPanel();
        lblYBank2 = new System.Windows.Forms.Label();
        pnlOutputsBank2 = new System.Windows.Forms.FlowLayoutPanel();

        pnlHmiContent = new System.Windows.Forms.Panel();

        pnlTableContent = new System.Windows.Forms.Panel();
        lblDgvCp = new System.Windows.Forms.Label();
        dgvControlPanel = new System.Windows.Forms.DataGridView();
        lblDgvInputs = new System.Windows.Forms.Label();
        dgvInputs = new System.Windows.Forms.DataGridView();
        lblDgvOutputs = new System.Windows.Forms.Label();
        dgvOutputs = new System.Windows.Forms.DataGridView();

        grpLog = new System.Windows.Forms.GroupBox();
        rtbLog = new System.Windows.Forms.RichTextBox();
        pnlLogTools = new System.Windows.Forms.Panel();
        btnClearLog = new System.Windows.Forms.Button();
        chkAutoScroll = new System.Windows.Forms.CheckBox();
        chkPauseLog = new System.Windows.Forms.CheckBox();
        chkFilterLog = new System.Windows.Forms.CheckBox();

        statusStrip = new System.Windows.Forms.StatusStrip();
        statusConnection = new System.Windows.Forms.ToolStripStatusLabel();
        statusCycleTime = new System.Windows.Forms.ToolStripStatusLabel();
        statusTxRx = new System.Windows.Forms.ToolStripStatusLabel();

        pnlTop.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitMainVertical).BeginInit();
        splitMainVertical.Panel1.SuspendLayout();
        splitMainVertical.Panel2.SuspendLayout();
        splitMainVertical.SuspendLayout();

        tabMain.SuspendLayout();
        tabIoCards.SuspendLayout();
        tabHmi.SuspendLayout();
        tabIoTable.SuspendLayout();

        ((System.ComponentModel.ISupportInitialize)splitIo).BeginInit();
        splitIo.Panel1.SuspendLayout();
        splitIo.Panel2.SuspendLayout();
        splitIo.SuspendLayout();

        grpInputs.SuspendLayout();
        pnlInputsScroll.SuspendLayout();

        grpOutputs.SuspendLayout();
        pnlOutputsScroll.SuspendLayout();

        pnlTableContent.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvControlPanel).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvInputs).BeginInit();
        ((System.ComponentModel.ISupportInitialize)dgvOutputs).BeginInit();

        grpLog.SuspendLayout();
        pnlLogTools.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        // 
        // pnlTop
        // 
        pnlTop.BackColor = System.Drawing.Color.FromArgb(20, 24, 32);
        pnlTop.Controls.Add(lblLogo);
        pnlTop.Controls.Add(lblPort);
        pnlTop.Controls.Add(cboPorts);
        pnlTop.Controls.Add(btnRefreshPorts);
        pnlTop.Controls.Add(lblParams);
        pnlTop.Controls.Add(lblInterval);
        pnlTop.Controls.Add(cboInterval);
        pnlTop.Controls.Add(chkSimulation);
        pnlTop.Controls.Add(btnConnect);
        pnlTop.Controls.Add(lblConnectionStatus);
        pnlTop.Controls.Add(btnStartStation3);
        pnlTop.Controls.Add(btnMarqueeTest);
        pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
        pnlTop.Location = new System.Drawing.Point(0, 0);
        pnlTop.Name = "pnlTop";
        pnlTop.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
        pnlTop.Size = new System.Drawing.Size(1420, 68);
        pnlTop.TabIndex = 0;

        // 
        // lblLogo
        // 
        lblLogo.AutoSize = true;
        lblLogo.Font = new System.Drawing.Font("Segoe UI", 12.5F, System.Drawing.FontStyle.Bold);
        lblLogo.ForeColor = System.Drawing.Color.FromArgb(0, 210, 255);
        lblLogo.Location = new System.Drawing.Point(12, 14);
        lblLogo.Name = "lblLogo";
        lblLogo.Size = new System.Drawing.Size(220, 23);
        lblLogo.TabIndex = 0;
        lblLogo.Text = "FX3U PLC 序列埠監控系統";

        // 
        // lblPort (調整位置避免遮擋右側 ComboBox)
        // 
        lblPort.AutoSize = true;
        lblPort.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
        lblPort.ForeColor = System.Drawing.Color.FromArgb(180, 190, 205);
        lblPort.Location = new System.Drawing.Point(248, 17);
        lblPort.Name = "lblPort";
        lblPort.Size = new System.Drawing.Size(83, 15);
        lblPort.TabIndex = 1;
        lblPort.Text = "序列埠(Port):";

        // 
        // cboPorts (設定於 340，保有舒適間距不重疊)
        // 
        cboPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        cboPorts.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        cboPorts.Location = new System.Drawing.Point(340, 13);
        cboPorts.Name = "cboPorts";
        cboPorts.Size = new System.Drawing.Size(100, 25);
        cboPorts.TabIndex = 2;

        // 
        // btnRefreshPorts
        // 
        btnRefreshPorts.BackColor = System.Drawing.Color.FromArgb(48, 56, 70);
        btnRefreshPorts.FlatAppearance.BorderSize = 0;
        btnRefreshPorts.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnRefreshPorts.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        btnRefreshPorts.ForeColor = System.Drawing.Color.White;
        btnRefreshPorts.Location = new System.Drawing.Point(448, 13);
        btnRefreshPorts.Name = "btnRefreshPorts";
        btnRefreshPorts.Size = new System.Drawing.Size(46, 25);
        btnRefreshPorts.TabIndex = 3;
        btnRefreshPorts.Text = "重整";
        btnRefreshPorts.UseVisualStyleBackColor = false;
        btnRefreshPorts.Click += btnRefreshPorts_Click;

        // 
        // lblParams
        // 
        lblParams.AutoSize = true;
        lblParams.Font = new System.Drawing.Font("Segoe UI", 8.25F);
        lblParams.ForeColor = System.Drawing.Color.FromArgb(125, 140, 160);
        lblParams.Location = new System.Drawing.Point(248, 43);
        lblParams.Name = "lblParams";
        lblParams.Size = new System.Drawing.Size(210, 13);
        lblParams.TabIndex = 4;
        lblParams.Text = "規格: 9600 bps | 7 DataBits | Even | 1 StopBit";

        // 
        // lblInterval
        // 
        lblInterval.AutoSize = true;
        lblInterval.Font = new System.Drawing.Font("Segoe UI", 9F);
        lblInterval.ForeColor = System.Drawing.Color.FromArgb(180, 190, 205);
        lblInterval.Location = new System.Drawing.Point(514, 17);
        lblInterval.Name = "lblInterval";
        lblInterval.Size = new System.Drawing.Size(58, 15);
        lblInterval.TabIndex = 5;
        lblInterval.Text = "輪詢週期:";

        // 
        // cboInterval
        // 
        cboInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        cboInterval.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        cboInterval.Items.AddRange(new object[] { "100 ms", "200 ms", "500 ms", "1000 ms" });
        cboInterval.Location = new System.Drawing.Point(578, 13);
        cboInterval.Name = "cboInterval";
        cboInterval.Size = new System.Drawing.Size(86, 25);
        cboInterval.TabIndex = 6;
        cboInterval.SelectedIndexChanged += cboInterval_SelectedIndexChanged;

        // 
        // chkSimulation
        // 
        chkSimulation.AutoSize = true;
        chkSimulation.Font = new System.Drawing.Font("Segoe UI", 9F);
        chkSimulation.ForeColor = System.Drawing.Color.FromArgb(200, 175, 255);
        chkSimulation.Location = new System.Drawing.Point(685, 16);
        chkSimulation.Name = "chkSimulation";
        chkSimulation.Size = new System.Drawing.Size(161, 19);
        chkSimulation.TabIndex = 7;
        chkSimulation.Text = "模擬模式 (免實體 PLC)";
        chkSimulation.UseVisualStyleBackColor = true;

        // 
        // btnConnect
        // 
        btnConnect.BackColor = System.Drawing.Color.FromArgb(16, 120, 65);
        btnConnect.FlatAppearance.BorderSize = 0;
        btnConnect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnConnect.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        btnConnect.ForeColor = System.Drawing.Color.White;
        btnConnect.Location = new System.Drawing.Point(865, 11);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new System.Drawing.Size(185, 34);
        btnConnect.TabIndex = 8;
        btnConnect.Text = "開始連線 (Connect)";
        btnConnect.UseVisualStyleBackColor = false;
        btnConnect.Click += btnConnect_Click;

        // 
        // lblConnectionStatus
        // 
        lblConnectionStatus.AutoSize = true;
        lblConnectionStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        lblConnectionStatus.ForeColor = System.Drawing.Color.FromArgb(160, 170, 185);
        lblConnectionStatus.Location = new System.Drawing.Point(1058, 24);
        lblConnectionStatus.Name = "lblConnectionStatus";
        lblConnectionStatus.Size = new System.Drawing.Size(61, 15);
        lblConnectionStatus.TabIndex = 9;
        lblConnectionStatus.Text = "● 未連線";

        // 
        // btnStartStation3 (右移至 X=1200，避免連線狀態文字遮擋)
        // 
        btnStartStation3.BackColor = System.Drawing.Color.FromArgb(124, 58, 237);
        btnStartStation3.FlatAppearance.BorderSize = 0;
        btnStartStation3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnStartStation3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        btnStartStation3.ForeColor = System.Drawing.Color.White;
        btnStartStation3.Location = new System.Drawing.Point(1200, 6);
        btnStartStation3.Name = "btnStartStation3";
        btnStartStation3.Size = new System.Drawing.Size(170, 26);
        btnStartStation3.TabIndex = 10;
        btnStartStation3.Text = "▶ 機電丙三啟動";
        btnStartStation3.UseVisualStyleBackColor = false;
        btnStartStation3.Click += btnStartStation3_Click;

        // 
        // btnMarqueeTest (移至機電丙三啟動按鈕下方)
        // 
        btnMarqueeTest.BackColor = System.Drawing.Color.FromArgb(217, 119, 6);
        btnMarqueeTest.FlatAppearance.BorderSize = 0;
        btnMarqueeTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnMarqueeTest.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        btnMarqueeTest.ForeColor = System.Drawing.Color.White;
        btnMarqueeTest.Location = new System.Drawing.Point(1200, 35);
        btnMarqueeTest.Name = "btnMarqueeTest";
        btnMarqueeTest.Size = new System.Drawing.Size(170, 26);
        btnMarqueeTest.TabIndex = 11;
        btnMarqueeTest.Text = "★ 跑馬燈測試";
        btnMarqueeTest.UseVisualStyleBackColor = false;
        btnMarqueeTest.Click += btnMarqueeTest_Click;

        // 
        // splitMainVertical (上下分割配置)
        // 
        splitMainVertical.Dock = System.Windows.Forms.DockStyle.Fill;
        splitMainVertical.Location = new System.Drawing.Point(0, 68);
        splitMainVertical.Name = "splitMainVertical";
        splitMainVertical.Orientation = System.Windows.Forms.Orientation.Horizontal;

        // 
        // splitMainVertical.Panel1 (上方：I/O 監控與控制區)
        // 
        splitMainVertical.Panel1.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
        splitMainVertical.Panel1.Controls.Add(tabMain);
        splitMainVertical.Panel1.Padding = new System.Windows.Forms.Padding(6);

        // 
        // splitMainVertical.Panel2 (下方：通訊封包即時日誌區)
        // 
        splitMainVertical.Panel2.BackColor = System.Drawing.Color.FromArgb(20, 24, 32);
        splitMainVertical.Panel2.Controls.Add(grpLog);
        splitMainVertical.Panel2.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);

        splitMainVertical.Size = new System.Drawing.Size(1420, 754);
        splitMainVertical.SplitterDistance = 530;
        splitMainVertical.TabIndex = 1;

        // 
        // tabMain
        // 
        tabMain.Controls.Add(tabIoCards);
        tabMain.Controls.Add(tabHmi);
        tabMain.Controls.Add(tabIoTable);
        tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
        tabMain.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        tabMain.ItemSize = new System.Drawing.Size(260, 32);
        tabMain.Location = new System.Drawing.Point(6, 6);
        tabMain.Name = "tabMain";
        tabMain.SelectedIndex = 0;
        tabMain.Size = new System.Drawing.Size(1408, 518);
        tabMain.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
        tabMain.TabIndex = 0;

        // 
        // tabIoCards
        // 
        tabIoCards.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
        tabIoCards.Controls.Add(splitIo);
        tabIoCards.Location = new System.Drawing.Point(4, 36);
        tabIoCards.Name = "tabIoCards";
        tabIoCards.Padding = new System.Windows.Forms.Padding(4);
        tabIoCards.Size = new System.Drawing.Size(1400, 478);
        tabIoCards.TabIndex = 0;
        tabIoCards.Text = "● I/O 接點即時監控 (左右並列)";

        // 
        // splitIo (左右並列分割)
        // 
        splitIo.Dock = System.Windows.Forms.DockStyle.Fill;
        splitIo.Location = new System.Drawing.Point(4, 4);
        splitIo.Name = "splitIo";
        splitIo.Orientation = System.Windows.Forms.Orientation.Vertical;

        // 
        // splitIo.Panel1 (左側：輸入 X 接點)
        // 
        splitIo.Panel1.Controls.Add(grpInputs);
        splitIo.Panel1.Padding = new System.Windows.Forms.Padding(2);

        // 
        // splitIo.Panel2 (右側：輸出 Y 接點)
        // 
        splitIo.Panel2.Controls.Add(grpOutputs);
        splitIo.Panel2.Padding = new System.Windows.Forms.Padding(2);

        splitIo.Size = new System.Drawing.Size(1392, 470);
        splitIo.SplitterDistance = 675;
        splitIo.TabIndex = 0;

        // 
        // grpInputs (左側 X 接點面板)
        // 
        grpInputs.Controls.Add(pnlInputsScroll);
        grpInputs.Dock = System.Windows.Forms.DockStyle.Fill;
        grpInputs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        grpInputs.ForeColor = System.Drawing.Color.FromArgb(0, 230, 118);
        grpInputs.Location = new System.Drawing.Point(2, 2);
        grpInputs.Name = "grpInputs";
        grpInputs.Padding = new System.Windows.Forms.Padding(6);
        grpInputs.Size = new System.Drawing.Size(671, 466);
        grpInputs.TabIndex = 0;
        grpInputs.TabStop = false;
        grpInputs.Text = "X0 ~ X17 輸入接點即時監控 (綠色/紅色 LED)";

        // 
        // pnlInputsScroll (支援自動捲軸)
        // 
        pnlInputsScroll.AutoScroll = true;
        pnlInputsScroll.Controls.Add(pnlInputsBank2);
        pnlInputsScroll.Controls.Add(lblXBank2);
        pnlInputsScroll.Controls.Add(pnlInputsBank1);
        pnlInputsScroll.Controls.Add(lblXBank1);
        pnlInputsScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        pnlInputsScroll.Location = new System.Drawing.Point(6, 24);
        pnlInputsScroll.Name = "pnlInputsScroll";
        pnlInputsScroll.Padding = new System.Windows.Forms.Padding(4);
        pnlInputsScroll.Size = new System.Drawing.Size(659, 436);
        pnlInputsScroll.TabIndex = 0;

        // 
        // lblXBank1
        // 
        lblXBank1.AutoSize = true;
        lblXBank1.Dock = System.Windows.Forms.DockStyle.Top;
        lblXBank1.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
        lblXBank1.ForeColor = System.Drawing.Color.FromArgb(170, 185, 205);
        lblXBank1.Location = new System.Drawing.Point(4, 4);
        lblXBank1.Name = "lblXBank1";
        lblXBank1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        lblXBank1.Size = new System.Drawing.Size(161, 23);
        lblXBank1.TabIndex = 0;
        lblXBank1.Text = "機構位置感測器群 (X0 ~ X7):";

        // 
        // pnlInputsBank1
        // 
        pnlInputsBank1.AutoSize = true;
        pnlInputsBank1.Dock = System.Windows.Forms.DockStyle.Top;
        pnlInputsBank1.Location = new System.Drawing.Point(4, 27);
        pnlInputsBank1.Name = "pnlInputsBank1";
        pnlInputsBank1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
        pnlInputsBank1.Size = new System.Drawing.Size(651, 60);
        pnlInputsBank1.TabIndex = 1;

        // 
        // lblXBank2
        // 
        lblXBank2.AutoSize = true;
        lblXBank2.Dock = System.Windows.Forms.DockStyle.Top;
        lblXBank2.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
        lblXBank2.ForeColor = System.Drawing.Color.FromArgb(170, 185, 205);
        lblXBank2.Location = new System.Drawing.Point(4, 87);
        lblXBank2.Name = "lblXBank2";
        lblXBank2.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
        lblXBank2.Size = new System.Drawing.Size(201, 27);
        lblXBank2.TabIndex = 2;
        lblXBank2.Text = "感測器與控制面板開關 (X10 ~ X17):";

        // 
        // pnlInputsBank2
        // 
        pnlInputsBank2.AutoSize = true;
        pnlInputsBank2.Dock = System.Windows.Forms.DockStyle.Top;
        pnlInputsBank2.Location = new System.Drawing.Point(4, 114);
        pnlInputsBank2.Name = "pnlInputsBank2";
        pnlInputsBank2.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
        pnlInputsBank2.Size = new System.Drawing.Size(651, 60);
        pnlInputsBank2.TabIndex = 3;

        // 
        // grpOutputs (右側 Y 接點面板)
        // 
        grpOutputs.Controls.Add(pnlOutputsScroll);
        grpOutputs.Dock = System.Windows.Forms.DockStyle.Fill;
        grpOutputs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        grpOutputs.ForeColor = System.Drawing.Color.FromArgb(255, 145, 0);
        grpOutputs.Location = new System.Drawing.Point(2, 2);
        grpOutputs.Name = "grpOutputs";
        grpOutputs.Padding = new System.Windows.Forms.Padding(6);
        grpOutputs.Size = new System.Drawing.Size(709, 466);
        grpOutputs.TabIndex = 1;
        grpOutputs.TabStop = false;
        grpOutputs.Text = "Y0 ~ Y17 輸出接點即時監控與強制控制 (橘/紅/黃/綠 LED)";

        // 
        // pnlOutputsScroll (支援自動捲軸)
        // 
        pnlOutputsScroll.AutoScroll = true;
        pnlOutputsScroll.Controls.Add(pnlOutputsBank2);
        pnlOutputsScroll.Controls.Add(lblYBank2);
        pnlOutputsScroll.Controls.Add(pnlOutputsBank1);
        pnlOutputsScroll.Controls.Add(lblYBank1);
        pnlOutputsScroll.Dock = System.Windows.Forms.DockStyle.Fill;
        pnlOutputsScroll.Location = new System.Drawing.Point(6, 24);
        pnlOutputsScroll.Name = "pnlOutputsScroll";
        pnlOutputsScroll.Padding = new System.Windows.Forms.Padding(4);
        pnlOutputsScroll.Size = new System.Drawing.Size(697, 436);
        pnlOutputsScroll.TabIndex = 0;

        // 
        // lblYBank1
        // 
        lblYBank1.AutoSize = true;
        lblYBank1.Dock = System.Windows.Forms.DockStyle.Top;
        lblYBank1.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
        lblYBank1.ForeColor = System.Drawing.Color.FromArgb(170, 185, 205);
        lblYBank1.Location = new System.Drawing.Point(4, 4);
        lblYBank1.Name = "lblYBank1";
        lblYBank1.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
        lblYBank1.Size = new System.Drawing.Size(149, 23);
        lblYBank1.TabIndex = 0;
        lblYBank1.Text = "氣缸與致動器群 (Y0 ~ Y7):";

        // 
        // pnlOutputsBank1
        // 
        pnlOutputsBank1.AutoSize = true;
        pnlOutputsBank1.Dock = System.Windows.Forms.DockStyle.Top;
        pnlOutputsBank1.Location = new System.Drawing.Point(4, 27);
        pnlOutputsBank1.Name = "pnlOutputsBank1";
        pnlOutputsBank1.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
        pnlOutputsBank1.Size = new System.Drawing.Size(689, 60);
        pnlOutputsBank1.TabIndex = 1;

        // 
        // lblYBank2
        // 
        lblYBank2.AutoSize = true;
        lblYBank2.Dock = System.Windows.Forms.DockStyle.Top;
        lblYBank2.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
        lblYBank2.ForeColor = System.Drawing.Color.FromArgb(170, 185, 205);
        lblYBank2.Location = new System.Drawing.Point(4, 87);
        lblYBank2.Name = "lblYBank2";
        lblYBank2.Padding = new System.Windows.Forms.Padding(0, 8, 0, 4);
        lblYBank2.Size = new System.Drawing.Size(189, 27);
        lblYBank2.TabIndex = 2;
        lblYBank2.Text = "備用點與面板指示燈 (Y10 ~ Y17):";

        // 
        // pnlOutputsBank2
        // 
        pnlOutputsBank2.AutoSize = true;
        pnlOutputsBank2.Dock = System.Windows.Forms.DockStyle.Top;
        pnlOutputsBank2.Location = new System.Drawing.Point(4, 114);
        pnlOutputsBank2.Name = "pnlOutputsBank2";
        pnlOutputsBank2.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
        pnlOutputsBank2.Size = new System.Drawing.Size(689, 60);
        pnlOutputsBank2.TabIndex = 3;

        // 
        // tabHmi
        // 
        tabHmi.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
        tabHmi.Controls.Add(pnlHmiContent);
        tabHmi.Location = new System.Drawing.Point(4, 36);
        tabHmi.Name = "tabHmi";
        tabHmi.Padding = new System.Windows.Forms.Padding(6);
        tabHmi.Size = new System.Drawing.Size(1400, 478);
        tabHmi.TabIndex = 1;
        tabHmi.Text = "★ 機台控制面板總覽 (HMI)";

        // 
        // pnlHmiContent
        // 
        pnlHmiContent.AutoScroll = true;
        pnlHmiContent.BackColor = System.Drawing.Color.FromArgb(28, 33, 43);
        pnlHmiContent.Dock = System.Windows.Forms.DockStyle.Fill;
        pnlHmiContent.Location = new System.Drawing.Point(6, 6);
        pnlHmiContent.Name = "pnlHmiContent";
        pnlHmiContent.Padding = new System.Windows.Forms.Padding(16);
        pnlHmiContent.Size = new System.Drawing.Size(1388, 466);
        pnlHmiContent.TabIndex = 0;

        // 
        // tabIoTable
        // 
        tabIoTable.BackColor = System.Drawing.Color.FromArgb(24, 28, 36);
        tabIoTable.Controls.Add(pnlTableContent);
        tabIoTable.Location = new System.Drawing.Point(4, 36);
        tabIoTable.Name = "tabIoTable";
        tabIoTable.Padding = new System.Windows.Forms.Padding(8);
        tabIoTable.Size = new System.Drawing.Size(1400, 478);
        tabIoTable.TabIndex = 2;
        tabIoTable.Text = "📋 I/O 配置與控制面板說明表";

        // 
        // pnlTableContent
        // 
        pnlTableContent.AutoScroll = true;
        pnlTableContent.Controls.Add(dgvControlPanel);
        pnlTableContent.Controls.Add(lblDgvCp);
        pnlTableContent.Controls.Add(dgvInputs);
        pnlTableContent.Controls.Add(lblDgvInputs);
        pnlTableContent.Controls.Add(dgvOutputs);
        pnlTableContent.Controls.Add(lblDgvOutputs);
        pnlTableContent.Dock = System.Windows.Forms.DockStyle.Fill;
        pnlTableContent.Location = new System.Drawing.Point(8, 8);
        pnlTableContent.Name = "pnlTableContent";
        pnlTableContent.Size = new System.Drawing.Size(1384, 462);
        pnlTableContent.TabIndex = 0;

        // 
        // lblDgvCp
        // 
        lblDgvCp.AutoSize = true;
        lblDgvCp.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        lblDgvCp.ForeColor = System.Drawing.Color.FromArgb(255, 215, 0);
        lblDgvCp.Location = new System.Drawing.Point(6, 8);
        lblDgvCp.Name = "lblDgvCp";
        lblDgvCp.Size = new System.Drawing.Size(121, 19);
        lblDgvCp.TabIndex = 0;
        lblDgvCp.Text = "【控制面板說明】";

        // 
        // dgvControlPanel
        // 
        dgvControlPanel.AllowUserToAddRows = false;
        dgvControlPanel.AllowUserToDeleteRows = false;
        dgvControlPanel.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        dgvControlPanel.BackgroundColor = System.Drawing.Color.FromArgb(32, 38, 48);
        dgvControlPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;
        dgvControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
        dgvControlPanel.Location = new System.Drawing.Point(0, 30);
        dgvControlPanel.Name = "dgvControlPanel";
        dgvControlPanel.ReadOnly = true;
        dgvControlPanel.RowHeadersVisible = false;
        dgvControlPanel.Size = new System.Drawing.Size(1384, 150);
        dgvControlPanel.TabIndex = 1;

        // 
        // lblDgvInputs
        // 
        lblDgvInputs.AutoSize = true;
        lblDgvInputs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        lblDgvInputs.ForeColor = System.Drawing.Color.FromArgb(0, 230, 118);
        lblDgvInputs.Location = new System.Drawing.Point(6, 190);
        lblDgvInputs.Name = "lblDgvInputs";
        lblDgvInputs.Size = new System.Drawing.Size(155, 19);
        lblDgvInputs.TabIndex = 2;
        lblDgvInputs.Text = "【輸入 I/O 配置表 (X)】";

        // 
        // dgvInputs
        // 
        dgvInputs.AllowUserToAddRows = false;
        dgvInputs.AllowUserToDeleteRows = false;
        dgvInputs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        dgvInputs.BackgroundColor = System.Drawing.Color.FromArgb(32, 38, 48);
        dgvInputs.BorderStyle = System.Windows.Forms.BorderStyle.None;
        dgvInputs.Dock = System.Windows.Forms.DockStyle.Top;
        dgvInputs.Location = new System.Drawing.Point(0, 215);
        dgvInputs.Name = "dgvInputs";
        dgvInputs.ReadOnly = true;
        dgvInputs.RowHeadersVisible = false;
        dgvInputs.Size = new System.Drawing.Size(1384, 180);
        dgvInputs.TabIndex = 3;

        // 
        // lblDgvOutputs
        // 
        lblDgvOutputs.AutoSize = true;
        lblDgvOutputs.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        lblDgvOutputs.ForeColor = System.Drawing.Color.FromArgb(255, 145, 0);
        lblDgvOutputs.Location = new System.Drawing.Point(6, 405);
        lblDgvOutputs.Name = "lblDgvOutputs";
        lblDgvOutputs.Size = new System.Drawing.Size(155, 19);
        lblDgvOutputs.TabIndex = 4;
        lblDgvOutputs.Text = "【輸出 I/O 配置表 (Y)】";

        // 
        // dgvOutputs
        // 
        dgvOutputs.AllowUserToAddRows = false;
        dgvOutputs.AllowUserToDeleteRows = false;
        dgvOutputs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        dgvOutputs.BackgroundColor = System.Drawing.Color.FromArgb(32, 38, 48);
        dgvOutputs.BorderStyle = System.Windows.Forms.BorderStyle.None;
        dgvOutputs.Dock = System.Windows.Forms.DockStyle.Top;
        dgvOutputs.Location = new System.Drawing.Point(0, 430);
        dgvOutputs.Name = "dgvOutputs";
        dgvOutputs.ReadOnly = true;
        dgvOutputs.RowHeadersVisible = false;
        dgvOutputs.Size = new System.Drawing.Size(1384, 180);
        dgvOutputs.TabIndex = 5;

        // 
        // grpLog (移至下方，全寬顯示)
        // 
        grpLog.Controls.Add(rtbLog);
        grpLog.Controls.Add(pnlLogTools);
        grpLog.Dock = System.Windows.Forms.DockStyle.Fill;
        grpLog.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        grpLog.ForeColor = System.Drawing.Color.FromArgb(70, 160, 245);
        grpLog.Location = new System.Drawing.Point(8, 4);
        grpLog.Name = "grpLog";
        grpLog.Padding = new System.Windows.Forms.Padding(6);
        grpLog.Size = new System.Drawing.Size(1404, 212);
        grpLog.TabIndex = 0;
        grpLog.TabStop = false;
        grpLog.Text = "通訊封包即時日誌 (TX / RX Communication Log)";

        // 
        // rtbLog
        // 
        rtbLog.BackColor = System.Drawing.Color.FromArgb(16, 19, 24);
        rtbLog.BorderStyle = System.Windows.Forms.BorderStyle.None;
        rtbLog.Dock = System.Windows.Forms.DockStyle.Fill;
        rtbLog.Font = new System.Drawing.Font("Cascadia Mono", 9.5F);
        rtbLog.ForeColor = System.Drawing.Color.FromArgb(230, 235, 240);
        rtbLog.Location = new System.Drawing.Point(6, 24);
        rtbLog.Name = "rtbLog";
        rtbLog.ReadOnly = true;
        rtbLog.Size = new System.Drawing.Size(1392, 150);
        rtbLog.TabIndex = 0;
        rtbLog.Text = "";

        // 
        // pnlLogTools
        // 
        pnlLogTools.BackColor = System.Drawing.Color.FromArgb(20, 24, 30);
        pnlLogTools.Controls.Add(btnClearLog);
        pnlLogTools.Controls.Add(chkAutoScroll);
        pnlLogTools.Controls.Add(chkPauseLog);
        pnlLogTools.Controls.Add(chkFilterLog);
        pnlLogTools.Dock = System.Windows.Forms.DockStyle.Bottom;
        pnlLogTools.Location = new System.Drawing.Point(6, 174);
        pnlLogTools.Name = "pnlLogTools";
        pnlLogTools.Size = new System.Drawing.Size(1392, 32);
        pnlLogTools.TabIndex = 1;

        // 
        // btnClearLog
        // 
        btnClearLog.BackColor = System.Drawing.Color.FromArgb(48, 56, 70);
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        btnClearLog.Font = new System.Drawing.Font("Segoe UI", 8F);
        btnClearLog.ForeColor = System.Drawing.Color.White;
        btnClearLog.Location = new System.Drawing.Point(6, 4);
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new System.Drawing.Size(75, 24);
        btnClearLog.TabIndex = 0;
        btnClearLog.Text = "清除日誌";
        btnClearLog.UseVisualStyleBackColor = false;
        btnClearLog.Click += btnClearLog_Click;

        // 
        // chkAutoScroll
        // 
        chkAutoScroll.AutoSize = true;
        chkAutoScroll.Checked = true;
        chkAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
        chkAutoScroll.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        chkAutoScroll.ForeColor = System.Drawing.Color.FromArgb(200, 210, 225);
        chkAutoScroll.Location = new System.Drawing.Point(95, 7);
        chkAutoScroll.Name = "chkAutoScroll";
        chkAutoScroll.Size = new System.Drawing.Size(74, 19);
        chkAutoScroll.TabIndex = 1;
        chkAutoScroll.Text = "自動捲動";
        chkAutoScroll.UseVisualStyleBackColor = true;

        // 
        // chkPauseLog
        // 
        chkPauseLog.AutoSize = true;
        chkPauseLog.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        chkPauseLog.ForeColor = System.Drawing.Color.FromArgb(200, 210, 225);
        chkPauseLog.Location = new System.Drawing.Point(185, 7);
        chkPauseLog.Name = "chkPauseLog";
        chkPauseLog.Size = new System.Drawing.Size(74, 19);
        chkPauseLog.TabIndex = 2;
        chkPauseLog.Text = "暫停更新";
        chkPauseLog.UseVisualStyleBackColor = true;

        // 
        // chkFilterLog
        // 
        chkFilterLog.AutoSize = true;
        chkFilterLog.Checked = true;
        chkFilterLog.CheckState = System.Windows.Forms.CheckState.Checked;
        chkFilterLog.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        chkFilterLog.ForeColor = System.Drawing.Color.FromArgb(0, 210, 255);
        chkFilterLog.Location = new System.Drawing.Point(275, 7);
        chkFilterLog.Name = "chkFilterLog";
        chkFilterLog.Size = new System.Drawing.Size(160, 19);
        chkFilterLog.TabIndex = 3;
        chkFilterLog.Text = "僅顯示動作步驟 (精簡)";
        chkFilterLog.UseVisualStyleBackColor = true;
        chkFilterLog.CheckedChanged += chkFilterLog_CheckedChanged;

        // 
        // statusStrip
        // 
        statusStrip.BackColor = System.Drawing.Color.FromArgb(18, 22, 28);
        statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { statusConnection, statusCycleTime, statusTxRx });
        statusStrip.Location = new System.Drawing.Point(0, 822);
        statusStrip.Name = "statusStrip";
        statusStrip.Size = new System.Drawing.Size(1420, 24);
        statusStrip.TabIndex = 2;

        // 
        // statusConnection
        // 
        statusConnection.ForeColor = System.Drawing.Color.FromArgb(200, 210, 225);
        statusConnection.Name = "statusConnection";
        statusConnection.Size = new System.Drawing.Size(100, 19);
        statusConnection.Text = "連線狀態: 未連線";

        // 
        // statusCycleTime
        // 
        statusCycleTime.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
        statusCycleTime.ForeColor = System.Drawing.Color.FromArgb(200, 210, 225);
        statusCycleTime.Name = "statusCycleTime";
        statusCycleTime.Size = new System.Drawing.Size(95, 19);
        statusCycleTime.Text = "輪詢延遲: -- ms";

        // 
        // statusTxRx
        // 
        statusTxRx.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
        statusTxRx.ForeColor = System.Drawing.Color.FromArgb(200, 210, 225);
        statusTxRx.Name = "statusTxRx";
        statusTxRx.Size = new System.Drawing.Size(140, 19);
        statusTxRx.Text = "TX: 0 | RX: 0 | 錯誤: 0";

        // 
        // MainForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        BackColor = System.Drawing.Color.FromArgb(20, 24, 32);
        ClientSize = new System.Drawing.Size(1420, 846);
        Controls.Add(splitMainVertical);
        Controls.Add(pnlTop);
        Controls.Add(statusStrip);
        MinimumSize = new System.Drawing.Size(1100, 700);
        Name = "MainForm";
        StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        Text = "三菱 FX3U PLC 序列埠監控與控制系統 (Mitsubishi FX3U Monitor) - 機電整合 I/O 配置版";
        pnlTop.ResumeLayout(false);
        pnlTop.PerformLayout();
        splitMainVertical.Panel1.ResumeLayout(false);
        splitMainVertical.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitMainVertical).EndInit();
        splitMainVertical.ResumeLayout(false);
        tabMain.ResumeLayout(false);
        tabIoCards.ResumeLayout(false);
        tabHmi.ResumeLayout(false);
        tabIoTable.ResumeLayout(false);
        splitIo.Panel1.ResumeLayout(false);
        splitIo.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitIo).EndInit();
        splitIo.ResumeLayout(false);
        grpInputs.ResumeLayout(false);
        pnlInputsScroll.ResumeLayout(false);
        pnlInputsScroll.PerformLayout();
        grpOutputs.ResumeLayout(false);
        pnlOutputsScroll.ResumeLayout(false);
        pnlOutputsScroll.PerformLayout();
        pnlTableContent.ResumeLayout(false);
        pnlTableContent.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dgvControlPanel).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvInputs).EndInit();
        ((System.ComponentModel.ISupportInitialize)dgvOutputs).EndInit();
        grpLog.ResumeLayout(false);
        pnlLogTools.ResumeLayout(false);
        pnlLogTools.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
