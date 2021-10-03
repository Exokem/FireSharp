using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SharpFire.Event
{
	public partial class ResourceEvents : ResourceDictionary
	{
		private bool _paused = true;

		private readonly Geometry Pause = Geometry.Parse("F0 M0,0 L12,0 L12,40 L0,40 ZM23,0 L35,0 L35,40 L23,40 Z");
		private readonly Geometry Play = Geometry.Parse("F0 M0,0 L0,40 L35,20 Z");

		void State(object sender, MouseButtonEventArgs e)
		{
			Path path = sender as Path;

			if (path == null)
				return;

			path.Data = _paused ? Play : Pause;
			_paused = !_paused;
		}
	}
}
