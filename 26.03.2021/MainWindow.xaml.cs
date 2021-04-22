using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using FB2Library.Elements;
using FB2Library.Elements.Poem;
using Microsoft.Win32;

// Myrzaliyev Daniyar

namespace FB2wpfLib
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FB2File _file;
        private MenuBooks _menuBooks;
        // Счетчик изображений.
        private static int imgCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            // Установка режима прокрутки по умолчанию.
            fdReader.ViewingMode = FlowDocumentReaderViewingMode.Scroll;
            // Добавление событий для каждого раздела меню.
            foreach (MenuItem item in mMenu.Items)
            {
                item.Click += onMenuItemClick;
            }
            _menuBooks = new MenuBooks();
            _file = new FB2File();
        }

        // Обработчик нажатия на раздел меню. Меняет цвет выбранного раздела меню.
        private void onMenuItemClick(object sender, RoutedEventArgs e)
        {
            MenuItem miActive = (MenuItem)e.OriginalSource;
            StackPanel spActive = (StackPanel)miActive.Header;
            Image imgActive = (Image)spActive.Children[0];

            string newSource = imgActive.Source.ToString().Replace("light", "white");

            ResetMenuItemsToDefault();
            changeMenuItemsColorsOnClick(miActive);
            changeMenuItemsImagesOnClick(imgActive, newSource);
        }

        /// <summary>
        /// Сбрасывает цвета и иконки всех разделов меню и устанавливает их по умолчанию
        /// </summary>
        private void ResetMenuItemsToDefault()
        {
            foreach (MenuItem mi in mMenu.Items)
            {
                mi.Background = (Brush)new BrushConverter().ConvertFrom("#101820");

                StackPanel spObj = (StackPanel)mi.Header;
                Image img = (Image)spObj.Children[0];
                img.Source = new BitmapImage(new Uri(img.Source.ToString().Replace("white", "light")));
            }
        }

        /// <summary>
        /// Меняет цвет фона активного раздела меню.
        /// </summary>
        /// <param name="activeMenuItem">Активный раздел меню</param>
        private void changeMenuItemsColorsOnClick(MenuItem activeMenuItem) => activeMenuItem.Background = (Brush)new BrushConverter().ConvertFrom("#f2aa4c");

        /// <summary>
        /// Меняет иконку активного раздела меню. (на такую же, но с другим цветом).
        /// </summary>
        /// <param name="activeImage">Картинка активного раздела меню</param>
        /// <param name="newSource">Путь к новой картинке</param>
        private void changeMenuItemsImagesOnClick(Image activeImage, string newSource) => activeImage.Source = new BitmapImage(new Uri(newSource));

        /// <summary>
        /// Обработчик нажатия на Книгу в Библиотеке. Заполняет FlowDocument всем содержимым Книги.
        /// </summary>
        /// <param name="fd">Объект FlowDocument в файле XAML, куда добавляется вся книга.</param>
        /// <param name="menuBook">Добавляемая Книга.</param>
        /// <param name="mi">Элемент MenuItem для вызова его события "Click" при отображении Книги.</param>
        public static void On_MenuBook_Click(object sender, RoutedEventArgs e, FlowDocument fd, MenuBook menuBook, MenuItem mi)
        {
            var _file = menuBook.FB2File;

            AddBookToFlowDocument(fd, menuBook);
            MenuBooks.LastOpenedMenuBook = menuBook;

            // Вызываем событие "Click" для перемещения в раздел "Читать книги".
            mi.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        }

        /// <summary>
        /// Заполняет FlowDocument fd всеми строками книги.
        /// </summary>
        /// <param name="fd">Объект FlowDocument в файле XAML, куда добавляется вся книга.</param>
        /// <param name="menuBook">Добавляемая Книга.</param>
        private static void AddBookToFlowDocument(FlowDocument fd, MenuBook menuBook)
        {
            var _file = menuBook.FB2File;
            // Очитска окна.
            fd.Blocks.Clear();

            // Добавление информации о книге
            AddBookInfo(fd, _file);

            // Добавление содержимого книги
            foreach (var body in _file.Bodies)
            {
                AddBodySections(body.Sections, fd, _file);
            }
            imgCount = 0;
        }

        // Добавляет информацию о книге (Название, авторы, год и город публикации, издание и тд.) на страницу
        private static void AddBookInfo(FlowDocument fd, FB2File _file)
        {
            // Название книги
            var bookTitle = new Paragraph(new Run(_file.TitleInfo.BookTitle.ToString()));
            bookTitle.FontSize = 24;
            bookTitle.FontFamily = new FontFamily("Calibri");
            bookTitle.FontWeight = FontWeights.Bold;
            bookTitle.TextAlignment = TextAlignment.Center;
            fd.Blocks.Add(bookTitle);

            if (_file.Images.Count > 0)
            {
                Image bookAvatar = new Image();
                bookAvatar.Source = byteArrToImgSource(_file.Images.First().Value.BinaryData);
                var par = new Paragraph();
                // Элемент <Figure> с картинкой обложки книги. (располагается слева)
                Figure myFigure = new Figure();
                myFigure.Width = new FigureLength(250);
                myFigure.Height = new FigureLength(300);
                myFigure.HorizontalAnchor = FigureHorizontalAnchor.PageLeft;
                var innerPar = new Paragraph();
                innerPar.Inlines.Add(bookAvatar);
                myFigure.Blocks.Add(innerPar);
                par.Inlines.Add(myFigure);
                fd.Blocks.Add(par);
                imgCount++;
            }

            if (_file.DocumentInfo.DocumentAuthors[0].FirstName != null && _file.DocumentInfo.DocumentAuthors[0].LastName != null) fd.Blocks.Add(new Paragraph(new Run("Автор: " + _file.DocumentInfo.DocumentAuthors[0].FirstName + " " + _file.DocumentInfo.DocumentAuthors[0].LastName)));
            if (_file.DocumentInfo.DocumentAuthors[0].EMail != null) fd.Blocks.Add(new Paragraph(new Run("Почта автора: " + _file.DocumentInfo.DocumentAuthors[0].EMail.Text)));
            if (_file.PublishInfo.Publisher != null) fd.Blocks.Add(new Paragraph(new Run("Издательство " + _file.PublishInfo.Publisher.Text)));
            if (_file.PublishInfo.City != null) fd.Blocks.Add(new Paragraph(new Run("Город: " + _file.PublishInfo.City.Text)));
            if (_file.PublishInfo.Year != null) fd.Blocks.Add(new Paragraph(new Run("Год издания: " + _file.PublishInfo.Year.ToString())));
            if (_file.TitleInfo.Genres.Count() > 0) fd.Blocks.Add(new Paragraph(new Run("Жанр: " + _file.TitleInfo.Genres.First().Genre)));
            if (_file.TitleInfo.Language != null) fd.Blocks.Add(new Paragraph(new Run("Язык: " + _file.TitleInfo.Language)));

            if (_file.CustomInfo.Count > 0) fd.Blocks.Add(new Paragraph(new Run(_file.CustomInfo[0].Text)));
            if (_file.DocumentInfo.History != null)
            {
                foreach (var content in _file.DocumentInfo.History.Content)
                    fd.Blocks.Add(new Paragraph(new Run(content + " ")));
            }
            // <annotation>
            if (_file.TitleInfo.Annotation != null)
            {
                var annotation = new Run(_file.TitleInfo.Annotation.ToString());
                annotation.FontStyle = FontStyles.Italic;
                fd.Blocks.Add(new Paragraph(annotation));
            }
        }

        // Добавляет разделы из заданного тела <body> на страницу.
        private static void AddBodySections(List<SectionItem> sections, FlowDocument fd, FB2File _file)
        {
            // <section>
            foreach (var section in sections)
            {
                AddSection(section, fd, _file);
            }
        }

        // Добавляет Раздел <section> на страницу.
        private static void AddSection(SectionItem section, FlowDocument fd, FB2File _file)
        {
            // Добавление заголовка внутри <section>
            if (section.Title != null)
            {
                var sectionTitle = new Paragraph(new Run("\n" + section.Title + "\n"));
                sectionTitle.FontFamily = new FontFamily("Soin Sans Pro");
                sectionTitle.FontSize = 20;
                sectionTitle.FontWeight = FontWeights.Bold;
                fd.Blocks.Add(sectionTitle);
            }
            else fd.Blocks.Add(new Paragraph(new LineBreak()));

            // Добавляет Эпиграфы внутри <section> если имеются.
            if (section.Epigraphs.Count > 0)
            {
                AddSectionEpigraphs(section.Epigraphs, fd);
            }

            // Добавляет все содержимое <section> в параграф par.
            AddSectionContent(section, fd, _file);
        }

        // Добавляет Эпиграфы внутри <section> на страницу.
        private static void AddSectionEpigraphs(List<EpigraphItem> secEpigraphs, FlowDocument fd)
        {
            foreach (var epigraph in secEpigraphs)
            {
                foreach (var epigraphData in epigraph.EpigraphData)
                {
                    // Если Стих <Poem> содержится в Эпиграфе, то добавляет его на страницу в виде курсивного текста
                    if (epigraphData.ToString().Contains("PoemItem"))
                    {
                        AddPoemToFlowDocument((PoemItem)epigraphData, fd);
                    }
                    else
                    {
                        fd.Blocks.Add(new Paragraph(new Run(epigraphData.ToString())));
                    }
                }
            }
        }

        // Добавляет содержимое (content) <section> в параграф par.
        private static void AddSectionContent(SectionItem section, FlowDocument fd, FB2File _file)
        {
            foreach (var content in section.Content)
            {
                // Если строка начинается с диалога.
                if (content.ToString().StartsWith("—") || content.ToString().StartsWith("-") || content.ToString().StartsWith("–"))
                {
                    fd.Blocks.Add(new Paragraph(new Run("\n" + content.ToString())));
                }
                // Добавление стиха (курсивный текст)
                else if (content.ToString().Contains("PoemItem"))
                {
                    AddPoemToFlowDocument((PoemItem)content, fd);
                }
                // Добавление цитат в тексте книги
                else if (content.ToString().Contains("CiteItem"))
                {
                    AddCiteToFlowDocument((CiteItem)content, fd);
                }
                // Добавление картинки
                else if (content.ToString().Contains("ImageItem"))
                {
                    AddImageToFlowDocument(_file, fd);
                }
                // Рекурсия (в случае, если имеется <section> внутри <section>.
                else if (content.ToString().Contains("SectionItem"))
                {
                    AddSection((SectionItem)content, fd, _file);
                }
                // Добавление сплошного текста книги
                else
                {
                    fd.Blocks.Add(new Paragraph(new Run(content.ToString())));
                }
            }
        }

        // Добавляет элемент <poem> - стих (курсивный текст)
        private static void AddPoemToFlowDocument(PoemItem poemItem, FlowDocument fd)
        {
            var par = new Paragraph();
            foreach (var poemContent in poemItem.Content)
            {
                var poemLines = ((StanzaItem)poemContent).Lines;
                for (int i = 0; i < poemLines.Count; i++)
                {
                    var poemString = new Run();
                    if (i == poemLines.Count - 1) poemString = new Run(poemLines[i].ToString());
                    else poemString = new Run(poemLines[i].ToString() + "\n"); // Отступы

                    poemString.FontStyle = FontStyles.Italic;
                    par.Inlines.Add(poemString);
                }
            }
            par.Margin = new Thickness { Left = 30 };
            fd.Blocks.Add(par);
        }

        // Добавляет элемент <cite> - цитата в тексте книги
        private static void AddCiteToFlowDocument(CiteItem citeItem, FlowDocument fd)
        {
            foreach (var citiAuthor in citeItem.TextAuthors)
            {
                var par = new Paragraph();
                var authorRun = new Run(citiAuthor.ToString());
                authorRun.FontStyle = FontStyles.Italic;
                par.Inlines.Add(authorRun);
                par.Margin = new Thickness { Left = 20 };
                fd.Blocks.Add(par);
            }
            foreach (var cityString in citeItem.CiteData)
            {
                var par = new Paragraph();
                var citistrRun = new Run(cityString.ToString());
                citistrRun.FontStyle = FontStyles.Italic;
                par.Inlines.Add(citistrRun);
                par.Margin = new Thickness { Left = 20 };
                fd.Blocks.Add(par);
            }
        }

        // Добавляет элемент <image> - картинка
        private static void AddImageToFlowDocument(FB2File _file, FlowDocument fd)
        {
            Image bookImage = new Image();
            bookImage.Source = byteArrToImgSource(_file.Images.ElementAt(imgCount).Value.BinaryData);
            var parImg = new Paragraph();
            bookImage.MaxWidth = 500;
            parImg.Inlines.Add(bookImage);
            parImg.TextAlignment = TextAlignment.Center;
            fd.Blocks.Add(parImg);
            imgCount++;
        }

        // Конвертирует двоичную строку в ссылку на картинку.
        public static BitmapImage byteArrToImgSource(byte[] binaryData)
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = new MemoryStream(binaryData);
            bi.EndInit();
            return bi;
        }



        // Обработчик нажатия на элемент меню "Библиотека". Выводит на экран все доступные и уже открытые книги. (Библиотеку)
        private void ShowLibrary_Click(object sender, RoutedEventArgs e)
        {
            // Очитска страницы
            fd.Blocks.Clear();
            if (_menuBooks.GetBooksCount() > 0)
            {
                _menuBooks.ShowLibraryMenu(fd, miReadBooks);
            }
            else
            {
                MessageBox.Show("Книги не были добавлены!", "ERROR");
            }
        }

        // Обработчик нажатия на элемент меню "Читать книги".
        private void ReadingPage_Click(object sender, RoutedEventArgs e)
        {
            // Очитска страницы
            fd.Blocks.Clear();
            if (MenuBooks.LastOpenedMenuBook != null)
            {
                AddBookToFlowDocument(fd, MenuBooks.LastOpenedMenuBook);
            }
            else
            {
                MessageBox.Show("Вы еще не начинали читать книгу или не добавили их в библиотеку!", "ERROR");
            }
        }

        // Обработчик нажатия на элемент меню "Добавить файл".
        private async void miAddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "(*.fb2)|*.fb2|All files (*.*)|*.*";

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    // Считывание xml (fb2) файла.
                    _file = await new FB2Reader().ReadAsync(XDocument.Load(new StreamReader(openFileDialog.FileName)).ToString());

                    // Добавление Обложек книг и их Названий для их последующего отображения в разделе "Библиотека".
                    _menuBooks.AddMenuBookFromFB2File(_file);

                    // Вызываем событие "Click" для перемещения в раздел "Библиотека".
                    miLibrary.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }
    }
}
