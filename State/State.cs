using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FireSharp.Frames;
using Microsoft.Win32;
using NAudio.Wave;
using File = System.IO.File;

namespace FireSharp.State
{
	public static class State
	{
		private static SharpWindow _instance = null;
		private static Casette _casette;
		private static int _index = 0;
		private static bool _killParallel;
		private static double _progress = 0;

		public static void AttachInstance(SharpWindow instance)
		{
			if (_instance != null)
				throw new InvalidOperationException("Cannot attach multiple state instances");

			CancellationTokenSource cancelSource = new();

			_instance = instance;
			_instance.Closing += (sender, args) =>
			{
				_killParallel = true;
				_audion.Stop();
				_audion.Dispose();

				cancelSource.Cancel();
				cancelSource.Dispose();
			};

			ThreadPool.QueueUserWorkItem(new WaitCallback(ParallelManagerStart), cancelSource.Token);
		}

		private static void UpdateStateControl() => _instance.Dispatcher.Invoke(() => _instance.UpdateStateControlPath());

		private static void ParallelManagerStart(object obj)
		{
			while (!_killParallel)
			{
				if (_activeWaveStream != null)
				{
					_progress = _activeWaveStream.CurrentTime.Divide(_activeWaveStream.TotalTime);

					_instance.Progress.Dispatcher.Invoke(() => _instance.Progress.Value = _progress);

					if (!Paused)
					{
						if (1.0D <= _progress)
						{
							Debug.WriteLine("Track playback concluded");

							_index++;

							if (LoadTrack())
							{
								_audion.Play();
							}

							else
							{
								_audion.Stop();
							}
							

							UpdateStateControl();
						}
					}
				}
			}
		}

		private static readonly WaveOutEvent _audion = new();

		private static WaveStream _activeWaveStream;

		public static bool Paused => _audion.PlaybackState == PlaybackState.Paused;
		public static bool Stopped => _audion.PlaybackState == PlaybackState.Stopped;
		public static bool Playing => !Paused && !Stopped;

		private static bool Playable => _casette != null && 0 <= _index && _index < _casette.Tracks;

		private static bool LoadTrack()
		{
			if (Playable)
			{
				_audion.Stop();
				_activeWaveStream = new AudioFileReader(_casette[_index].Path);
				_audion.Init(new WaveChannel32(_activeWaveStream));
				return true;
			}

			else if (_index != 0)
			{
				// Attempt to reset position to beginning

				_index = 0;
				bool reset = LoadTrack();

				return false; // <--------------- RETURN reset TO LOOP
			}

			return false;
		}

		private static void CheckedPlay()
		{
			if (Playable)
			{
				_audion.Play();
			}
		}

		public static void PauseSwitch()
		{
			if (!Playing)
			{
				CheckedPlay();
			}

			else 
			{
				_audion.Pause();
			}
		}

		public static void TrackSwitch(bool next = true)
		{
			if (next)
			{
				// skip to next track

				_index++;
				LoadTrack();
				CheckedPlay();
			}

			else
			{
				// revert to previous track

				_index--;
				LoadTrack();
				CheckedPlay();
			}
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
			if (!Playing)
				LoadTrack();
		}

		internal static void ReceiveAudioPaths(string[] audioPaths)
		{
			if (_casette == null)
				_casette = Casette.Empty();

			_casette.ReceiveAudioPaths(audioPaths);
			LoadCasette(_casette);
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

		public static bool Saved => _casette == null || _casette.Saved;
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
			Path = "";
		}

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

		public Track this[int ix] => Queue[ix];

		public string Path { get; private set; }
		public bool Saved { get; private set; }
		public int Tracks => Queue.Count;

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
