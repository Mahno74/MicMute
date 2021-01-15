using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using Microsoft.Win32;
using Shortcut;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reactive;
using System.Threading;
using System.Windows.Forms;
using MicMute.Properties;
using System.Linq;
using System.Runtime.InteropServices;

namespace MicMute
{
    public partial class MainForm : Form
    {
        public CoreAudioController AudioController = new CoreAudioController();
        private readonly HotkeyBinder hotkeyBinder = new HotkeyBinder();
        private Hotkey hotkey;

        public MainForm()
        {
            InitializeComponent();
            Width = 300; Height = 80;
            this.Location = new Point(Settings.Default.posX, Settings.Default.posY);
            ShowInTaskbar = false;
        }
        //проверка запущен ли zoom
        //private bool ZoomIsOn => Process.GetProcessesByName("zoom").Any();
        private void OnNextDevice(DeviceChangedArgs next) => UpdateDevice(AudioController.DefaultCaptureDevice);
        private void MicOFF() {
            Height = 5;
            BackColor = Color.Green;
        }
        private void MicON()
        {
            Height = 80;
            BackColor = Color.Red;
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            //if (ZoomIsOn) {
            //    MessageBox.Show("Запущена конференция в ZOOM, приложение будет закрыто");
            //    Application.Exit();
            //} else {
            //    //Создаем хот кей для выключения-включения микрофонов
            //    try {
            //        hotkey = new Hotkey(Modifiers.Control, Keys.A);
            //        hotkeyBinder.Bind(hotkey).To(ToggleMicStatus);
            //    } catch (Exception ex) {
            //        MessageBox.Show(ex.Message);
            //    }
            //}
            try {
                hotkey = new Hotkey(Modifiers.Control, Keys.A);
                hotkeyBinder.Bind(hotkey).To(ToggleMicStatus);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
            var device = AudioController.DefaultCaptureDevice;
            //проверяем доступность микрофона при старте
            if (device.IsMuted) {
                MicOFF();
            } else {
                MicON();
            }
            UpdateDevice(AudioController.DefaultCaptureDevice);
            AudioController.AudioDeviceChanged.Subscribe(OnNextDevice);
        }

        private void OnMuteChanged(DeviceMuteChangedArgs next) => UpdateStatus(next.Device);

        IDisposable muteChangedSubscription;
        public void UpdateDevice(IDevice device)
        {
            muteChangedSubscription?.Dispose();
            muteChangedSubscription = device?.MuteChanged.Subscribe(OnMuteChanged);
            UpdateStatus(device);
        }

        readonly Icon iconOff = Properties.Resources.off;
        readonly Icon iconOn = Properties.Resources.on;
        readonly Icon iconError = Properties.Resources.error;

        public void UpdateStatus(IDevice device)
        {
            if (device != null) {
                UpdateIcon(device.IsMuted ? iconOff : iconOn, device.FullName);
            } else {
                UpdateIcon(iconError, "< No device >");
            }
        }
        private void UpdateIcon(Icon icon, string tooltipText)
        {
            this.icon.Icon = icon;
            this.icon.Text = tooltipText;
        }

        public async void ToggleMicStatus()
        {
            await AudioController.DefaultCaptureDevice?.ToggleMuteAsync();

            var device = AudioController.DefaultCaptureDevice;
            if (!device.IsMuted) {
                MicON();
            } else {
                MicOFF();
            } 
        }

        private void Icon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleMicStatus();
            }
        }


        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.posX = Location.X;
            Settings.Default.posY = Location.Y;
            Settings.Default.Save();
            Application.Exit();
        }

        #region Dragging Window

        bool dragging = false;
        int xOffset = 0;
        int yOffset = 0;
        private void MainForm_MouseDown(object sender, MouseEventArgs e) {
            dragging = true;

            xOffset = Cursor.Position.X - this.Location.X;
            yOffset = Cursor.Position.Y - this.Location.Y;
        }
        private void MainForm_MouseMove(object sender, MouseEventArgs e) {
            if (dragging) {
                this.Location = new Point(Cursor.Position.X - xOffset, Cursor.Position.Y - yOffset);
                this.Update();
            }
        }
      
        private void MainForm_MouseUp(object sender, MouseEventArgs e) {
            dragging = false;
        }

        #endregion


        private void InfoAboutMic_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            var device = AudioController.DefaultCaptureDevice;
            this.InfoToolStripMenuItem.Text = device.FullName.ToString();
        }
    }
}
