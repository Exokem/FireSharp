using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

		public static bool Saved => _casette == null || _casette.Saved;

		public static void AttachInstance(SharpWindow instance)
		{
			if (_attached)
				throw new InvalidOperationException("Cannot attach multiple state instances");

			_instance = instance;
			_attached = true;
		}

		internal enum CloseAction
		{
			SAVE, CANCEL, IGNORE
		}

		internal static CloseAction RequestSave()
		{
			MessageBoxResult result = MessageBox.Show(_instance,
				"The current casette is unsaved. Save the current casette?", "Unsaved Casette",
				MessageBoxButton.YesNoCancel);

			if (result == MessageBoxResult.Yes)
				return CloseAction.SAVE;
			else if (result == MessageBoxResult.Cancel)
				return CloseAction.CANCEL;
			else return CloseAction.IGNORE;
		}

		internal static void SaveOnClose(CancelEventArgs eventArgs = null)
		{
			if (!Saved)
			{
				CloseAction result = RequestSave();

				if (eventArgs != null && result == CloseAction.CANCEL)
					eventArgs.Cancel = true;
				else if (result == CloseAction.SAVE)
				{
					bool? saved = DialogProvider.RequestCasetteSavePath();

					if (saved.HasValue && !saved.Value && eventArgs != null)
						eventArgs.Cancel = true;
				}
			}
		}

		internal static void LoadCasette(Casette casette)
		{
			_casette = casette;
			_instance.UpdateTrackList(_casette.Queue);
		}

		internal static void ReceiveAudioPaths(string[] audioPaths)
		{
			if (_casette == null)
				_casette = Casette.Empty();

			_casette.ReceiveAudioPaths(audioPaths);
			_instance.UpdateTrackList(_casette.Queue);
		}

		internal static void ReceiveCasettePath(string casettePath)
		{
			if (_casette is {Saved: false})
			{
				switch (RequestSave())
				{
					case CloseAction.SAVE: DialogProvider.RequestCasetteSavePath();
						break;
					case CloseAction.CANCEL: return;
				}
			}

			else if (_casette != null && casettePath == _casette.Path)
				return;

			LoadCasette(Casette.Import(casettePath));
		}

		internal static void EjectCasette()
		{
			_casette = null;
			_instance.UpdateTrackList(new List<Track>());
		}

		internal static void SaveEjectedCasette(string casettePath)
		{
			if (_casette == null)
				throw new InvalidOperationException("Cannot save null casette");

			if (File.Exists(casettePath))
				File.Delete(casettePath);

			FileStream saveStream = File.Create(casettePath);
			StreamWriter saveWriter = new StreamWriter(saveStream);

			foreach (Track track in _casette.Queue)
				saveWriter.WriteLine(track.Path);

			saveWriter.Dispose();

			EjectCasette();
		}
	}

	internal static class DialogProvider
	{
		private static readonly OpenFileDialog _audioDialog = new() {Filter = "MP3 Files (*.mp3)|*.mp3|M4A Files (*.m4a)|*.m4a"};
		private static readonly SaveFileDialog _saveCasetteDialog = new() {Filter = "FireSharp Casettes (*.cst)|*.cst"};
		private static readonly OpenFileDialog _loadCasetteDialog = new() {Filter = "FireSharp Casettes (*.cst)|*.cst"};

#nullable enable
		internal static void RequestAudioPaths()
		{
			_audioDialog.Multiselect = true;
			bool? result = _audioDialog.ShowDialog();

			if (result.HasValue && result.Value)
				State.ReceiveAudioPaths(_audioDialog.FileNames);
		}

		internal static void RequestCasetteLoadPath()
		{
			bool? result = _loadCasetteDialog.ShowDialog();

			if (result.HasValue && result.Value)
				State.ReceiveCasettePath(_loadCasetteDialog.FileName);
		}

		internal static bool PromptSave()
		{
			if (State.Saved)
				return true;

			State.CloseAction action = State.RequestSave();

			if (action == State.CloseAction.SAVE)
			{
				bool? result = RequestCasetteSavePath();
				if (result.HasValue)
					return result.Value;
			}
			else if (action == State.CloseAction.CANCEL)
				return false;

			return true;
		}

		internal static bool? RequestCasetteSavePath()
		{
			if (State.Saved)
			{
				State.EjectCasette();
				return false;
			}

			bool? result = _saveCasetteDialog.ShowDialog();

			if (result.HasValue && result.Value)
				State.SaveEjectedCasette(_saveCasetteDialog.FileName);

			return result;
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

			casette.ReceiveAudioPaths(File.ReadAllLines(path), true);
			casette.Path = path;

			return casette;
		}

		public static Casette Empty() => new Casette();

		internal void ReceiveAudioPaths(string[] audioPaths, bool importOperation = false)
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

			Saved = importOperation;
		}

		public string Path { get; private set; }
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
