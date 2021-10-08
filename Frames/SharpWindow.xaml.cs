using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FireSharp.State;
using Microsoft.Win32;
using NAudio.Wave;

namespace FireSharp.Frames
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class SharpWindow : Window
	{
		private static readonly WaveOutEvent _audion = new();
		private static bool _stateFixed;

		// public static ObservableCollection<Entry> Entries = new ObservableCollection<Entry>();

		public SharpWindow()
		{
			InitializeComponent();

			_loader = new LoadPrompt(this);

			// Ensure loader closes with main window

			Closed += (sender, args) => _loader.Close();
			Closing += (sender, args) => State.State.SaveOnClose(args);

			State.State.AttachInstance(this);
		}

		public void UpdateTrackList(List<Track> stateSet)
		{
			// TrackList.ItemsSource = stateSet;

			UIElement trackLabel = TrackGrid.Children[0];
			UIElement titleLabel = TrackGrid.Children[1];
			UIElement authorLabel = TrackGrid.Children[2];
			UIElement durationLabel = TrackGrid.Children[3];

			TrackGrid.RowDefinitions.Clear();
			TrackGrid.Children.Clear();

			TrackGrid.RowDefinitions.Add(new RowDefinition(){Height = GridLength.Auto});

			TrackGrid.Children.Add(trackLabel);
			TrackGrid.Children.Add(titleLabel);
			TrackGrid.Children.Add(authorLabel);
			TrackGrid.Children.Add(durationLabel);

			Style buttonStyle = Resources["IndirectControl"] as Style;
			Thickness borderThickness = new Thickness(0, 0, 0, 0.5);

			foreach (Track track in stateSet)
			{
				TrackGrid.RowDefinitions.Add(new RowDefinition(){Height = GridLength.Auto});

				Button index = new() {Content = $"{track.Index}", Style = buttonStyle, BorderThickness = borderThickness};
				index.SetValue(Grid.RowProperty, track.Index);
				index.SetValue(Grid.ColumnProperty, 0);

				Button title = new() {Content = $"{track.Title}", Style = buttonStyle, BorderThickness = borderThickness};
				Grid.SetRow(title, track.Index);
				Grid.SetColumn(title, 1);

				Button author = new() {Content = $"{track.Author}", Style = buttonStyle, BorderThickness = borderThickness};
				Grid.SetRow(author, track.Index);
				Grid.SetColumn(author, 2);

				Button duration = new() {Content = $"{track.Duration}", Style = buttonStyle, BorderThickness = borderThickness};
				Grid.SetRow(duration, track.Index);
				Grid.SetColumn(duration, 3);

				TrackGrid.Children.Add(index);
				TrackGrid.Children.Add(title);
				TrackGrid.Children.Add(author);
				TrackGrid.Children.Add(duration);
			}
		}

		private readonly LoadPrompt _loader;

		private void ChangeState(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			if (button == null)
				return;

			if (!_stateFixed)
				return;

			if (_audion.PlaybackState == PlaybackState.Playing)
				_audion.Pause();
			else
				_audion.Play();
		}

		private void Import(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new() {Filter = "MP3 Files (*.mp3)|*.mp3"};

			bool? result = dialog.ShowDialog();

			if (result.HasValue && result.Value)
			{
				Debug.WriteLine(dialog.FileName);

				Uri uri = new(dialog.FileName, UriKind.Absolute);

				WaveStream fileStream = new AudioFileReader(dialog.FileName);

				if (_audion.PlaybackState == PlaybackState.Playing)
				{
					_audion.Pause();
					_audion.Stop();
				}

				_audion.Init(new WaveChannel32(fileStream));
				_stateFixed = true;

				// Thread progressManager = new Thread(() =>
				// {
				// 	while (true)
				// 	{
				// 		Debug.WriteLine($"{fileStream.CurrentTime.Ticks} / {fileStream.TotalTime.Ticks}");
				// 		Progress.Dispatcher.Invoke(() =>
				// 			Progress.Value = fileStream.CurrentTime.Divide(fileStream.TotalTime));
				//
				// 	}
				// });
				//
				// progressManager.Start();

				_audion.Play();
			}
		}

		private readonly Geometry _pause = Geometry.Parse("F0 M0,0 L6,0 L6,20 L0,20 ZM11,0 L17,0 L17,20 L11,20 Z");
		private readonly Geometry _play = Geometry.Parse("F0 M0,0 L0,20 L17,10 Z");

		private bool _statePaused = true;

		private static readonly Brush CALM_RED = new SolidColorBrush(Color.FromRgb(238, 18, 37));

		public void UpdateStateControlPath()
		{
			StateControlPath.Data = State.State.Paused || State.State.Stopped ? _play : _pause;
		}

		private void StateControlSwitch(object sender, RoutedEventArgs e)
		{
			State.State.PauseSwitch();
			UpdateStateControlPath();
		}

		private static void Recolor(Path path, MouseEventArgs e, Brush stroke, Brush fill = null)
		{
			path.Stroke = stroke;
			path.Fill = fill ?? Brushes.Transparent;
		}

		private void StateControlEnter(object sender, MouseEventArgs e) => Recolor(StateControlPath, e, CALM_RED);
		private void StateControlExit(object sender, MouseEventArgs e) => Recolor(StateControlPath, e, Brushes.DarkGray);

		private void NextEnter(object sender, MouseEventArgs e)
		{
			Recolor(NextControlPath, e, CALM_RED);
		}

		private void NextExit(object sender, MouseEventArgs e) => Recolor(NextControlPath, e, Brushes.DarkGray);

		private void PrevEnter(object sender, MouseEventArgs e) => Recolor(PrevControlPath, e, CALM_RED);
		private void PrevExit(object sender, MouseEventArgs e) => Recolor(PrevControlPath, e, Brushes.DarkGray);

		private static void PathControlPress(Path path, object sender, MouseButtonEventArgs e)
		{
			Button button = sender as Button;

			if (button == null)
				return;

			if (e.LeftButton == MouseButtonState.Pressed)
				Recolor(path, e, CALM_RED, CALM_RED);
			else if (button.IsMouseOver)
				Recolor(path, e, CALM_RED);
			else
				Recolor(path, e, Brushes.DarkGray);
		}

		private void StateControlPress(object sender, MouseButtonEventArgs e) => PathControlPress(StateControlPath, sender, e);

		private void NextControlPress(object sender, MouseButtonEventArgs e) => PathControlPress(NextControlPath, sender, e);

		private void PrevControlPress(object sender, MouseButtonEventArgs e) => PathControlPress(PrevControlPath, sender, e);

		private void Load(object sender, RoutedEventArgs e)
		{
			_loader.Owner = this;
			_loader.ShowDialog();
		}

		private void Eject(object sender, RoutedEventArgs e)
		{
			if (DialogProvider.PromptSave())
				State.State.EjectCasette();
		}
	}
}
