using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace FB2wpfLib
{
	/// <summary>
	/// Simple class for reading Fb2 file.
	/// </summary>
	public class FB2Reader : IFB2Reader
	{
		/// <summary>
		/// Читает файл Fb2 из строки.
		/// </summary>
		/// <param name="xml">Содержимое файла Fb2 в виде строки.</param>
		public Task<FB2File> ReadAsync(string xml)
		{
			return Task.Factory.StartNew(() =>
			{
				var file = new FB2File();
				var fb2Document = XDocument.Parse(xml);
				file.Load(fb2Document, false);
				return file;
			});
		}
	}
}
