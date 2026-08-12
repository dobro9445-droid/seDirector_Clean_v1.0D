using System.Drawing;
using System.Windows.Forms;
using seDirector.Models;

namespace seDirector.GUI;

public sealed class NetworkSettingsForm : Form
{
    private readonly Server _server;

    private readonly TextBox _txtLocalIP;
    private readonly TextBox _txtExternalIP;
    private readonly NumericUpDown _numGamePort;
    private readonly NumericUpDown _numRconPort;
    private readonly NumericUpDown _numQueryPort;
    private readonly CheckBox _chkUseWAN;
    private readonly TextBox _txtWANPassword;

    public NetworkSettingsForm(Server server)
    {
        _server = server;

        var network = server.Network ?? new NetworkConfig();

        Text = "Сетевые настройки — " + server.Name;
        Size = new Size(480, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9.5f);

        var lblLocalIP = new Label { Text = "Локальный IP (LAN):", Location = new Point(15, 20), AutoSize = true };
        _txtLocalIP = new TextBox { Text = network.LocalIP, Location = new Point(180, 17), Size = new Size(270, 25) };

        var lblExternalIP = new Label { Text = "Внешний IP (WAN):", Location = new Point(15, 55), AutoSize = true };
        _txtExternalIP = new TextBox { Text = network.ExternalIP, Location = new Point(180, 52), Size = new Size(270, 25) };

        var lblGamePort = new Label { Text = "Игровой порт:", Location = new Point(15, 90), AutoSize = true };
        _numGamePort = new NumericUpDown { Value = network.GamePort, Minimum = 1, Maximum = 65535, Location = new Point(180, 87), Size = new Size(120, 25) };

        var lblRconPort = new Label { Text = "RCON порт:", Location = new Point(15, 125), AutoSize = true };
        _numRconPort = new NumericUpDown { Value = network.RconPort, Minimum = 1, Maximum = 65535, Location = new Point(180, 122), Size = new Size(120, 25) };

        var lblQueryPort = new Label { Text = "Query порт:", Location = new Point(15, 160), AutoSize = true };
        _numQueryPort = new NumericUpDown { Value = network.QueryPort, Minimum = 1, Maximum = 65535, Location = new Point(180, 157), Size = new Size(120, 25) };

        _chkUseWAN = new CheckBox { Text = "Использовать WAN режим", Location = new Point(15, 200), AutoSize = true, Checked = network.UseWAN };

        var lblWANPassword = new Label { Text = "WAN пароль:", Location = new Point(15, 235), AutoSize = true };
        _txtWANPassword = new TextBox { Text = network.WANPassword, Location = new Point(180, 232), Size = new Size(270, 25), UseSystemPasswordChar = true };

        var btnDetectIP = new Button { Text = "Определить локальный IP", Location = new Point(15, 280), Size = new Size(180, 30) };
        btnDetectIP.Click += (s, e) => DetectLocalIP();

        var btnSave = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Location = new Point(270, 330), Size = new Size(90, 32) };
        var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Location = new Point(365, 330), Size = new Size(85, 32) };

        btnSave.Click += (s, e) => SaveSettings();

        Controls.AddRange(new Control[]
        {
            lblLocalIP, _txtLocalIP,
            lblExternalIP, _txtExternalIP,
            lblGamePort, _numGamePort,
            lblRconPort, _numRconPort,
            lblQueryPort, _numQueryPort,
            _chkUseWAN,
            lblWANPassword, _txtWANPassword,
            btnDetectIP,
            btnSave, btnCancel
        });

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void DetectLocalIP()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    _txtLocalIP.Text = ip.ToString();
                    return;
                }
            }
            MessageBox.Show("Не удалось определить локальный IP.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка определения IP: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveSettings()
    {
        if (_server.Network == null)
            _server.Network = new NetworkConfig();

        _server.Network.LocalIP = _txtLocalIP.Text.Trim();
        _server.Network.ExternalIP = _txtExternalIP.Text.Trim();
        _server.Network.GamePort = (int)_numGamePort.Value;
        _server.Network.RconPort = (int)_numRconPort.Value;
        _server.Network.QueryPort = (int)_numQueryPort.Value;
        _server.Network.UseWAN = _chkUseWAN.Checked;
        _server.Network.WANPassword = _txtWANPassword.Text;

        if (_server.Port == null || _server.Port != _server.Network.GamePort)
            _server.Port = _server.Network.GamePort;

        if (_server.Rcon != null)
        {
            _server.Rcon.Host = _chkUseWAN.Checked && !string.IsNullOrWhiteSpace(_server.Network.ExternalIP)
                ? _server.Network.ExternalIP
                : _server.Network.LocalIP;
            _server.Rcon.Port = _server.Network.RconPort;
        }
    }
}
