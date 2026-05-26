// MainWindow.xaml.cs
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MinimalMusicPlayer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Track> tracks = new ObservableCollection<Track>();
        private int currentTrackIndex = -1;
        private bool isPlaying = false;
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private bool isSliderDragging = false;

        // Pure C# Entry Point bypassing implicit WPF App.xaml generation bugs
        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            MainWindow window = new MainWindow();
            app.Run(window);
        }

        public MainWindow()
        {
            InitializeComponent();
            QueueListBox.ItemsSource = tracks;
            
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += Timer_Tick;
            
            mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Audio Files (*.mp3;*.wav;*.flac;*.m4a)|*.mp3;*.wav;*.flac;*.m4a"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                int startIndex = tracks.Count;
                foreach (string filePath in openFileDialog.FileNames)
                {
                    var track = ExtractTrackInfo(filePath, tracks.Count + 1);
                    tracks.Add(track);
                }

                if (currentTrackIndex == -1 && tracks.Count > 0)
                {
                    LoadTrack(startIndex);
                }
            }
        }

        private Track ExtractTrackInfo(string filePath, int targetIndex)
        {
            var track = new Track
            {
                Index = targetIndex,
                FilePath = filePath,
                Name = Path.GetFileNameWithoutExtension(filePath),
                Artist = "Unknown Artist",
                IsCurrent = false
            };

            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    if (!string.IsNullOrEmpty(file.Tag.Title))
                        track.Name = file.Tag.Title;
                    
                    if (file.Tag.Performers.Length > 0)
                        track.Artist = string.Join(", ", file.Tag.Performers);
                }
            }
            catch
            {
                // Fallback to filename if metadata extraction fails
            }

            return track;
        }

        private void LoadTrack(int index)
        {
            if (index < 0 || index >= tracks.Count) return;

            currentTrackIndex = index;
            var currentTrack = tracks[currentTrackIndex];

            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].IsCurrent = (i == currentTrackIndex);
            }
            QueueListBox.Items.Refresh();

            TxtTitle.Text = currentTrack.Name;
            TxtArtist.Text = currentTrack.Artist;

            LoadCoverArt(currentTrack.FilePath);

            mediaPlayer.Open(new Uri(currentTrack.FilePath));
            
            if (isPlaying)
            {
                mediaPlayer.Play();
                timer.Start();
            }
            else
            {
                mediaPlayer.Play();
                mediaPlayer.Pause();
                UpdateTimelineInfo();
            }
        }

        private void LoadCoverArt(string filePath)
        {
            try
            {
                using (var file = TagLib.File.Create(filePath))
                {
                    if (file.Tag.Pictures.Length > 0)
                    {
                        var bin = file.Tag.Pictures[0].Data.Data;
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = new MemoryStream(bin);
                        bitmap.EndInit();

                        ImgCover.Source = bitmap;
                        DefaultCover.Visibility = Visibility.Collapsed;
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to default geometry if cover extraction fails
            }

            ImgCover.Source = null;
            DefaultCover.Visibility = Visibility.Visible;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (!isSliderDragging && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                TimelineSlider.Value = (mediaPlayer.Position.TotalSeconds / mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds) * 100;
                TxtCurrentTime.Text = mediaPlayer.Position.ToString(@"m\:ss");
                TxtTotalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
            }
        }

        private void UpdateTimelineInfo()
        {
            var dispatcherTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            dispatcherTimer.Tick += (s, a) =>
            {
                dispatcherTimer.Stop();
                if (mediaPlayer.NaturalDuration.HasTimeSpan)
                {
                    TxtTotalTime.Text = mediaPlayer.NaturalDuration.TimeSpan.ToString(@"m\:ss");
                }
            };
            dispatcherTimer.Start();
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (currentTrackIndex == -1) return;

            isPlaying = !isPlaying;
            if (isPlaying)
            {
                PlayIcon.Visibility = Visibility.Collapsed;
                PauseIcon.Visibility = Visibility.Visible;
                mediaPlayer.Play();
                timer.Start();
            }
            else
            {
                PlayIcon.Visibility = Visibility.Visible;
                PauseIcon.Visibility = Visibility.Collapsed;
                mediaPlayer.Pause();
                timer.Stop();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (tracks.Count == 0) return;
            int nextIndex = (currentTrackIndex + 1) % tracks.Count;
            LoadTrack(nextIndex);
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (tracks.Count == 0) return;
            int prevIndex = (currentTrackIndex - 1 + tracks.Count) % tracks.Count;
            LoadTrack(prevIndex);
        }

        private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
        {
            BtnNext_Click(this, new RoutedEventArgs());
        }

        private void TimelineSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (currentTrackIndex != -1 && mediaPlayer.NaturalDuration.HasTimeSpan)
            {
                double targetSeconds = (TimelineSlider.Value / 100) * mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                mediaPlayer.Position = TimeSpan.FromSeconds(targetSeconds);
            }
        }

        private void QueueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QueueListBox.SelectedIndex != -1)
            {
                LoadTrack(QueueListBox.SelectedIndex);
                QueueListBox.SelectedIndex = -1;
            }
        }
    }

    public class Track
    {
        public int Index { get; set; }
        public string DisplayIndex => Index.ToString();
        public required string FilePath { get; set; }
        public required string Name { get; set; }
        public required string Artist { get; set; }
        public bool IsCurrent { get; set; }
    }
}
