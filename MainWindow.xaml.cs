using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MinimalMusicPlayer
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Track> tracks;
        private int currentTrackIndex = 0;
        private bool isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadTracks();
        }

        private void LoadTracks()
        {
            tracks = new ObservableCollection<Track>
            {
                new Track { Index = 1, Name = "Nuvole Bianche", Artist = "Einaudi", IsCurrent = true },
                new Track { Index = 2, Name = "Experience", Artist = "Einaudi", IsCurrent = false },
                new Track { Index = 3, Name = "Divenire", Artist = "Einaudi", IsCurrent = false },
                new Track { Index = 4, Name = "River Flows", Artist = "Yiruma", IsCurrent = false },
                new Track { Index = 5, Name = "Comptine", Artist = "Tiersen", IsCurrent = false },
                new Track { Index = 6, Name = "Clair de Lune", Artist = "Debussy", IsCurrent = false }
            };

            QueueListBox.ItemsSource = tracks;
            UpdatePlayerUI();
        }

        private void UpdatePlayerUI()
        {
            for (int i = 0; i < tracks.Count; i++)
            {
                tracks[i].IsCurrent = (i == currentTrackIndex);
            }
            
            QueueListBox.Items.Refresh();

            var currentTrack = tracks[currentTrackIndex];
            TxtTitle.Text = currentTrack.Name;
            TxtArtist.Text = currentTrack.Artist;
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            isPlaying = !isPlaying;
            if (isPlaying)
            {
                PlayIcon.Visibility = Visibility.Collapsed;
                PauseIcon.Visibility = Visibility.Visible;
            }
            else
            {
                PlayIcon.Visibility = Visibility.Visible;
                PauseIcon.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            currentTrackIndex = (currentTrackIndex + 1) % tracks.Count;
            UpdatePlayerUI();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            currentTrackIndex = (currentTrackIndex - 1 + tracks.Count) % tracks.Count;
            UpdatePlayerUI();
        }

        private void QueueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (QueueListBox.SelectedIndex != -1)
            {
                currentTrackIndex = QueueListBox.SelectedIndex;
                UpdatePlayerUI();
                QueueListBox.SelectedIndex = -1;
            }
        }
    }

    public class Track
    {
        public int Index { get; set; }
        public string DisplayIndex => Index.ToString();
        public string Name { get; set; }
        public string Artist { get; set; }
        public bool IsCurrent { get; set; }
    }
}
