using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NAudio.Wave;

namespace FireSharp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static WaveOutEvent _audion = new WaveOutEvent();
		private static bool _stateFixed = false;

		// public static ObservableCollection<Entry> Entries = new ObservableCollection<Entry>();

		public MainWindow()
		{
			InitializeComponent();
		}

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
				

				Thread progressManager = new Thread(() =>
				{
					while (true)
					{
						Debug.WriteLine($"{fileStream.CurrentTime.Ticks} / {fileStream.TotalTime.Ticks}");
						Progress.Dispatcher.Invoke(() =>
							Progress.Value = fileStream.CurrentTime.Divide(fileStream.TotalTime));
						
					}
				});

				progressManager.Start();

				_audion.Play();
			}
		}

		private readonly Geometry _pause = Geometry.Parse("F0 M0,0 L6,0 L6,20 L0,20 ZM11,0 L17,0 L17,20 L11,20 Z");
		private readonly Geometry _play = Geometry.Parse("F0 M0,0 L0,20 L17,10 Z");

		private bool _statePaused = true;

		private static readonly Brush CALM_RED = new SolidColorBrush(Color.FromRgb(238, 18, 37));

		private void StateControlSwitch(object sender, RoutedEventArgs e)
		{
			StateControlPath.Data = _statePaused ? _pause : _play;
			_statePaused = !_statePaused;
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

		private void StateControlPress(object sender, MouseButtonEventArgs e) =>
			PathControlPress(StateControlPath, sender, e);

		private void NextControlPress(object sender, MouseButtonEventArgs e) =>
			PathControlPress(NextControlPath, sender, e);
		
		private void PrevControlPress(object sender, MouseButtonEventArgs e) =>
			PathControlPress(PrevControlPath, sender, e);
	}
}
