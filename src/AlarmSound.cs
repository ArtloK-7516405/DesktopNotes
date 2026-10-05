using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;

namespace DesktopNotes
{
    public static class AlarmSound
    {
        static SoundPlayer sp;
        static MediaPlayer mp;

        public static void Start()
        {
            Stop();
            var f = Store.Data.Settings.Sound;
            try
            {
                if (!string.IsNullOrEmpty(f) && File.Exists(f))
                {
                    mp = new MediaPlayer();
                    mp.MediaEnded += (s, e) => { mp.Position = TimeSpan.Zero; mp.Play(); };
                    mp.Open(new Uri(f));
                    mp.Play();
                    return;
                }
            }
            catch { }
            sp = new SoundPlayer(new MemoryStream(Wav()));
            sp.PlayLooping();
        }

        public static void Stop()
        {
            sp?.Stop(); sp = null;
            if (mp != null) { mp.Stop(); mp.Close(); mp = null; }
        }

        // Встроенный сигнал: три коротких гудка + пауза, генерируется на лету.
        static byte[] Wav()
        {
            const int sr = 22050;
            var data = new List<short>();
            void Tone(double hz, double sec)
            {
                int n = (int)(sr * sec);
                for (int i = 0; i < n; i++)
                {
                    double env = Math.Min(1, Math.Min(i, n - i) / 200.0);
                    data.Add((short)(Math.Sin(2 * Math.PI * hz * i / sr) * 0.6 * env * short.MaxValue));
                }
            }
            void Gap(double sec) { for (int i = 0; i < (int)(sr * sec); i++) data.Add(0); }
            for (int k = 0; k < 3; k++) { Tone(1000, .18); Gap(.08); }
            Gap(.6);

            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            int len = data.Count * 2;
            w.Write("RIFF".ToCharArray()); w.Write(36 + len); w.Write("WAVE".ToCharArray());
            w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(sr); w.Write(sr * 2); w.Write((short)2); w.Write((short)16);
            w.Write("data".ToCharArray()); w.Write(len);
            foreach (var s in data) w.Write(s);
            return ms.ToArray();
        }
    }
}
