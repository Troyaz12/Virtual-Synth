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
        private double volume = 0.25;

        private WaveOutEvent waveOut;
        private SynthWaveProvider synthProvider;

        private Dictionary<string, double> noteFrequencies;
        private List<ActiveNote> activeNotes = new List<ActiveNote>();

        public MainWindow()
        {
            InitializeComponent();

            // Setup notes
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

            // Setup audio
            synthProvider = new SynthWaveProvider(activeNotes, volume);
            waveOut = new WaveOutEvent();
            waveOut.Init(synthProvider);
            waveOut.Play();
        }

        // Add note to active list
        private void Key_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var note = btn?.Content.ToString();
            if (note == null || !noteFrequencies.ContainsKey(note)) return;

            string waveform = ((ComboBoxItem)WaveformSelector.SelectedItem)?.Content.ToString() ?? "Sine";

            activeNotes.Add(new ActiveNote
            {
                Frequency = noteFrequencies[note],
                Phase = 0,
                Duration = 0.5, // half-second notes
                Waveform = waveform
            });
        }

        // Update volume
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volume = e.NewValue / 100.0;
            if (synthProvider != null)
                synthProvider.SetVolume(volume);
        }

        // Dispose audio on close
        protected override void OnClosed(EventArgs e)
        {
            waveOut.Dispose();
            base.OnClosed(e);
        }

        // Active note class
        private class ActiveNote
        {
            public double Frequency;
            public double Phase;
            public double Duration;
            public string Waveform;
        }

        // Polyphonic WaveProvider
        private class SynthWaveProvider : WaveProvider16
        {
            private List<ActiveNote> activeNotes;
            private double volume;

            public SynthWaveProvider(List<ActiveNote> notes, double vol)
            {
                activeNotes = notes;
                volume = vol;
            }

            public void SetVolume(double vol)
            {
                volume = vol;
            }

            public override int Read(short[] buffer, int offset, int sampleCount)
            {
                for (int n = 0; n < sampleCount; n++)
                {
                    double sampleValue = 0;

                    for (int i = activeNotes.Count - 1; i >= 0; i--)
                    {
                        var note = activeNotes[i];
                        double val = note.Waveform switch
                        {
                            "Sine" => Math.Sin(2 * Math.PI * note.Frequency * note.Phase),
                            "Square" => Math.Sign(Math.Sin(2 * Math.PI * note.Frequency * note.Phase)),
                            "Saw" => 2.0 * (note.Frequency * note.Phase - Math.Floor(note.Frequency * note.Phase)) - 1.0,
                            _ => Math.Sin(2 * Math.PI * note.Frequency * note.Phase)
                        };
                        sampleValue += val;

                        note.Phase += 1.0 / WaveFormat.SampleRate;
                        note.Duration -= 1.0 / WaveFormat.SampleRate;

                        if (note.Duration <= 0)
                            activeNotes.RemoveAt(i);
                    }

                    // Prevent clipping
                    sampleValue = Math.Max(-1.0, Math.Min(1.0, sampleValue));

                    buffer[offset + n] = (short)(sampleValue * volume * short.MaxValue);
                }
                return sampleCount;
            }
        }
    }
}
