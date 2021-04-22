using System;
using VVVV.Audio;

namespace VL.Audio.Extension.Effects
{
    public class MonoDelaySignal : AudioSignal
    {
        SigParamAudio FAudioIn = new SigParamAudio("Input");

        SigParamDiff<float> DelayMs = new SigParamDiff<float>("Delay ms (0-2000)");
        SigParamDiff<float> Feedback = new SigParamDiff<float>("Feedback %");
        SigParamDiff<float> WetDry = new SigParamDiff<float>("Wet/Dry %");

        float _feedback;
        float _wetDry;

        float[] _delayBuffer;
        int _delayBufferReadIndex;
        int _delayBufferWriteIndex;
        int _delayInSamples;

        public MonoDelaySignal()
        {
            InitializeDelayBuffer();
            ReconfigureDelayBuffer();

            DelayMs.ValueChanged += value => ReconfigureDelayBuffer();
            Feedback.ValueChanged += value => _feedback = value / 100;
            WetDry.ValueChanged += value => _wetDry = value / 100;
        }

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            FAudioIn.Read(buffer, offset, count);

            for (int i = 0; i < count; i++)
            {
                float xn = buffer[i];
                float yn = _delayBuffer[_delayBufferReadIndex];

                if(_delayInSamples == 0)
                {
                    yn = xn;
                }

                _delayBuffer[_delayBufferWriteIndex] = xn + _feedback * yn;

                buffer[i] = _wetDry * yn + (1f - _wetDry) * xn;

                _delayBufferWriteIndex++;
                if (_delayBufferWriteIndex >= _delayBuffer.Length)
                    _delayBufferWriteIndex = 0;

                _delayBufferReadIndex++;
                if (_delayBufferReadIndex >= _delayBuffer.Length)
                    _delayBufferReadIndex = 0;
            }
        }

        protected override void Engine_SampleRateChanged(object sender, EventArgs e)
        {
            base.Engine_SampleRateChanged(sender, e);

            InitializeDelayBuffer();
            ReconfigureDelayBuffer();
        }

        private void InitializeDelayBuffer()
        {
            var bufferSize = 2 * SampleRate; // 2 seconds
            _delayBuffer = new float[bufferSize];
        }

        private void ReconfigureDelayBuffer()
        {
            ResetDelay();

            float delayInMs = DelayMs.Value;

            if (delayInMs < 0)
                delayInMs = 0;

            if (delayInMs > 2000)
                delayInMs = 2000;

            _delayInSamples = Convert.ToInt32(delayInMs * (SampleRate / 1000f));

            _delayBufferWriteIndex = _delayInSamples;
        }

        private void ResetDelay()
        {
            _delayBufferReadIndex = 0;
            _delayBufferWriteIndex = 0;

            Array.Clear(_delayBuffer, 0, _delayBuffer.Length);
        }


    }
}
