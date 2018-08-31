using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;

using Newtonsoft.Json;

namespace TitanMessage
{
	public class Translation
	{
		public string FileType { get; set; }
		public string Description { get; set; }
		public string RelativePath { get; set; }
		public TranslatableEntry[] Entries { get; set; }

		public Translation(string fileType, string relativePath)
		{
			FileType = fileType;
			Description = Path.GetFileNameWithoutExtension(relativePath);
			RelativePath = relativePath;
		}
	}

	[DebuggerDisplay("ID = {ID}, Original = {Original.Substring(0, System.Math.Min(Original.Length, 64))}, Translation = {Translation.Substring(0, System.Math.Min(Translation.Length, 64))}")]
	public class TranslatableEntry : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		int id;
		bool isIgnored;
		string original, translation, notes;

		public int ID
		{
			get => id;
			set => SetField(ref id, value, nameof(ID));
		}

		public bool IsIgnored
		{
			get => isIgnored;
			set => SetField(ref isIgnored, value, nameof(IsIgnored));
		}

		[JsonIgnore]
		public string Original
		{
			get => original;
			set => SetField(ref original, value, nameof(Original));
		}

		[JsonProperty("Original")]
		public string[] OriginalArray
		{
			get => Original?.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			set => Original = string.Join(Environment.NewLine, value);
		}

		[JsonIgnore]
		public string Translation
		{
			get => translation;
			set => SetField(ref translation, value, nameof(Translation));
		}

		[JsonProperty("Translation")]
		public string[] TranslationArray
		{
			get => Translation?.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			set => Translation = string.Join(Environment.NewLine, value);
		}

		[JsonIgnore]
		public string Notes
		{
			get => notes;
			set => SetField(ref notes, value, nameof(Notes));
		}

		[JsonProperty("Notes")]
		public string[] NotesArray
		{
			get => Notes?.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			set => Notes = string.Join(Environment.NewLine, value);
		}
	}
}
