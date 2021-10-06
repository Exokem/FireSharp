using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using File = System.IO.File;

namespace FireSharp.State
{
	static class State
	{


		
	}

	class Casette
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

			string[] trackPaths = File.ReadAllLines(path);

			foreach (string trackPath in trackPaths)
				casette._queue.Add(Track.Import(trackPath));

			return casette;
		}

		private readonly List<Track> _queue = new List<Track>();

		public Track this[int ix] => _queue[ix];
	}

	class Track
	{
		private Track(string title, string author, TimeSpan duration)
		{
			Title = title;
			Author = author;
			Duration = duration;
		}

		public static Track Import(string path)
		{
			TagLib.File audioFile = TagLib.File.Create(path);
			TagLib.Tag tag = audioFile.Tag;

			return new(tag.Title, tag.FirstAlbumArtist, audioFile.Properties.Duration);
		}

		public string Title { get; }
		public string Author { get; }
		public TimeSpan Duration { get; }
	}
}
