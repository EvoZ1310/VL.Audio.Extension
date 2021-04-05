using System;
using VVVV.Audio;

namespace VL.Audio.Extension.Effects
{
    public class MonoDelaySignal : AudioSignal
    {
        SigParamAudio FAudioIn = new SigParamAudio("Input");

        SigParamDiff<float> DelayMs = new SigParamDiff<float>("Delay ms", 300);
        SigParamDiff<float> FeedbackDB = new SigParamDiff<float>("Feedback DB", -5);
        SigParamDiff<float> MixInDB = new SigParamDiff<float>("MixIn DB", 0);
        SigParamDiff<float> OutputWetDB = new SigParamDiff<float>("Output Wet DB", -6);
        SigParamDiff<float> OutputDryDB = new SigParamDiff<float>("Output Dry DB", 0);
        SigParam<bool> ResampleOnLengthChange = new SigParam<bool>("Resample On Length Change", initValue: true);

        float delaypos;
        float odelay;
        float delaylen;
        float wetmix;
        float drymix;
        float wetmix2;
        float drymix2;
        float rspos;
        int rspos2;
        float drspos;
        int tpos;
        float[] buffer = new float[500000];

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            FAudioIn.Read(buffer, offset, count);
            for (int i = 0; i < count; i++)
            {
                buffer[i] = Sample(buffer[i]);
            }
        }

        public float Sample(float sample)
        {
            int dpint = (int)delaypos;
            float os1 = buffer[dpint + 0];

            buffer[dpint + 0] = Math.Min(Math.Max(sample * drymix + os1 * wetmix, -4), 4);

            if ((delaypos += 1) >= delaylen)
            {
                delaypos = 0;
            }

            return sample * drymix2 + os1 * wetmix2;
        }

        public MonoDelaySignal()
        {
            DelayMs.ValueChanged += Resample;
            FeedbackDB.ValueChanged += value => wetmix = (float)Math.Pow(2, value / 6);
            MixInDB.ValueChanged += value => drymix = (float)Math.Pow(2, value / 6);
            OutputWetDB.ValueChanged += value => wetmix2 = (float)Math.Pow(2, value / 6);
            OutputDryDB.ValueChanged += value => drymix2 = (float)Math.Pow(2, value / 6);
        }

        private void Resample(float value)
        {
            odelay = delaylen;
            delaylen = Math.Min(value * SampleRate / 1000, 500000);
            if (odelay != delaylen)
            {
                if (ResampleOnLengthChange.Value && odelay > delaylen)
                {
                    // resample down delay buffer, heh
                    rspos = 0; rspos2 = 0;
                    drspos = odelay / delaylen;
                    for (int n = 0; n < delaylen; n++)
                    {
                        tpos = ((int)rspos) * 2;
                        buffer[rspos2 + 0] = buffer[tpos + 0];
                        buffer[rspos2 + 1] = buffer[tpos + 1];
                        rspos2 += 2;
                        rspos += drspos;
                    }
                    delaypos /= drspos;
                    delaypos = (int)delaypos;
                    if (delaypos < 0) delaypos = 0;
                }
                else
                {
                    if (ResampleOnLengthChange.Value && odelay < delaylen)
                    {
                        // resample up delay buffer, heh
                        drspos = odelay / delaylen;
                        rspos = odelay;
                        rspos2 = (int)delaylen * 2;
                        for (int n = 0; n < (int)delaylen; n++)
                        {
                            rspos -= drspos;
                            rspos2 -= 2;

                            tpos = ((int)(rspos)) * 2;
                            buffer[rspos2 + 0] = buffer[tpos + 0];
                            buffer[rspos2 + 1] = buffer[tpos + 1];
                        }
                        delaypos /= drspos;
                        delaypos = (int)delaypos;
                        if (delaypos < 0) delaypos = 0;
                    }
                    else
                    {
                        if (ResampleOnLengthChange.Value && delaypos >= delaylen) delaypos = 0;
                    }
                }
                //freembuf(delaylen*2);
            }
        }
    }
}
