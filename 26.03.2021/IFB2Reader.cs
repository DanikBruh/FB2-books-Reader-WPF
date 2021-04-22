using System.IO;
using System.Threading.Tasks;

namespace FB2wpfLib
{
	public interface IFB2Reader
	{
		/// <summary>
		/// Читает файл Fb2 из строки.
		/// </summary>
		/// <param name="xml">Содержимое файла Fb2 в виде строки.</param>
		Task<FB2File> ReadAsync(string xml);
	}
}
