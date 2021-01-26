using System.Windows.Forms;
using NAudio.CoreAudioApi;

namespace MicMute {
    class CoreAudioMicMute {
        private readonly MMDevice[] rgMicDevice; //Для записи найденных для нас устройств
        readonly int MaxMicro = 0;

        public CoreAudioMicMute() {
            MMDeviceEnumerator DevEnum = new MMDeviceEnumerator();

            MMDeviceCollection devices = DevEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active); // DataFlow.Capture - Микрофоны(или устройства в которые поступает звук), DeviceState.Active - Активные устройства
                                                                                                                // Поиск активных устройств(для нас микрофонов)
            MaxMicro = 0;
            for (int i = 0; i < devices.Count; i++) // devices.Count - количество устройств(активные микрофоны)
            {
                MMDevice deviceAt = devices[i];
                if (deviceAt.DataFlow == DataFlow.Capture && deviceAt.State == DeviceState.Active) {
                    ++MaxMicro;
                }
            }
            // Заносим в массив (все) найденный(ые) микрофон(ы) или другие устройства(динамики, наушники или др)  
            rgMicDevice = new MMDevice[MaxMicro];
            MaxMicro = 0;
            for (int i = 0; i < devices.Count; i++) {
                MMDevice deviceAt = devices[i];
                if (deviceAt.DataFlow == DataFlow.Capture && deviceAt.State == DeviceState.Active) //Меняем на свое устройство(а)
                {
                    MaxMicro++;
                    rgMicDevice[MaxMicro - 1] = deviceAt;
                }
            }

            if (MaxMicro == 0)//Если не найден ни один микрофон(устройство)
                MessageBox.Show("Микрофон не найден!"); //Было в коде, от куда я взял. Программа прекратит выполнение, выдав экзепшин), если не поменять на, что либо другое.
        }

        public void SetMute(bool mute) //Функция, отключающая звук устройств записанных в массив  private MMDevice[] rgMicDevice
        {
            for (int i = 0; i < MaxMicro; i++) {
                rgMicDevice[i].AudioEndpointVolume.Mute = mute; //= true - выключить звук устройства(для нас микрофона)
            }
        }
    }
}
