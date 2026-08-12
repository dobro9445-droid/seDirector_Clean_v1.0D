using System.Drawing;
using System.Windows.Forms;
using seDirector.Models;

namespace seDirector.GUI;

public sealed class ServerSettingsForm : Form
{
    private readonly Server _server;

    private readonly TextBox _txtName;
    private readonly ComboBox _cmbType;
    private readonly TextBox _txtPath;
    private readonly TextBox _txtArguments;
    private readonly NumericUpDown _numPort;
    private readonly NumericUpDown _numMaxPlayers;
    private readonly TextBox _txtMap;
    private readonly NumericUpDown _numTickrate;
    private readonly TextBox _txtGameMode;
    private readonly ComboBox _cmbPriority;
    private readonly CheckBox _chkAutoStart;
    private readonly CheckBox _chkRestartOnExit;
    private readonly NumericUpDown _numRestartDelay;
    private readonly NumericUpDown _numMaxRestarts;
    private readonly CheckBox _chkHideConsole;
    private readonly CheckBox _chkAutoFindPort;
    private readonly TextBox _txtAffinity;

    public ServerSettingsForm(Server server)
    {
        _server = server;

        Text = "Настройки сервера — " + server.Name;
        Size = new Size(560, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabMain = new TabPage("Основные");
        var tabAuto = new TabPage("Автозапуск");

        int y = 12;
        int labelX = 12;
        int fieldX = 170;
        int fieldW = 350;
        int step = 35;

        var lblName = new Label { Text = "Имя сервера:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtName = new TextBox { Text = server.Name, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };
        y += step;

        var lblType = new Label { Text = "Тип:", Location = new Point(labelX, y + 3), AutoSize = true };
        _cmbType = new ComboBox { Location = new Point(fieldX, y), Size = new Size(fieldW, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbType.Items.AddRange(new object[] { "Generic", "Source", "Minecraft", "Rust", "CS2" });
        _cmbType.SelectedItem = server.Type ?? "Generic";
        y += step;

        var lblPath = new Label { Text = "Путь к exe:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtPath = new TextBox { Text = server.Path, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };
        y += step;

        var lblArgs = new Label { Text = "Аргументы:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtArguments = new TextBox { Text = server.Arguments, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };
        y += step;

        var lblPort = new Label { Text = "Порт:", Location = new Point(labelX, y + 3), AutoSize = true };
        _numPort = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = server.Port ?? 27015, Location = new Point(fieldX, y), Size = new Size(120, 25) };
        y += step;

        var lblMaxPlayers = new Label { Text = "Макс. игроков:", Location = new Point(labelX, y + 3), AutoSize = true };
        _numMaxPlayers = new NumericUpDown { Minimum = 1, Maximum = 256, Value = server.MaxPlayers, Location = new Point(fieldX, y), Size = new Size(120, 25) };
        y += step;

        var lblMap = new Label { Text = "Стартовая карта:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtMap = new TextBox { Text = server.Map, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };
        y += step;

        var lblTickrate = new Label { Text = "Тикрейт:", Location = new Point(labelX, y + 3), AutoSize = true };
        _numTickrate = new NumericUpDown { Minimum = 10, Maximum = 128, Value = server.Tickrate, Location = new Point(fieldX, y), Size = new Size(120, 25) };
        y += step;

        var lblGameMode = new Label { Text = "Режим CS2:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtGameMode = new TextBox { Text = server.GameMode, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };

        tabMain.Controls.AddRange(new Control[] { lblName, _txtName, lblType, _cmbType, lblPath, _txtPath, lblArgs, _txtArguments, lblPort, _numPort, lblMaxPlayers, _numMaxPlayers, lblMap, _txtMap, lblTickrate, _numTickrate, lblGameMode, _txtGameMode });

        y = 12;

        var lblPriority = new Label { Text = "Приоритет:", Location = new Point(labelX, y + 3), AutoSize = true };
        _cmbPriority = new ComboBox { Location = new Point(fieldX, y), Size = new Size(fieldW, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbPriority.Items.AddRange(new object[] { "Normal", "High", "Low" });
        _cmbPriority.SelectedItem = server.Priority ?? "Normal";
        y += step;

        _chkAutoStart = new CheckBox { Text = "Автозапуск при старте программы", Location = new Point(labelX, y), AutoSize = true, Checked = server.AutoStart };
        y += step;

        _chkRestartOnExit = new CheckBox { Text = "Автоперезапуск при падении", Location = new Point(labelX, y), AutoSize = true, Checked = server.RestartOnExit };
        y += step;

        var lblRestartDelay = new Label { Text = "Задержка перезапуска (сек):", Location = new Point(labelX, y + 3), AutoSize = true };
        _numRestartDelay = new NumericUpDown { Minimum = 0, Maximum = 3600, Value = server.RestartDelaySeconds, Location = new Point(fieldX, y), Size = new Size(120, 25) };
        y += step;

        var lblMaxRestarts = new Label { Text = "Лимит перезапусков:", Location = new Point(labelX, y + 3), AutoSize = true };
        _numMaxRestarts = new NumericUpDown { Minimum = 0, Maximum = 100, Value = server.MaxRestartAttempts, Location = new Point(fieldX, y), Size = new Size(120, 25) };
        y += step;

        _chkHideConsole = new CheckBox { Text = "Скрывать окно консоли сервера", Location = new Point(labelX, y), AutoSize = true, Checked = server.HideConsole };
        y += step;

        _chkAutoFindPort = new CheckBox { Text = "Автоподбор свободного порта", Location = new Point(labelX, y), AutoSize = true, Checked = server.AutoFindFreePort };
        y += step;

        var lblAffinity = new Label { Text = "Привязка к ядрам CPU:", Location = new Point(labelX, y + 3), AutoSize = true };
        _txtAffinity = new TextBox { Text = server.ProcessorAffinity, Location = new Point(fieldX, y), Size = new Size(fieldW, 25) };

        tabAuto.Controls.AddRange(new Control[] { lblPriority, _cmbPriority, _chkAutoStart, _chkRestartOnExit, lblRestartDelay, _numRestartDelay, lblMaxRestarts, _numMaxRestarts, _chkHideConsole, _chkAutoFindPort, lblAffinity, _txtAffinity });

        tabs.TabPages.Add(tabMain);
        tabs.TabPages.Add(tabAuto);

        var btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Location = new Point(350, 440), Size = new Size(90, 32) };
        var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(445, 440), Size = new Size(85, 32) };
        btnSave.Click += (s, e) => SaveSettings();

        Controls.Add(tabs);
        Controls.Add(btnSave);
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void SaveSettings()
    {
        _server.Name = _txtName.Text.Trim();
        _server.Type = _cmbType.SelectedItem?.ToString() ?? "Generic";
        _server.Path = _txtPath.Text.Trim();
        _server.Arguments = _txtArguments.Text.Trim();
        _server.Port = (int)_numPort.Value;
        _server.MaxPlayers = (int)_numMaxPlayers.Value;
        _server.Map = _txtMap.Text.Trim();
        _server.Tickrate = (int)_numTickrate.Value;
        _server.GameMode = _txtGameMode.Text.Trim();
        _server.Priority = _cmbPriority.SelectedItem?.ToString() ?? "Normal";
        _server.AutoStart = _chkAutoStart.Checked;
        _server.RestartOnExit = _chkRestartOnExit.Checked;
        _server.RestartDelaySeconds = (int)_numRestartDelay.Value;
        _server.MaxRestartAttempts = (int)_numMaxRestarts.Value;
        _server.HideConsole = _chkHideConsole.Checked;
        _server.AutoFindFreePort = _chkAutoFindPort.Checked;
        _server.ProcessorAffinity = _txtAffinity.Text.Trim();
    }
}
