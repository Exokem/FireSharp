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

		private readonly Geometry Pause = Geometry.Parse("F0 M0,0 L6,0 L6,20 L0,20 ZM11,0 L17,0 L17,20 L11,20 Z");
		private readonly Geometry Play = Geometry.Parse("F0 M0,0 L0,20 L17,10 Z");

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
