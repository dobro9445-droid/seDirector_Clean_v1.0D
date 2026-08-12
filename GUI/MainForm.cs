using System.Drawing;
using System.Windows.Forms;
using seDirector.Core;
using seDirector.Models;

namespace seDirector.GUI;

public sealed class MainForm : Form
{
    private readonly ServerManager _manager;
    private readonly BackupService _backupService;
    private readonly RconService _rconService;
    private readonly SteamCmdService _steamCmdService;
    private readonly SoftStopService _softStopService;
    private readonly UpdateService _updateService;
    private readonly Logger _logger;
    private readonly DataGridView _grid;
    private readonly TextBox _logBox;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    public MainForm(ServerManager manager, BackupService backupService, RconService rconService, SteamCmdService steamCmdService, SoftStopService softStopService, UpdateService updateService, Logger logger)
    {
        _manager = manager;
        _backupService = backupService;
        _rconService = rconService;
        _steamCmdService = steamCmdService;
        _softStopService = softStopService;
        _updateService = updateService;
        _logger = logger;

        Text = "seDirector Clean v1.5";
        Size = new Size(1050, 650);
        MinimumSize = new Size(950, 550);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);

        var topPanel = new Panel { Dock = DockStyle.Top, Height = 45 };
        var btnStart = new Button { Text = "Запустить", Location = new Point(5, 8), Size = new Size(95, 30) };
        var btnStop = new Button { Text = "Остановить", Location = new Point(105, 8), Size = new Size(95, 30) };
        var btnRestart = new Button { Text = "Перезапуск", Location = new Point(205, 8), Size = new Size(95, 30) };
        var btnBackup = new Button { Text = "Бэкап", Location = new Point(305, 8), Size = new Size(75, 30) };
        var btnSteam = new Button { Text = "SteamCMD", Location = new Point(385, 8), Size = new Size(90, 30) };
        var btnRcon = new Button { Text = "RCON", Location = new Point(480, 8), Size = new Size(65, 30) };
        var btnUpdate = new Button { Text = "Обновления", Location = new Point(550, 8), Size = new Size(100, 30) };
        var btnReload = new Button { Text = "Перечитать", Location = new Point(655, 8), Size = new Size(90, 30) };
        var btnNetwork = new Button { Text = "Сеть", Location = new Point(750, 8), Size = new Size(70, 30) };
        var btnSettings = new Button { Text = "Настройки", Location = new Point(825, 8), Size = new Size(100, 30) };

        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnRestart.Click += BtnRestart_Click;
        btnBackup.Click += BtnBackup_Click;
        btnSteam.Click += BtnSteam_Click;
        btnRcon.Click += BtnRcon_Click;
        btnUpdate.Click += BtnUpdate_Click;
        btnReload.Click += BtnReload_Click;
        btnNetwork.Click += BtnNetwork_Click;
        btnSettings.Click += BtnSettings_Click;

        topPanel.Controls.AddRange(new Control[] { btnStart, btnStop, btnRestart, btnBackup, btnSteam, btnRcon, btnUpdate, btnReload, btnNetwork, btnSettings });

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        _grid.Columns.Add("Num", "N");
        _grid.Columns.Add("Name", "Имя");
        _grid.Columns.Add("Status", "Статус");
        _grid.Columns.Add("Uptime", "Аптайм");
        _grid.Columns.Add("Memory", "Память");
        _grid.Columns.Add("Path", "Путь");
        _grid.Columns["Num"].FillWeight = 8;
        _grid.Columns["Name"].FillWeight = 20;
        _grid.Columns["Status"].FillWeight = 18;
        _grid.Columns["Uptime"].FillWeight = 12;
        _grid.Columns["Memory"].FillWeight = 12;
        _grid.Columns["Path"].FillWeight = 30;

        var splitContainer = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 350 };

        _logBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(220, 220, 220),
            Font = new Font("Consolas", 9f)
        };

        var logLabel = new Label { Text = " Лог событий", Dock = DockStyle.Top, Height = 22, TextAlign = ContentAlignment.MiddleLeft };

        splitContainer.Panel1.Controls.Add(_grid);
        splitContainer.Panel2.Controls.Add(_logBox);
        splitContainer.Panel2.Controls.Add(logLabel);

        Controls.Add(splitContainer);
        Controls.Add(topPanel);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _refreshTimer.Tick
