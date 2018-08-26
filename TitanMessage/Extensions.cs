using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;

namespace TitanMessage
{
	public static class Extensions
	{
		public static void SerializeToFile(this object obj, string jsonFileName)
		{
			using (var writer = new StreamWriter(jsonFileName))
			{
				writer.Write(JsonConvert.SerializeObject(obj, Formatting.Indented));
			}
		}

		public static T DeserializeFromFile<T>(this string jsonFileName)
		{
			using (var reader = new StreamReader(jsonFileName))
			{
				return (T)JsonConvert.DeserializeObject(reader.ReadToEnd(), typeof(T), new JsonSerializerSettings() { Formatting = Formatting.Indented });
			}
		}
	}
}
