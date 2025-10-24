using NAudio.Wave;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Synth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WaveOutEvent waveOut;
        private readonly BufferedWaveProvider waveProvider;
        private readonly WaveFormat waveFormat;
        private readonly Dictionary<string, double> noteFrequencies;
        private double volume = 0.25;

        public MainWindow()
        {
            InitializeComponent();

            // Define sample rate & audio format
            waveFormat = new WaveFormat(44100, 1);
            waveOut = new WaveOutEvent();
            waveProvider = new BufferedWaveProvider(waveFormat);
            waveOut.Init(waveProvider);
            waveOut.Play();

            // Define frequencies for notes
            noteFrequencies = new Dictionary<string, double>
            {
                {"C", 261.63},
                {"D", 293.66},
                {"E", 329.63},
                {"F", 349.23},
                {"G", 392.00},
                {"A", 440.00},
                {"B", 493.88}
            };

        }

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var note = btn?.Content.ToString();
            if (note == null || !noteFrequencies.ContainsKey(note)) return;

            PlayTone(noteFrequencies[note], 0.3); // 0.3 seconds
        }

        private void PlayTone(double frequency, double durationSeconds)
        {
            int sampleRate = waveFormat.SampleRate;
            int sampleCount = (int)(sampleRate * durationSeconds);
            byte[] buffer = new byte[sampleCount * 2]; // 16-bit samples

            // Get selected waveform
            string waveform = ((ComboBoxItem)WaveformSelector.SelectedItem).Content.ToString();

            for (int n = 0; n < sampleCount; n++)
            {
                double t = (double)n / sampleRate;
                double sampleValue = waveform switch
                {
                    "Sine" => Math.Sin(2 * Math.PI * frequency * t),
                    "Square" => Math.Sign(Math.Sin(2 * Math.PI * frequency * t)),
                    "Saw" => 2.0 * (t * frequency - Math.Floor(t * frequency)) - 1.0,
                    _ => Math.Sin(2 * Math.PI * frequency * t)
                };

                short sample = (short)(sampleValue * volume * short.MaxValue); // volume = 0–1
                buffer[2 * n] = (byte)(sample & 0xFF);
                buffer[2 * n + 1] = (byte)((sample >> 8) & 0xFF);
            }

            waveProvider.AddSamples(buffer, 0, buffer.Length);
        }
        protected override void OnClosed(EventArgs e)
        {
            waveOut.Dispose();
            base.OnClosed(e);
        }
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volume = e.NewValue / 100.0; // convert 0–100 to 0–1
        }
    }
}
