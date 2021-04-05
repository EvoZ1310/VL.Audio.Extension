using System;
using VVVV.Audio;

namespace VL.Audio.Extension.Effects
{
    public class CompressorSignal : AudioSignal
    {
        SigParamAudio AudioIn = new SigParamAudio("Input");

        SigParamDiff<float> Threshold = new SigParamDiff<float>("Treshold (%)");
        SigParamDiff<float> SlopeAngle = new SigParamDiff<float>("Slope Angle (%)");
        SigParamDiff<float> AttackTime = new SigParamDiff<float>("Attack Time (ms)");
        SigParamDiff<float> ReleaseTime = new SigParamDiff<float>("Release Time (ms)");
        SigParamDiff<float> WindowTime = new SigParamDiff<float>("Window Time (ms)");
        SigParamDiff<float> LookAheadTime = new SigParamDiff<float>("Look Ahead Time (ms)");

        public bool IsActive { get; private set; }

        float _threshold;
        float _slopeAngle;
        float _attackTime;
        float _releaseTime;

        float _attack;
        float _release;

        int _windowSampleCount;
        int _lookaheadSampleCount;

        public CompressorSignal()
        {
            Threshold.ValueChanged += value => ValueChanged(value);
            SlopeAngle.ValueChanged += value => ValueChanged(value);
            AttackTime.ValueChanged += value => ValueChanged(value);
            ReleaseTime.ValueChanged += value => ValueChanged(value);
            WindowTime.ValueChanged += value => ValueChanged(value);
            LookAheadTime.ValueChanged += value => ValueChanged(value);

            ValueChanged(0);
        }

        private void ValueChanged(float value)
        {
            _threshold = Threshold.Value * 0.01f; // value to 0-1
            _slopeAngle = SlopeAngle.Value * 0.01f; // value to 0-1

            _attackTime = AttackTime.Value / 1000; // milliseconds to seconds
            _releaseTime = ReleaseTime.Value / 1000; // milliseconds to seconds

            // attack and release "per sample decay"
            if (_attackTime != 0)
            {
                _attack = (float)Math.Exp(-1.0f / (SampleRate * _attackTime));
            }

            if (_releaseTime != 0)
            {
                _release = (float)Math.Exp(-1.0f / (SampleRate * _releaseTime));
            }

            _windowSampleCount = (int)(SampleRate * (WindowTime.Value / 1000));
            _lookaheadSampleCount = (int)(SampleRate * (LookAheadTime.Value / 1000));
        }

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            AudioIn.Read(buffer, offset, count);

            IsActive = false;

            double envelope = 0;

            for (int i = 0; i < count; i++)
            {
                // calculate RMS
                float sum = 0;
                for(int j = 0; j < _windowSampleCount; j++)
                {
                    int indexOfSampleToSum = i + j + _lookaheadSampleCount;

                    float sampleValue;
                    if (indexOfSampleToSum < count)
                    {
                        sampleValue = buffer[indexOfSampleToSum];
                    }
                    else { break; } // if we are ahead of the current buffer size, break loop (looked too far into the future ;) )

                    sum = sampleValue * sampleValue;
                }

                double rms = Math.Sqrt(sum / _windowSampleCount); // the root-mean-square  

                // dynamic selection: attack or release?
                double theta = rms > envelope ? _attack : _release;

                // smoothing with capacitor, envelope extraction...
                // here be aware of pIV denormal numbers glitch (??????)
                envelope = (1.0 - theta) * rms + theta * envelope;

                // the very easy hard knee 1:N compressor
                double gain = 1.0;
                if (envelope > _threshold)
                {
                    gain = gain - (envelope - _threshold) * _slopeAngle;
                    IsActive = true;
                }


                buffer[i] = buffer[i] * (float)gain;
            }
        }
    }
}
