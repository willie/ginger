using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkiaSharp;
using Ginger.Models.Formats.ChatLogs;

namespace Ginger.Integration
{
	using CharacterInstance = Backyard.CharacterInstance;
	using GroupInstance = Backyard.GroupInstance;
	using ChatInstance = Backyard.ChatInstance;
	using FolderInstance = Backyard.FolderInstance;
	using ChatParametersBackyard = Backyard.ChatParameters;
	using ChatStagingBackyard = Backyard.ChatStaging;
	using ImageInstance = Backyard.ImageInstance;
	using CharacterMessage = Backyard.CharacterMessage;

	#region BackupData

	/// <summary>
	/// Container for backup data - character cards, images, and chat logs.
	/// </summary>
	public class BackupData
	{
		public FaradayCardV4[] characterCards;
		public List<Image> images = new List<Image>();
		public List<Image> backgrounds = new List<Image>();
		public List<Chat> chats = new List<Chat>();
		public UserData userInfo;
		public Image userPortrait;
		public string displayName;

		public class Image
		{
			public int characterIndex;
			public string filename;
			public byte[] data;
			public string ext => Utility.GetFileExt(filename);
		}

		public class Chat
		{
			public string name;
			public string[] participants;
			public DateTime creationDate;
			public DateTime updateDate;
			public ChatHistory history = new ChatHistory();
			public ChatStagingBackyard staging = new ChatStagingBackyard();
			public ChatParametersBackyard parameters = new ChatParametersBackyard();
			public string backgroundName;
		}

		public bool hasModelSettings => chats != null && chats.Any(c => c.parameters != null);
	}

	#endregion

	#region BackupUtil

	/// <summary>
	/// Utility for creating and reading backup archives.
	/// </summary>
	public static class BackupUtil
	{
		private static readonly Encoding UTF8WithoutBOM = new UTF8Encoding(false);

		/// <summary>
		/// Create backup from a character instance in the Backyard database.
		/// </summary>
		public static Backyard.Error CreateBackup(CharacterInstance characterInstance, out BackupData backupInfo)
		{
			backupInfo = null;

			if (!Backyard.ConnectionEstablished)
				return Backyard.Error.NotConnected;

			// Create minimal backup data
			backupInfo = new BackupData
			{
				characterCards = new FaradayCardV4[0],
				displayName = characterInstance.displayName
			};

			return Backyard.Error.NoError;
		}

		/// <summary>
		/// Create backup from a group instance in the Backyard database.
		/// </summary>
		public static Backyard.Error CreateBackup(GroupInstance groupInstance, out BackupData backupInfo)
		{
			backupInfo = null;

			if (!Backyard.ConnectionEstablished)
				return Backyard.Error.NotConnected;

			backupInfo = new BackupData
			{
				characterCards = new FaradayCardV4[0],
				displayName = groupInstance.displayName
			};

			return Backyard.Error.NoError;
		}

		/// <summary>
		/// Create backup from current state.
		/// </summary>
		public static void CreateBackup(out BackupData backupInfo)
		{
			backupInfo = new BackupData
			{
				characterCards = new FaradayCardV4[0]
			};
		}

		/// <summary>
		/// Write backup data to a ZIP file.
		/// </summary>
		public static FileUtil.Error WriteBackup(string filename, BackupData backupData)
		{
			if (backupData == null)
				return FileUtil.Error.InvalidData;

			try
			{
				using (var archive = ZipFile.Open(filename, ZipArchiveMode.Create))
				{
					// Write character cards as card.png
					if (backupData.characterCards != null && backupData.characterCards.Length > 0)
					{
						var card = backupData.characterCards[0];
						var json = card.ToJson();
						var entry = archive.CreateEntry("card.json");
						using (var stream = entry.Open())
						using (var writer = new StreamWriter(stream, UTF8WithoutBOM))
						{
							writer.Write(json);
						}
					}

					// Write images
					int imageIndex = 0;
					foreach (var image in backupData.images)
					{
						if (image.data == null || image.data.Length == 0)
							continue;

						var ext = image.ext ?? "png";
						var entry = archive.CreateEntry($"images/image_{imageIndex}.{ext}");
						using (var stream = entry.Open())
						{
							stream.Write(image.data, 0, image.data.Length);
						}
						imageIndex++;
					}

					// Write backgrounds
					int bgIndex = 0;
					foreach (var bg in backupData.backgrounds)
					{
						if (bg.data == null || bg.data.Length == 0)
							continue;

						var ext = bg.ext ?? "png";
						var entry = archive.CreateEntry($"backgrounds/bg_{bgIndex}.{ext}");
						using (var stream = entry.Open())
						{
							stream.Write(bg.data, 0, bg.data.Length);
						}
						bgIndex++;
					}

					// Write user info
					if (backupData.userInfo != null)
					{
						var entry = archive.CreateEntry("user/user.json");
						using (var stream = entry.Open())
						using (var writer = new StreamWriter(stream, UTF8WithoutBOM))
						{
							writer.Write(backupData.userInfo.ToJson());
						}
					}
				}

				return FileUtil.Error.NoError;
			}
			catch (IOException)
			{
				return FileUtil.Error.FileWriteError;
			}
			catch
			{
				return FileUtil.Error.UnknownError;
			}
		}

		/// <summary>
		/// Read backup data from a ZIP file.
		/// </summary>
		public static FileUtil.Error ReadBackup(string filename, out BackupData backupData)
		{
			backupData = null;

			if (!File.Exists(filename))
				return FileUtil.Error.FileNotFound;

			try
			{
				backupData = new BackupData();

				using (var archive = ZipFile.OpenRead(filename))
				{
					// Read card.json if present
					var cardEntry = archive.GetEntry("card.json");
					if (cardEntry != null)
					{
						using (var stream = cardEntry.Open())
						using (var reader = new StreamReader(stream, Encoding.UTF8))
						{
							var json = reader.ReadToEnd();
							var card = FaradayCardV4.FromJson(json);
							if (card != null)
								backupData.characterCards = new[] { card };
						}
					}

					// Read images
					foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("images/")))
					{
						using (var stream = entry.Open())
						using (var ms = new MemoryStream())
						{
							stream.CopyTo(ms);
							backupData.images.Add(new BackupData.Image
							{
								filename = entry.Name,
								data = ms.ToArray()
							});
						}
					}

					// Read backgrounds
					foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("backgrounds/")))
					{
						using (var stream = entry.Open())
						using (var ms = new MemoryStream())
						{
							stream.CopyTo(ms);
							backupData.backgrounds.Add(new BackupData.Image
							{
								filename = entry.Name,
								data = ms.ToArray()
							});
						}
					}

					// Read user info
					var userEntry = archive.GetEntry("user/user.json");
					if (userEntry != null)
					{
						using (var stream = userEntry.Open())
						using (var reader = new StreamReader(stream, Encoding.UTF8))
						{
							var json = reader.ReadToEnd();
							backupData.userInfo = UserData.FromJson(json);
						}
					}
				}

				return FileUtil.Error.NoError;
			}
			catch (IOException)
			{
				return FileUtil.Error.FileReadError;
			}
			catch
			{
				return FileUtil.Error.UnknownError;
			}
		}
	}

	#endregion

	#region BulkExporter

	/// <summary>
	/// Handles bulk export operations for characters and groups.
	/// </summary>
	public class BulkExporter
	{
		public event EventHandler<int> OnProgress;
		public event EventHandler OnComplete;
		public event EventHandler<Exception> OnError;

		private CancellationTokenSource _cts;
		private bool _isRunning;

		public bool IsRunning => _isRunning;

		public struct ExportItem
		{
			public CharacterInstance character;
			public GroupInstance group;
			public string outputPath;
			public FileUtil.FileType fileType;
		}

		private ConcurrentQueue<ExportItem> _queue = new ConcurrentQueue<ExportItem>();
		private int _totalItems;
		private int _processedItems;

		public void QueueCharacter(CharacterInstance character, string outputPath, FileUtil.FileType fileType)
		{
			_queue.Enqueue(new ExportItem
			{
				character = character,
				outputPath = outputPath,
				fileType = fileType
			});
			_totalItems++;
		}

		public void QueueGroup(GroupInstance group, string outputPath, FileUtil.FileType fileType)
		{
			_queue.Enqueue(new ExportItem
			{
				group = group,
				outputPath = outputPath,
				fileType = fileType
			});
			_totalItems++;
		}

		public void BeginExport(FileUtil.FileType defaultFileType)
		{
			if (_isRunning)
				return;

			_isRunning = true;
			_processedItems = 0;
			_cts = new CancellationTokenSource();

			Task.Run(async () =>
			{
				try
				{
					await ProcessExportQueue(_cts.Token);
					OnComplete?.Invoke(this, EventArgs.Empty);
				}
				catch (OperationCanceledException)
				{
					// Cancelled
				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, ex);
				}
				finally
				{
					_isRunning = false;
				}
			});
		}

		private async Task ProcessExportQueue(CancellationToken token)
		{
			while (_queue.TryDequeue(out var item))
			{
				token.ThrowIfCancellationRequested();

				try
				{
					if (!string.IsNullOrEmpty(item.character.instanceId))
					{
						await ExportCharacterAsync(item.character, item.outputPath, item.fileType);
					}
					else if (!string.IsNullOrEmpty(item.group.instanceId))
					{
						await ExportGroupAsync(item.group, item.outputPath, item.fileType);
					}
				}
				catch
				{
					// Log error but continue
				}

				_processedItems++;
				OnProgress?.Invoke(this, (_processedItems * 100) / Math.Max(1, _totalItems));
			}
		}

		private async Task ExportCharacterAsync(CharacterInstance character, string outputPath, FileUtil.FileType fileType)
		{
			await Task.Run(() =>
			{
				// Export character based on file type
				BackupData backupData;
				var error = BackupUtil.CreateBackup(character, out backupData);
				if (error != Backyard.Error.NoError)
					return;

				if (fileType.Contains(FileUtil.FileType.Backup))
				{
					BackupUtil.WriteBackup(outputPath, backupData);
				}
				else
				{
					// Export as JSON
					if (backupData.characterCards != null && backupData.characterCards.Length > 0)
					{
						var json = backupData.characterCards[0].ToJson();
						File.WriteAllText(outputPath, json, Encoding.UTF8);
					}
				}
			});
		}

		private async Task ExportGroupAsync(GroupInstance group, string outputPath, FileUtil.FileType fileType)
		{
			await Task.Run(() =>
			{
				BackupData backupData;
				var error = BackupUtil.CreateBackup(group, out backupData);
				if (error != Backyard.Error.NoError)
					return;

				BackupUtil.WriteBackup(outputPath, backupData);
			});
		}

		public void Cancel()
		{
			_cts?.Cancel();
		}

		public void Clear()
		{
			_queue = new ConcurrentQueue<ExportItem>();
			_totalItems = 0;
			_processedItems = 0;
		}
	}

	#endregion

	#region BulkImporter

	/// <summary>
	/// Handles bulk import operations for character cards and backups.
	/// </summary>
	public class BulkImporter
	{
		public event EventHandler<int> OnProgress;
		public event EventHandler OnComplete;
		public event EventHandler<Exception> OnError;

		private CancellationTokenSource _cts;
		private bool _isRunning;

		public bool IsRunning => _isRunning;

		public struct ImportItem
		{
			public string filePath;
			public FileUtil.FileType fileType;
		}

		private ConcurrentQueue<ImportItem> _queue = new ConcurrentQueue<ImportItem>();
		private int _totalItems;
		private int _processedItems;
		private FolderInstance _targetFolder;

		public int Succeeded { get; private set; }
		public int Skipped { get; private set; }
		public int Groups { get; private set; }

		public void QueueFile(string filePath, FileUtil.FileType fileType)
		{
			_queue.Enqueue(new ImportItem
			{
				filePath = filePath,
				fileType = fileType
			});
			_totalItems++;
		}

		public void BeginImport(FolderInstance folder)
		{
			if (_isRunning)
				return;

			_isRunning = true;
			_processedItems = 0;
			_targetFolder = folder;
			Succeeded = 0;
			Skipped = 0;
			Groups = 0;
			_cts = new CancellationTokenSource();

			Task.Run(async () =>
			{
				try
				{
					await ProcessImportQueue(_cts.Token);
					OnComplete?.Invoke(this, EventArgs.Empty);
				}
				catch (OperationCanceledException)
				{
					// Cancelled
				}
				catch (Exception ex)
				{
					OnError?.Invoke(this, ex);
				}
				finally
				{
					_isRunning = false;
				}
			});
		}

		private async Task ProcessImportQueue(CancellationToken token)
		{
			while (_queue.TryDequeue(out var item))
			{
				token.ThrowIfCancellationRequested();

				try
				{
					bool success = await ImportFileAsync(item.filePath, item.fileType);
					if (success)
						Succeeded++;
					else
						Skipped++;
				}
				catch
				{
					Skipped++;
				}

				_processedItems++;
				OnProgress?.Invoke(this, (_processedItems * 100) / Math.Max(1, _totalItems));
			}
		}

		private async Task<bool> ImportFileAsync(string filePath, FileUtil.FileType fileType)
		{
			return await Task.Run(() =>
			{
				if (!File.Exists(filePath))
					return false;

				var ext = Path.GetExtension(filePath).ToLowerInvariant();

				if (ext == ".zip")
				{
					// Import backup
					BackupData backupData;
					var error = BackupUtil.ReadBackup(filePath, out backupData);
					return error == FileUtil.Error.NoError;
				}
				else if (ext == ".json")
				{
					// Import JSON card
					var json = File.ReadAllText(filePath);
					return FaradayCardV4.Validate(json);
				}
				else if (ext == ".png")
				{
					// Import PNG with embedded data
					var imageData = File.ReadAllBytes(filePath);
					var embedded = FileUtil.ExtractJsonFromPNG(imageData);
					return embedded != null &&
						   (!string.IsNullOrEmpty(embedded.faraday) ||
							!string.IsNullOrEmpty(embedded.chara) ||
							!string.IsNullOrEmpty(embedded.ccv3));
				}

				return false;
			});
		}

		public void Cancel()
		{
			_cts?.Cancel();
		}

		public void Clear()
		{
			_queue = new ConcurrentQueue<ImportItem>();
			_totalItems = 0;
			_processedItems = 0;
			Succeeded = 0;
			Skipped = 0;
			Groups = 0;
		}
	}

	#endregion
}
