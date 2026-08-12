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
        Size = new Size(950, 650);
        MinimumSize = new Size(850, 550);
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

        btnStart.Click += BtnStart_Click;
        btnStop.Click += BtnStop_Click;
        btnRestart.Click += BtnRestart_Click;
        btnBackup.Click += BtnBackup_Click;
        btnSteam.Click += BtnSteam_Click;
        btnRcon.Click += BtnRcon_Click;
        btnUpdate.Click += BtnUpdate_Click;
        btnReload.Click += BtnReload_Click;
        btnNetwork.Click += BtnNetwork_Click;

        topPanel.Controls.AddRange(new Control[] { btnStart, btnStop, btnRestart, btnBackup, btnSteam, btnRcon, btnUpdate, btnReload, btnNetwork });

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
        _refreshTimer.Tick += (s, e) => RefreshData();

        Load += (s, e) => { RefreshData(); _refreshTimer.Start(); };

        FormClosing += (s, e) =>
        {
            var result = MessageBox.Show("Выйти из seDirector Clean?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No) { e.Cancel = true; return; }
            var stopResult = MessageBox.Show("Остановить все запущенные серверы?", "Остановка серверов", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (stopResult == DialogResult.Yes) _manager.StopAll();
            _refreshTimer.Stop();
        };
    }

    private int GetSelectedIndex()
    {
        if (_grid.SelectedRows.Count == 0) { MessageBox.Show("Выберите сервер в таблице.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning); return -1; }
        return _grid.SelectedRows[0].Index;
    }

    private void RefreshData()
    {
        _grid.Rows.Clear();
        for (var i = 0; i < _manager.Servers.Count; i++)
        {
            var server = _manager.Servers[i];
            _grid.Rows.Add(i + 1, server.Name, _manager.GetStatus(i), _manager.GetUptime(i), _manager.GetMemoryUsage(i), server.Path);
        }
        var lines = _logger.ReadLastLines(50);
        _logBox.Lines = lines.ToArray();
        if (_logBox.Lines.Length > 0) _logBox.SelectionStart = _logBox.Text.Length;
    }

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        var success = _manager.TryStart(index);
        MessageBox.Show(success ? "Сервер запущен." : "Не удалось запустить сервер.", "Результат", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        RefreshData();
    }

    private void BtnStop_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        var result = MessageBox.Show("Остановить сервер?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        var success = _softStopService.Stop(index);
        MessageBox.Show(success ? "Сервер остановлен." : "Не удалось остановить сервер.", "Результат", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        RefreshData();
    }

    private void BtnRestart_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        var result = MessageBox.Show("Перезапустить сервер?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        var success = _manager.TryRestart(index);
        MessageBox.Show(success ? "Сервер перезапущен." : "Не удалось перезапустить сервер.", "Результат", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        RefreshData();
    }

    private void BtnBackup_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        var success = _backupService.BackupServer(_manager.Servers[index]);
        MessageBox.Show(success ? "Резервная копия создана." : "Не удалось создать резервную копию.", "Результат", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    private void BtnSteam_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        if (!_steamCmdService.IsAvailable()) { MessageBox.Show("SteamCMD не найден.", "SteamCMD", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (_manager.IsRunning(index))
        {
            var stopResult = MessageBox.Show("Сервер запущен. Остановить и обновить?", "SteamCMD", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (stopResult != DialogResult.Yes) return;
            _manager.TryStop(index);
        }
        var result = MessageBox.Show("Обновить сервер через SteamCMD?", "SteamCMD", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;
        Cursor = Cursors.WaitCursor;
        var success = _steamCmdService.UpdateServer(_manager.Servers[index]);
        Cursor = Cursors.Default;
        MessageBox.Show(success ? "Обновление завершено." : "Ошибка обновления.", "SteamCMD", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        RefreshData();
    }

    private void BtnRcon_Click(object? sender, EventArgs e)
    {
        var index = GetSelectedIndex();
        if (index < 0) return;
        var server = _
