using AudioSwitcher.AudioApi.CoreAudio;
using Shortcut;
using System;
using System.Drawing;
using System.Windows.Forms;
using MicMute.Properties;
//проверка запущен ли zoom
//private bool ZoomIsOn => Process.GetProcessesByName("zoom").Any();
namespace MicMute
{
    public partial class MainForm : Form
    {
        private readonly CoreAudioMicMute CAMM = new CoreAudioMicMute();
        public CoreAudioController AudioController = new CoreAudioController();
        private readonly HotkeyBinder hotkeyBinder = new HotkeyBinder();
        private Hotkey hotkey;
        bool micMuteStatus;

        public MainForm() {
            InitializeComponent();
            TransparencyKey = Color.Black; //задаем прозрачность задника черного цвета
            Location = new Point(Settings.Default.posX, Settings.Default.posY); //загружаем последнюю точку расположения окна
            SetWindowSize(); //загружаем размер окна
            ShowInTaskbar = false; //не показываем окно в трее
            MouseWheel += This_MouseWheel; //прицеаляем обработчк события колеса мыши
        }

        private void SetWindowSize() {
            //если микрофон выключен
            if (micMuteStatus) {
                this.Width = Settings.Default.thisWidthOff;
                this.Height = Settings.Default.thisHeightOff;
            } else {
                this.Width = Settings.Default.thisWidthOn;
                this.Height = Settings.Default.thisHeightOn;
            }
        }

        private void MicOFF() {
            pictureBox1.Image = new Bitmap(Resources.mic_off_bmp_black);
            CAMM.SetMute(true);
            micMuteStatus = true;
            SetWindowSize();
        }
        private void MicON() {
            pictureBox1.Image = new Bitmap(Resources.mic_on_bmp_black);
            CAMM.SetMute(false);
            micMuteStatus = false;
            SetWindowSize();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
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
                micMuteStatus = true;
            } else {
                MicON();
                micMuteStatus = false;
            }
            UpdateDevice(AudioController.DefaultCaptureDevice);
            AudioController.AudioDeviceChanged.Subscribe(OnNextDevice);
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
            if (e.Button == MouseButtons.Left) {
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
        #region Update mic status
        private void OnNextDevice(AudioSwitcher.AudioApi.DeviceChangedArgs next) => UpdateDevice(AudioController.DefaultCaptureDevice);
        private void OnMuteChanged(AudioSwitcher.AudioApi.DeviceMuteChangedArgs next) => UpdateStatus(next.Device);

        IDisposable muteChangedSubscription;
        public void UpdateDevice(AudioSwitcher.AudioApi.IDevice device) {
            muteChangedSubscription?.Dispose();
            muteChangedSubscription = device?.MuteChanged.Subscribe(OnMuteChanged);
            UpdateStatus(device);
        }

        readonly Icon iconOff = Properties.Resources.off;
        readonly Icon iconOn = Properties.Resources.on;
        readonly Icon iconError = Properties.Resources.error;

        public void UpdateStatus(AudioSwitcher.AudioApi.IDevice device) {
            if (device != null) {
                UpdateIcon(device.IsMuted ? iconOff : iconOn, device.FullName);
            } else {
                UpdateIcon(iconError, "< No device >");
            }
        }
        private void UpdateIcon(Icon icon, string tooltipText) {
            this.icon.Icon = icon;
            this.icon.Text = tooltipText;
        }
        #endregion

        #region Dragging and sizing Window

        void This_MouseWheel(object sender, MouseEventArgs e) {

            if (e.Delta > 0) {
                Width += 5;
                Height += 5;
            } else {
                Width -= 5;
                Height -= 5;
            }

            if (Control.ModifierKeys == Keys.Shift) {
                if (micMuteStatus) {
                    Settings.Default.thisWidthOff = Width;
                    Settings.Default.thisHeightOff = Height;
                } else {
                    Settings.Default.thisWidthOn = Width;
                    Settings.Default.thisHeightOn = Height;
                }
            } else {
                Settings.Default.thisWidthOff = Width;
                Settings.Default.thisHeightOff = Height;
                Settings.Default.thisWidthOn = Width;
                Settings.Default.thisHeightOn = Height;
            }
        }

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
        private void MainForm_MouseUp(object sender, MouseEventArgs e) => dragging = false;

        #endregion

        #region Context menu
        private void InfoAboutMic_Opening(object sender, System.ComponentModel.CancelEventArgs e) {
            var device = AudioController.DefaultCaptureDevice;
            this.InfoToolStripMenuItem.Text =$"Используется микрофон {device.FullName}";
        }
        private void ResetToolStripMenuItem_Click(object sender, EventArgs e) {
            Width = 100;
            Height = 100;
            Settings.Default.thisHeightOff = 100;
            Settings.Default.thisHeightOn = 100;
            Settings.Default.thisWidthOff = 100;
            Settings.Default.thisWidthOn = 100;
        }
        #endregion


    }
}
