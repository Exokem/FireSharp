using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FireSharp.Frames
{
	/// <summary>
	/// Interaction logic for LoadPrompt.xaml
	/// </summary>
	public partial class LoadPrompt : Window
	{
		public LoadPrompt()
		{
			InitializeComponent();
		}

		private void StandardLoading(object sender, RoutedEventArgs e)
		{
			// open a single selector for casette files
		}
		
		private void EmptyLoading(object sender, RoutedEventArgs e)
		{
			// quietly eject the current casette and initialize a new one (very simple)
		}
		
		private void TrackInject(object sender, RoutedEventArgs e)
		{
			// open a multi selector for audio files
		}
	}
}
