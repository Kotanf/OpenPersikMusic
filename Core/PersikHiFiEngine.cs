using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace PersikMusic.Core
{
    public class PersikHiFiEngine : ISampleProvider
    {
        private readonly ISampleProvider _source;
        public WaveFormat WaveFormat => _source.WaveFormat;

        // --- ПАРАМЕТРЫ МАСТЕРИНГА ---
        private const float MasterGain = 1.12f;
        private const float Ceiling = 0.95f;

        // --- СОСТОЯНИЯ ФИЛЬТРОВ (5 полос) ---
        private float[] _lpL = new float[5], _lpR = new float[5];

        // --- ДИНАМИКА (Компрессия) ---
        private float[] _envL = new float[5], _envR = new float[5];
        private readonly float[] _thresholds = { 0.6f, 0.7f, 0.75f, 0.7f, 0.6f };
        private readonly float[] _ratios = { 1.8f, 1.4f, 1.2f, 1.3f, 1.5f };

        // --- ПСИХОАКУСТИКА ---
        private float _dcL, _dcR;
        private float _prevL, _prevR;

        public PersikHiFiEngine(ISampleProvider source)
        {
            _source = source;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int read = _source.Read(buffer, offset, count);
            if (read == 0) return 0;

            int channels = WaveFormat.Channels;

            for (int i = 0; i < read; i += channels)
            {
                float rawL = buffer[offset + i];
                float rawR = (channels > 1) ? buffer[offset + i + 1] : rawL;

                // 1. DC OFFSET REMOVAL (Чистка инфразвука)
                _dcL += 0.01f * (rawL - _dcL);
                _dcR += 0.01f * (rawR - _dcR);
                float curL = rawL - _dcL;
                float curR = rawR - _dcR;

                // 2. MULTIBAND DECOMPOSITION (5 дорожек)
                // Полосы: Sub, Low-Mid, Mid, Presence, Air
                float[] bandsL = SplitBands(curL, _lpL);
                float[] bandsR = SplitBands(curR, _lpR);

                float finalL = 0, finalR = 0;

                for (int b = 0; b < 5; b++)
                {
                    float bL = bandsL[b];
                    float bR = bandsR[b];

                    // 3. ADAPTIVE COMPRESSION (Для каждой дорожки)
                    float rmsL = Math.Abs(bL);
                    float rmsR = Math.Abs(bR);
                    _envL[b] = _envL[b] < rmsL ? Smooth(_envL[b], rmsL, 0.1f) : Smooth(_envL[b], rmsL, 0.001f);
                    _envR[b] = _envR[b] < rmsR ? Smooth(_envR[b], rmsR, 0.1f) : Smooth(_envR[b], rmsR, 0.001f);

                    bL = ApplyCompression(bL, _envL[b], b);
                    bR = ApplyCompression(bR, _envR[b], b);

                    // 4. FREQUENCY STEREO IMAGING
                    float mid = (bL + bR) * 0.5f;
                    float side = (bL - bR) * 0.5f;

                    // Умное расширение: бас собран, верх разнесен
                    float width = b switch
                    {
                        0 => 0.95f, // Sub: почти моно для плотности
                        1 => 1.05f, // Low-Mid: легкий объем
                        2 => 1.15f, // Mid: гитары
                        3 => 1.30f, // Presence: вокал и детальность
                        4 => 1.45f, // Air: супер-стерео
                        _ => 1.0f
                    };

                    side *= width;
                    bL = mid + side;
                    bR = mid - side;

                    finalL += bL;
                    finalR += bR;
                }

                // 5. TRANSIENT RECOVERY (Возврат четкости)
                float attackL = finalL - _prevL;
                float attackR = finalR - _prevR;
                _prevL = finalL; _prevR = finalR;
                finalL += attackL * 0.15f;
                finalR += attackR * 0.15f;

                // 6. ANALOG WARMTH (Мягкое насыщение)
                finalL = (float)Math.Tanh(finalL * 1.05f);
                finalR = (float)Math.Tanh(finalR * 1.05f);

                // 7. FINAL MASTERING LIMITER
                buffer[offset + i] = PushLimit(finalL * MasterGain);
                if (channels > 1)
                    buffer[offset + i + 1] = PushLimit(finalR * MasterGain);
            }

            return read;
        }

        private float[] SplitBands(float sample, float[] states)
        {
            // Упрощенный каскадный фильтр (Crossover)
            float[] bands = new float[5];
            float s = sample;

            float f1 = 0.08f; // 200Hz
            float f2 = 0.20f; // 1kHz
            float f3 = 0.50f; // 5kHz
            float f4 = 0.80f; // 12kHz

            states[0] += f1 * (s - states[0]);
            bands[0] = states[0]; // SUB

            states[1] += f2 * (s - states[1]);
            bands[1] = states[1] - states[0]; // LOW-MID

            states[2] += f3 * (s - states[2]);
            bands[2] = states[2] - states[1]; // MID

            states[3] += f4 * (s - states[3]);
            bands[3] = states[3] - states[2]; // PRESENCE

            bands[4] = s - states[3]; // AIR

            return bands;
        }

        private float ApplyCompression(float sample, float env, int band)
        {
            float thresh = _thresholds[band];
            if (env <= thresh) return sample;

            float gainReduction = 1.0f - (env - thresh) * (1.0f - 1.0f / _ratios[band]);
            return sample * Math.Max(0.5f, gainReduction);
        }

        private float Smooth(float cur, float target, float t)
            => cur + t * (target - cur);

        private float PushLimit(float x)
        {
            float a = Math.Abs(x);
            if (a <= 0.88f) return x;

            // Look-ahead soft-knee approximation
            float over = a - 0.88f;
            float res = 0.88f + (Ceiling - 0.88f) * (float)Math.Tanh(over * 2.2f);
            return x > 0 ? res : -res;
        }
    }
}