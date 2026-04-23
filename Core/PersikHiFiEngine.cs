using NAudio.Wave;
using System;

namespace PersikMusic.Core
{
    public class PersikHiFiEngine : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public WaveFormat WaveFormat => _source.WaveFormat;

        private float _currentGain = 1.0f;

        // Noise tracking (очень мягкий)
        private float _noiseEstimate = 0.0003f;
        private readonly float _noiseAdapt;

        // Limiter
        private readonly float _attack;
        private readonly float _release;
        private readonly float _targetGain = 1.4f; // громкость выше

        // HF tracking
        private float _hfTrack = 0f;

        public PersikHiFiEngine(ISampleProvider source)
        {
            _source = source;
            float sr = source.WaveFormat.SampleRate;

            _attack = (float)Math.Exp(-1.0 / (0.002 * sr));
            _release = (float)Math.Exp(-1.0 / (0.080 * sr));
            _noiseAdapt = (float)Math.Exp(-1.0 / (0.5 * sr));
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            if (read == 0) return 0;

            int ch = _source.WaveFormat.Channels;

            for (int i = 0; i < read; i += ch)
            {
                float L = buffer[offset + i];
                float R = (ch > 1 && i + 1 < read) ? buffer[offset + i + 1] : L;

                float level = Math.Max(Math.Abs(L), Math.Abs(R));

                // =====================================================
                // 1. VERY SOFT NOISE REDUCTION (НЕ портит звук)
                // =====================================================
                if (level < _noiseEstimate * 1.3f)
                {
                    _noiseEstimate = _noiseEstimate * _noiseAdapt +
                                     level * (1 - _noiseAdapt);
                }

                float threshold = _noiseEstimate * 1.8f;
                float noiseFactor = 1f;

                if (level < threshold)
                {
                    float r = level / threshold;
                    noiseFactor = 0.7f + 0.3f * r; // мягкое подавление
                }

                L *= noiseFactor;
                R *= noiseFactor;

                // =====================================================
                // 2. LIGHT DE-HISS (без "мыла")
                // =====================================================
                float hf = ((L + R) * 0.5f) - _hfTrack;
                _hfTrack += hf * 0.05f;

                float brightness = Math.Abs(hf);
                float hissCut = 1.0f - Math.Clamp(brightness * 1.0f, 0f, 0.2f);

                L *= hissCut;
                R *= hissCut;

                // =====================================================
                // 3. MID/SIDE HI-FI
                // =====================================================
                float mid = (L + R) * 0.5f;
                float side = (L - R) * 0.5f;

                mid = AddWarmth(mid);

                mid *= 0.97f;
                side *= 1.15f;

                L = mid + side;
                R = mid - side;

                // =====================================================
                // 4. LOUDNESS (makeup gain)
                // =====================================================
                float makeup = 1.15f;
                L *= makeup;
                R *= makeup;

                // =====================================================
                // 5. TRUE PEAK LIMITER (без клиппинга)
                // =====================================================
                float peak = Math.Max(Math.Abs(L), Math.Abs(R)) + 1e-6f;

                float desired = _targetGain;

                if (peak * desired > 0.98f)
                    desired = 0.98f / peak;

                if (desired < _currentGain)
                    _currentGain = desired + _attack * (_currentGain - desired);
                else
                    _currentGain = desired + _release * (_currentGain - desired);

                L *= _currentGain;
                R *= _currentGain;

                // =====================================================
                // 6. VERY SOFT CLIP (на всякий случай)
                // =====================================================
                L = SoftClip(L);
                R = SoftClip(R);

                // финальная защита
                L = Math.Clamp(L, -0.99f, 0.99f);
                R = Math.Clamp(R, -0.99f, 0.99f);

                buffer[offset + i] = L;
                if (ch > 1 && i + 1 < read)
                    buffer[offset + i + 1] = R;
            }

            return read;
        }

        private float AddWarmth(float x)
        {
            if (Math.Abs(x) < 0.3f)
                return x;

            return x * (1.0f - (x * x * 0.12f));
        }

        private float SoftClip(float x)
        {
            float a = Math.Abs(x);

            if (a < 0.9f)
                return x;

            float y = 0.9f + (a - 0.9f) / (1.0f + (a - 0.9f) * 5.0f);
            return x > 0 ? y : -y;
        }
    }
}