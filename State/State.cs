using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FireSharp.Frames;
using Microsoft.Win32;
using File = System.IO.File;

namespace FireSharp.State
{
	public static class State
	{
		private static Casette _casette;
		private static SharpWindow _instance;

		private static bool _attached;

		public static void AttachInstance(SharpWindow instance)
		{
			if (_attached)
				throw new InvalidOperationException("Cannot attach multiple state instances");

			_instance = instance;
			_attached = true;
		}

		internal static void LoadCasette(string path)
		{
			// _casette = 
		}

		internal static void ReceiveAudioPaths(string[] audioPaths)
		{
			if (_casette == null)
				_casette = Casette.Empty();

			_casette.ReceiveAudioPaths(audioPaths);
			_instance.UpdateTrackList(_casette.Queue);
			// _instance.TrackList.ItemsSource = _casette.Queue;
			// _instance.TrackList.UpdateLayout();
		}
	}

	internal static class DialogProvider
	{
		private static readonly OpenFileDialog _audioDialog = new() {Filter = "MP3 Files (*.mp3)|*.mp3|M4A Files (*.m4a)|*.m4a"};
		private static readonly SaveFileDialog _newCasetteDialog = new() {Filter = "FireSharp Casettes (*.cst)|*.cst"};

#nullable enable
		internal static void RequestAudioPaths()
		{
			_audioDialog.Multiselect = true;
			bool? result = _audioDialog.ShowDialog();

			if (result.HasValue && result.Value)
				State.ReceiveAudioPaths(_audioDialog.FileNames);
		}
	}

	internal class Casette
	{
		private Casette()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path">The (selected) absolute path to the casette file to be imported.</param>
		/// <returns></returns>
		public static Casette Import(string path)
		{
			Casette casette = new();

			casette.ReceiveAudioPaths(File.ReadAllLines(path));

			return casette;
		}

		public static Casette Empty() => new Casette();

		internal void ReceiveAudioPaths(string[] audioPaths)
		{
			if (audioPaths == null || audioPaths.Length == 0)
				return;

			foreach (string trackPath in audioPaths)
			{
				if (0 < trackPath.Length)
				{
					Track track = Track.Import(trackPath);
					track.Index = Queue.Count + 1;
					Queue.Add(track);
				}
			}

			Saved = false;
		}

		public Track this[int ix] => Queue[ix];
		public bool Saved { get; private set; }
		internal List<Track> Queue { get; } = new List<Track>();
	}

	public class Track
	{
		private Track(string path, string title, string author, TimeSpan duration)
		{
			Path = path;

			Title = title;
			Author = author;
			Duration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds);
		}

		public static Track Import(string path)
		{
			TagLib.File audioFile = TagLib.File.Create(path);
			TagLib.Tag tag = audioFile.Tag;

			return new(path, tag.Title, tag.FirstAlbumArtist, audioFile.Properties.Duration);
		}

		public string Path { get; private set; }

		public int Index { get; internal set; }
		public string Title { get; }
		public string Author { get; }
		public TimeSpan Duration { get; }
	}
}
