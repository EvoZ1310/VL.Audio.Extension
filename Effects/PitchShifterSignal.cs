using System;
using VVVV.Audio;

namespace VL.Audio.Extension.Effects
{
    public class PitchShifterSignal : AudioSignal
    {
        SigParamAudio FAudioIn = new SigParamAudio("Input");

        SigParamDiff<int> SemitonePitch = new SigParamDiff<int>("Semitone Pitch");
        SigParamDiff<bool> Filter = new SigParamDiff<bool>("Filter");

        int buffersize;
        float xfade;
        int bufloc;
        int buffer0;
        float pitch;

        float denorm;
        float v0;
        float h01, h02, h03, h04;
        float a1, a2, a3, b1, b2;
        float t0;
        readonly float[] pitchBuffer = new float[64000];

        public PitchShifterSignal()
        {
            Init();
        }

        private void Init()
        {
            buffersize = SampleRate; // srate|0;
            xfade = 100;
            bufloc = 10000;

            buffer0 = bufloc;
            pitch = 1.0f;
            denorm = (float)Math.Pow(10, -20);

            SemitonePitch.ValueChanged += value => PitchValueChanged(value);
            Filter.ValueChanged += value => FilterValueChanged(value);

            ValueChanged();
        }

        private void PitchValueChanged(float value)
        {
            ValueChanged();
        }

        private void FilterValueChanged(bool value)
        {
            ValueChanged();
        }

        private void ValueChanged()
        {
            // just for test
            //filter = slider8 > 0.5;
            int bsnew = (int)(Math.Min(50, 1000) * 0.001 * SampleRate);
            if (bsnew != buffersize)
            {
                buffersize = bsnew;
                v0 = buffer0 + buffersize * 0.5f;
                if (v0 > bufloc + buffersize)
                {
                    v0 -= buffersize;
                }
            }

            xfade = (int)(20 * 0.001 * SampleRate);
            if (xfade > bsnew * 0.5)
            {
                xfade = bsnew * 0.5f;
            }

            float npitch = (float)Math.Pow(2, (SemitonePitch.Value / 12));

            if (pitch != npitch)
            {
                pitch = npitch;
                float lppos = (pitch > 1.0f) ? 1.0f / pitch : pitch;
                if (lppos < (0.1f / SampleRate))
                {
                    lppos = 0.1f / SampleRate;
                }
                float r = 1.0f;
                float c = 1.0f / (float)Math.Tan(Math.PI * lppos * 0.5f);
                a1 = 1.0f / (1.0f + r * c + c * c);
                a2 = 2 * a1;
                a3 = a1;
                b1 = 2.0f * (1.0f - c * c) * a1;
                b2 = (1.0f - r * c + c * c) * a1;
                h01 = h02 = h03 = h04 = 0;
            }
        }

        protected override void FillBuffer(float[] buffer, int offset, int count)
        {
            FAudioIn.Read(buffer, offset, count);
            for (int i = 0; i < count; i++)
            {
                buffer[i] = Sample(buffer[i]);
            }
        }

        private float Sample(float sample)
        {
            int iv0 = (int)(v0);
            float frac0 = v0 - iv0;
            int iv02 = (iv0 >= (bufloc + buffersize - 1)) ? iv0 - buffersize + 1 : iv0 + 1;

            float ren0 = (pitchBuffer[iv0 + 0] * (1 - frac0) + pitchBuffer[iv02 + 0] * frac0);
            float vr = pitch;
            float tv, frac, tmp, tmp2;
            if (vr >= 1.0)
            {
                tv = v0;
                if (tv > buffer0) tv -= buffersize;
                if (tv >= buffer0 - xfade && tv < buffer0)
                {
                    // xfade
                    frac = (buffer0 - tv) / xfade;
                    tmp = v0 + xfade;
                    if (tmp >= bufloc + buffersize) tmp -= buffersize;
                    tmp2 = (tmp >= bufloc + buffersize - 1) ? bufloc : tmp + 1;
                    ren0 = ren0 * frac + (1 - frac) * (pitchBuffer[(int)tmp + 0] * (1 - frac0) + pitchBuffer[(int)tmp2 + 0] * frac0);
                    if (tv + vr > buffer0 + 1) v0 += xfade;
                }
            }
            else
            {// read pointer moving slower than write pointer
                tv = v0;
                if (tv < buffer0) tv += buffersize;
                if (tv >= buffer0 && tv < buffer0 + xfade)
                {
                    // xfade
                    frac = (tv - buffer0) / xfade;
                    tmp = v0 + xfade;
                    if (tmp >= bufloc + buffersize) tmp -= buffersize;
                    tmp2 = (tmp >= bufloc + buffersize - 1) ? bufloc : tmp + 1;
                    ren0 = ren0 * frac + (1 - frac) * (pitchBuffer[(int)tmp + 0] * (1 - frac0) + pitchBuffer[(int)tmp2 + 0] * frac0);
                    if (tv + vr < buffer0 + 1) v0 += xfade;
                }
            }


            if ((v0 += vr) >= (bufloc + buffersize)) v0 -= buffersize;

            if (Filter.Value && pitch > 1.0)
            {

                t0 = sample;
                sample = a1 * sample + a2 * h01 + a3 * h02 - b1 * h03 - b2 * h04 + denorm;
                h02 = h01; 
                h01 = t0;
                h04 = h03; 
                h03 = sample;
            }


            pitchBuffer[buffer0 + 0] = sample; // write after reading it to avoid clicks

            sample = ren0;

            if (Filter.Value && pitch < 1.0)
            {
                t0 = sample; 
                sample = a1 * sample + a2 * h01 + a3 * h02 - b1 * h03 - b2 * h04 + denorm;
                h02 = h01; 
                h01 = t0;
                h04 = h03; 
                h03 = sample;
            }

            //spl0 += os0 * drymix;
            //spl1 += os1 * drymix;

            if ((buffer0 += 1) >= (bufloc + buffersize)) buffer0 -= buffersize;

            return sample;
        }
    }
}
