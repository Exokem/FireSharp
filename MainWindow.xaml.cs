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

namespace SharpFire
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

			DataContext = this;

			// SizeChanged += (sender, args) =>
			// {
			// 	GridView view = XSelect.View as GridView;
			//
			// 	ScrollBarVisibility sv = (ScrollBarVisibility) XSelect.GetValue(ScrollViewer.VerticalScrollBarVisibilityProperty);
			//
			// 	var modifier = sv == ScrollBarVisibility.Visible ? SystemParameters.VerticalScrollBarWidth : 0;
			//
			// 	var workingWidth = XSelect.ActualWidth - modifier; // take into account vertical scrollbar
			// 	var col1 = 0.50;
			// 	var col2 = 0.50;
			//
			//
			// 	view.Columns[0].Width = workingWidth * col1;
			// 	view.Columns[1].Width = workingWidth * col2;
			// };
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

		public static void AddClickHandler(Path instance, RoutedEventHandler handler)
		{
			instance.AddHandler(ButtonBase.ClickEvent, handler);
		}

		public static void RemoveClickHandler(Path instance, RoutedEventHandler handler)
		{
			instance.RemoveHandler(ButtonBase.ClickEvent, handler);
		}
	}
}
