using NAudio.CoreAudioApi;
using NAudio.Wave;
using WakeUpAudioDevice.data;
using WakeUpAudioDevice.tools;

namespace WakeUpAudioDevice
{
    class Program
    {
        private static readonly TimeSpan fallAsleepTimeSpan = TimeSpan.FromMinutes(5);

        private static readonly SinWave wakeUpWave = new SinWave()
        {
            Frequency = 17_000,
            Amplitude = .01f
        }.Apply(it => it.SetWaveFormat(44_100, 1));

        private const string auxDeviceTag = "Xonar";
        private const string bluetoothDeviceTag = "JBL";
        private const float activeAmplitudeThreshold = .001f;
        private const int checkStateTimerMs = 2 * 60 * 1000;

        private static MMDevice? auxDevice;
        private static MMDevice? bluetoothDevice;

        private static WasapiLoopbackCapture? auxCapture;
        private static DateTime auxActiveLastTime = DateTime.UtcNow - TimeSpan.FromDays(1);

        static async Task Main(string[] args)
        {
            var enumerator = new MMDeviceEnumerator();

            auxDevice = enumerator
                .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            if (!auxDevice.FriendlyName.Contains(auxDeviceTag))
            {
                Console.WriteLine($"Device {auxDeviceTag} is not default endpoint.. Default audio is set to {auxDevice.FriendlyName}");
                return;
            }

            bluetoothDevice = enumerator
                .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                .FirstOrDefault((i) => i.FriendlyName.Contains(bluetoothDeviceTag));
            if (bluetoothDevice == null)
            {
                Console.WriteLine($"Device {bluetoothDeviceTag} not found..");
                return;
            }

            auxCapture = new WasapiLoopbackCapture(auxDevice).Apply(it =>
            {
                it.DataAvailable += (_, e) =>
                {
                    if (GetAverageInputAmplitude(e) > activeAmplitudeThreshold)
                    {
                        it.StopRecording();
                        if (DateTime.UtcNow - auxActiveLastTime > fallAsleepTimeSpan)
                        {
                            PlayWakeUpWave();
                        }
                        auxActiveLastTime = DateTime.UtcNow;
                    }
                };
            });

            await AudioDataListenerTask();
        }

        static float GetAverageInputAmplitude(WaveInEventArgs e)
        {
            float sum = 0;
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i);
                sum += Math.Abs(sample);
            }
            return sum / (e.BytesRecorded / 4);
        }

        static async Task AudioDataListenerTask()
        {
            CheckAudioState();

            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(checkStateTimerMs));
            while (await timer.WaitForNextTickAsync())
            {
                CheckAudioState();
            }
        }

        static void CheckAudioState()
        {
            if (auxCapture?.CaptureState == CaptureState.Stopped)
            {
                auxCapture.StartRecording();
            }
        }

        static void PlayWakeUpWave()
        {
            using (var wasapiOut = new WasapiOut(bluetoothDevice, AudioClientShareMode.Shared, false, 50))
            {
                wasapiOut.Init(wakeUpWave);
                wasapiOut.Play();
            }
        }
    }
}