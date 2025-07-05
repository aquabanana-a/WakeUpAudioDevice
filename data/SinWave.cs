using NAudio.Wave;

namespace WakeUpAudioDevice.data
{
    internal class SinWave : WaveProvider32
    {
        public float Frequency { get; set; }
        public float Amplitude { get; set; }
        private double phaseAngle;

        public override int Read(float[] buffer, int offset, int sampleCount)
        {
            int sampleRate = WaveFormat.SampleRate;
            for (int n = 0; n < sampleCount; n++)
            {
                buffer[n + offset] = Amplitude * (float)Math.Sin(phaseAngle);
                phaseAngle += 2 * Math.PI * Frequency / sampleRate;
                if (phaseAngle > 2 * Math.PI)
                    phaseAngle -= 2 * Math.PI;
            }
            return sampleCount;
        }
    }
}