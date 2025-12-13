using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ginger.Integration
{
	/// <summary>
	/// Model information as parsed from Backyard AI's model manifest.
	/// </summary>
	public class BackyardModelInfo
	{
		[JsonProperty("displayName", Required = Required.Always)]
		public string displayName;

		[JsonProperty("promptFormat", Required = Required.Always)]
		public string promptTemplate;

		[JsonProperty("ctxSize", Required = Required.Always)]
		public int ctxSize;

		public class FileEntry
		{
			[JsonProperty("name", Required = Required.Always)]
			public string id;

			[JsonProperty("displayName", Required = Required.Always)]
			public string displayName;

			[JsonProperty("localFilename", Required = Required.Always)]
			public string filename;

			[JsonProperty("cloudPlan", Required = Required.Default, NullValueHandling = NullValueHandling.Include)]
			public string cloudPlan;

			[JsonProperty("isDeprecated", Required = Required.Default)]
			public bool isDeprecated;
		}

		[JsonProperty("files", Required = Required.Always)]
		public FileEntry[] files;
	}

	/// <summary>
	/// Represents a single LLM model available for use.
	/// </summary>
	public struct BackyardModel
	{
		public string id;
		public string displayName;
		public string promptTemplate;
		public string fileFormat;
		public bool isCustomLocalModel;

		public enum CloudPlan
		{
			Undefined = 0,
			Free,
			Standard,
			Advanced,
			Pro,
		}

		public CloudPlan cloudPlan;
		public bool isCloudModel => cloudPlan != CloudPlan.Undefined;

		/// <summary>
		/// Compare by ID or display name (case-insensitive).
		/// </summary>
		public bool Compare(string nameOrId)
		{
			return string.Compare(nameOrId, id, StringComparison.OrdinalIgnoreCase) == 0
				|| string.Compare(nameOrId, displayName, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public override string ToString()
		{
			return displayName;
		}
	}

	/// <summary>
	/// Database of available LLM models for Backyard AI.
	/// Discovers local models from filesystem and parses cloud model information from manifest.
	/// </summary>
	public static class BackyardModelDatabase
	{
		public static string ModelDownloadPath { get; private set; }

		public static IList<BackyardModel> Models => _Models;
		private static List<BackyardModel> _Models = new List<BackyardModel>();
		private static List<BackyardModelInfo> _Entries = new List<BackyardModelInfo>();

		// Known free cloud model ID
		private static readonly string FreeCloudModel = "cloud.llama2.7b.fimbulvetr.gguf_v2.q8_0";

		/// <summary>
		/// Refresh the model database (clears and re-scans).
		/// </summary>
		public static void Refresh()
		{
			_Entries.Clear();
			_Models.Clear();
		}

		/// <summary>
		/// Find and populate models from the download path and JSON manifest.
		/// </summary>
		/// <param name="downloadPath">Directory containing .gguf and .bin model files</param>
		/// <param name="json">JSON manifest from Backyard AI containing model metadata</param>
		/// <returns>True if models were found, false otherwise</returns>
		public static bool FindModels(string downloadPath, string json)
		{
			if (string.IsNullOrEmpty(downloadPath))
				return false;

			_Entries.Clear();
			_Models.Clear();

			ModelDownloadPath = downloadPath;

			// Find local model files
			string[] modelFiles;
			try
			{
				if (!Directory.Exists(downloadPath))
					return false;

				var ggufFiles = Directory.EnumerateFiles(downloadPath, "*.gguf");
				var binFiles = Directory.EnumerateFiles(downloadPath, "*.bin");
				modelFiles = ggufFiles.Union(binFiles).ToArray();

				if (modelFiles.Length == 0)
					return false; // No models found
			}
			catch
			{
				return false; // Error accessing directory
			}

			// Parse JSON manifest
			if (!string.IsNullOrEmpty(json))
			{
				try
				{
					JArray list = JArray.Parse(json);
					foreach (var entry in list)
					{
						try
						{
							var modelInfo = entry.ToObject<BackyardModelInfo>();
							if (modelInfo != null && modelInfo.files != null)
								_Entries.Add(modelInfo);
						}
						catch
						{
							// Skip invalid entries
						}
					}
				}
				catch
				{
					// JSON parse error - continue with just local files
				}
			}

			// Match local files to manifest entries
			_Models = modelFiles
				.Select(fn => Path.GetFileName(fn))
				.Select(fn =>
				{
					var modelInfo = _Entries.FirstOrDefault(e =>
						e.files != null && e.files.Any(f =>
							string.Compare(f.filename, fn, StringComparison.OrdinalIgnoreCase) == 0));

					if (modelInfo != null)
					{
						var fileInfo = modelInfo.files.FirstOrDefault(f =>
							string.Compare(f.filename, fn, StringComparison.OrdinalIgnoreCase) == 0);

						return new BackyardModel
						{
							id = fileInfo?.id ?? fn,
							displayName = fileInfo?.displayName ?? Path.GetFileNameWithoutExtension(fn),
							promptTemplate = modelInfo.promptTemplate,
							fileFormat = GetFileExtension(fn),
							isCustomLocalModel = false,
							cloudPlan = BackyardModel.CloudPlan.Undefined,
						};
					}
					else
					{
						// Unknown local model
						return new BackyardModel
						{
							id = fn,
							displayName = Path.GetFileNameWithoutExtension(fn),
							fileFormat = GetFileExtension(fn),
							promptTemplate = null,
							isCustomLocalModel = true,
							cloudPlan = BackyardModel.CloudPlan.Undefined,
						};
					}
				})
				.OrderBy(i => i.displayName)
				.ToList();

			// Add cloud models from manifest
			var cloudModels = _Entries
				.Where(m => m.files != null && m.files.Any(f => !string.IsNullOrEmpty(f.cloudPlan)))
				.SelectMany(m =>
				{
					return m.files
						.Where(f => !f.isDeprecated && !string.IsNullOrEmpty(f.cloudPlan))
						.Select(f =>
						{
							var cloudPlan = ParseCloudPlan(f.cloudPlan);

							// Special case: known free model
							if (string.Compare(f.id, FreeCloudModel, StringComparison.Ordinal) == 0)
								cloudPlan = BackyardModel.CloudPlan.Free;

							if (cloudPlan == BackyardModel.CloudPlan.Undefined)
								return default;

							return new BackyardModel
							{
								id = f.id,
								displayName = $"Cloud ({cloudPlan}) - {m.displayName}",
								promptTemplate = m.promptTemplate,
								fileFormat = "",
								isCustomLocalModel = false,
								cloudPlan = cloudPlan,
							};
						})
						.Where(mm => mm.cloudPlan != BackyardModel.CloudPlan.Undefined);
				})
				.OrderBy(mm => mm.cloudPlan)
				.ThenBy(mm => mm.displayName);

			_Models.AddRange(cloudModels);

			return true;
		}

		/// <summary>
		/// Get a model by its ID or display name.
		/// </summary>
		public static BackyardModel GetModel(string modelId)
		{
			if (string.IsNullOrEmpty(modelId))
				return default;

			return _Models.FirstOrDefault(m => m.Compare(modelId));
		}

		/// <summary>
		/// Get all local models (non-cloud).
		/// </summary>
		public static IEnumerable<BackyardModel> GetLocalModels()
		{
			return _Models.Where(m => !m.isCloudModel);
		}

		/// <summary>
		/// Get all cloud models.
		/// </summary>
		public static IEnumerable<BackyardModel> GetCloudModels()
		{
			return _Models.Where(m => m.isCloudModel);
		}

		/// <summary>
		/// Get models by cloud plan tier.
		/// </summary>
		public static IEnumerable<BackyardModel> GetModelsByPlan(BackyardModel.CloudPlan plan)
		{
			return _Models.Where(m => m.cloudPlan == plan);
		}

		/// <summary>
		/// Check if any models are available.
		/// </summary>
		public static bool HasModels => _Models.Count > 0;

		/// <summary>
		/// Get the number of available models.
		/// </summary>
		public static int Count => _Models.Count;

		private static BackyardModel.CloudPlan ParseCloudPlan(string planString)
		{
			if (string.IsNullOrEmpty(planString))
				return BackyardModel.CloudPlan.Undefined;

			return planString.ToLowerInvariant() switch
			{
				"free" => BackyardModel.CloudPlan.Free,
				"standard" => BackyardModel.CloudPlan.Standard,
				"advanced" => BackyardModel.CloudPlan.Advanced,
				"pro" => BackyardModel.CloudPlan.Pro,
				_ => BackyardModel.CloudPlan.Undefined
			};
		}

		private static string GetFileExtension(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return "";

			var ext = Path.GetExtension(filename);
			if (string.IsNullOrEmpty(ext))
				return "";

			return ext.TrimStart('.').ToLowerInvariant();
		}
	}
}
