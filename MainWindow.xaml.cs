using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

		public MainWindow()
		{
			InitializeComponent();
		}

		private void ChangeState(object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			if (button == null)
				return;


		}

		private void Import(object sender, RoutedEventArgs e)
		{
			OpenFileDialog dialog = new OpenFileDialog();

			dialog.Filter = "MP3 Files (*.mp3)|*.mp3";

			bool? result = dialog.ShowDialog();

			if (result.HasValue && result.Value)
			{
				Debug.WriteLine(dialog.FileName);

				Uri uri = new(dialog.FileName, UriKind.Absolute);

				WaveStream fileStream = new AudioFileReader(dialog.FileName);

				_audion.Init(new WaveChannel32(fileStream));
				_audion.Play();
			}
		}
	}
}
