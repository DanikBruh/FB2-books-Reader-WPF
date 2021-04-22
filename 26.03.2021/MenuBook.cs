using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FB2wpfLib
{
	/// <summary>
	/// Класс для отображения книги в разделе меню "Библиотека".
	/// </summary>
	public class MenuBook
	{
		public BitmapImage CoverSource { get; }
		public string BookName { get; }
		public FB2File FB2File { get; }
		public MenuBook() { }
		public MenuBook(BitmapImage coverSource, string bookname, FB2File fB2File)
		{
			CoverSource = coverSource;
			BookName = bookname;
			FB2File = fB2File;
		}
	}

	/// <summary>
	/// Класс MenuBooks. Содержит список объектов типа MenuBook.
	/// </summary>
	public class MenuBooks
	{
		private List<MenuBook> _books;
		public static MenuBook LastOpenedMenuBook { get; set; }

		public MenuBooks()
		{
			_books = new List<MenuBook>();
			LastOpenedMenuBook = null;
		}

		// Возвращает кол-во книг.
		public int GetBooksCount() => _books.Count;

		// Ищет изображение обложки Книги ее название в объекте _file и добавляет объекты типа MenuBook в список _books.
		public void AddMenuBookFromFB2File(FB2File _file)
		{
			if (_books.Exists((MenuBook mb) => mb.BookName == _file.TitleInfo.BookTitle.Text))
				throw new Exception("Данная книга уже добавлена в библиотеку!");
			else 
			{
				// Проверка если ли изображения в файле FB2. Если нету, то добавляет картинку (No Image).
				if(_file.Images.Count > 0)
				{
					BitmapImage imgSource = MainWindow.byteArrToImgSource(_file.Images.First().Value.BinaryData);
					_books.Add(new MenuBook(imgSource, _file.TitleInfo.BookTitle.Text, _file));
				}
				else
				{
					BitmapImage imgSource = new BitmapImage(new Uri(Directory.GetParent(@"..") + @"/images/no-img.png"));
					_books.Add(new MenuBook(imgSource, _file.TitleInfo.BookTitle.Text, _file));
				}
			}
		}

		/// <summary>
		/// Создает таблицу содержащую добавленные книги в окно приложения.
		/// </summary>
		/// <param name="fd">Объект FlowDocument, расположенный на странице.</param>
		/// <param name="mi">Объект MenuItem, для передачи в качестве аргумента функции нажатия на книги в библиотеке.</param>
		public void ShowLibraryMenu(FlowDocument fd, MenuItem mi)
		{
			try
			{
				var sectionTitle = new Paragraph(new Run("\n Б И Б Л И О Т Е К А \n"));
				sectionTitle.FontFamily = new FontFamily("Soin Sans Pro");
				sectionTitle.FontSize = 20;
				sectionTitle.TextAlignment = TextAlignment.Center;
				sectionTitle.FontWeight = FontWeights.Medium;
				
				fd.Blocks.Add(sectionTitle);

				var buicont = new BlockUIContainer();
				var grid = new Grid();

				// Добавление столбцов в таблицу.
				for (int i = 1; i <= 7; i++)
				{
					var coldef = new ColumnDefinition();
					if (i % 2 != 0)
						coldef.Width = new GridLength(0.4, GridUnitType.Star);
					grid.ColumnDefinitions.Add(coldef);
				}

				AddRowDefinition(grid);

				// Добавление строк в таблицу.
				for (int i = 1; i <= _books.Count; i++)
				{
					if(i > 3 && i % 3 == 1)
						AddRowDefinition(grid);
				}

				// последняя строка для отступа
				var lastrowdef = new RowDefinition();
				lastrowdef.Height = new GridLength(1, GridUnitType.Star);
				grid.RowDefinitions.Add(lastrowdef);

				int ColumnIndex = 1;
				int RowIndex = 1;

				// Добавление книг в таблицу.
				for (int i = 0; i < _books.Count; i++)
				{
					if (i > 2 && i % 3 == 0)
					{
						RowIndex ++;
						ColumnIndex = 1;
					}
					var but = new Button();
					var cover = new Image();
					cover.Source = _books[i].CoverSource;
					cover.VerticalAlignment = VerticalAlignment.Bottom;
					but.Content = cover;
					but.ToolTip = _books[i].BookName;

					MenuBook menuBook = _books[i];
					but.Click += delegate (object sender, RoutedEventArgs e) { MainWindow.On_MenuBook_Click(sender, e, fd, menuBook, mi); };
					but.Margin = new Thickness { Bottom = 35 };
					Grid.SetRow(but, RowIndex);
					Grid.SetColumn(but, ColumnIndex);
					grid.Children.Add(but);
				
					ColumnIndex += 2;
				}
				buicont.Child = grid;
				fd.Blocks.Add(buicont);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "ERROR");
			}
		}

		// Добавляет ряд <RowDefinition> в таблицу grid.
		private void AddRowDefinition(Grid grid)
		{
			var rowdef = new RowDefinition();
			rowdef.Height = new GridLength();
			grid.RowDefinitions.Add(rowdef);
		}
	}
}