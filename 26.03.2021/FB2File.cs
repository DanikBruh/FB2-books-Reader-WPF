using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FB2Library.Elements;
using FB2Library.HeaderItems;

namespace FB2wpfLib
{
    public class FB2File
    {
        private XNamespace _fileNameSpace = XNamespace.None;

        private const string TitleInfoElementName = "title-info";
        private const string SrcTitleInfoElementName = "src-title-info";
        private const string DocumentInfoElementName = "document-info";
        private const string Fb2TextDescriptionElementName = "description";
        private const string Fb2TextBodyElementName = "body";
        private const string Fb2BinaryElementName = "binary";

        private readonly ItemTitleInfo _titleInfo = new ItemTitleInfo();
        private readonly ItemTitleInfo _srcTitleInfo = new ItemTitleInfo();
        private readonly ItemDocumentInfo _documentInfo = new ItemDocumentInfo();
        private readonly ItemPublishInfo _publishInfo = new ItemPublishInfo();
        private readonly List<ItemCustomInfo> _customInfo = new List<ItemCustomInfo>();
        private BodyItem _mainBody;
        private readonly List<BodyItem> _bodiesList = new List<BodyItem>();
        private readonly Dictionary<string, BinaryItem> _binaryObjects = new Dictionary<string, BinaryItem>();
        private readonly List<StyleElement> _styles = new List<StyleElement>();
        private readonly List<ShareInstructionType> _output = new List<ShareInstructionType>();


        /// <summary>
        /// Получает список изображений (сами данные изображения), хранящиеся в виде двоичных объектов
        /// </summary>
        public Dictionary<string, BinaryItem> Images { get { return _binaryObjects;} }

        /// <summary>
        /// Получить список тел <body> в документе (включая основной)
        /// </summary>
        public List<BodyItem> Bodies { get { return _bodiesList;}}

        /// <summary>
        /// Получает структуру TitleInfo документа
        /// </summary>
        public ItemTitleInfo TitleInfo { get { return _titleInfo; } }

        /// <summary>
        /// Получает структуру DocumentInfo
        /// </summary>
        public ItemDocumentInfo DocumentInfo { get { return _documentInfo; } }

        /// <summary>
        /// Получает структуру документа SourceTitleInfo
        /// </summary>
        public ItemTitleInfo SourceTitleInfo { get { return _srcTitleInfo; } }


        /// <summary>
        /// Получает добавленную пользователем информацию (если доступно)
        /// </summary>
        public List<ItemCustomInfo> CustomInfo { get { return _customInfo; } }

        /// <summary>
        /// Получает структуру PublishInfo документа
        /// </summary>
        public ItemPublishInfo PublishInfo { get { return _publishInfo; } }

        /// <summary>
        /// Получает основную структуру книги (саму книгу)
        /// </summary>
        public BodyItem MainBody { get { return _mainBody; } }

        /// <summary>
        /// Получает стили, примененные к книге
        /// </summary>
        public List<StyleElement> StyleSheets { get { return _styles; } }

        /// <summary>
        /// Загружает файл в виде данных из XML-файла.
        /// </summary>
        /// <param name="fileDocument">XML-документ, содержащий файл</param>
        /// <param name="loadHeaderOnly">если true, загружает только информацию заголовка</param>
        public void Load(XDocument fileDocument, bool loadHeaderOnly)
        {
            if (fileDocument == null)
            {
                throw new ArgumentNullException("fileDocument");
            }
            if (fileDocument.Root == null)
            {
                throw new ArgumentException("Document's root is NULL (empty document passed)");
            }

            // теоретически пространство имен должно быть "http://www.gribuser.ru/xml/fictionbook/2.0", но на всякий случай с недействительными файлами
            _fileNameSpace = fileDocument.Root.GetDefaultNamespace();

            _styles.Clear();
            IEnumerable<XElement> xStyles = fileDocument.Elements(_fileNameSpace + StyleElement.StyleElementName).ToArray();
            // попытка загрузить какой-то плохой FB2 с неправильным пространством имен
            if (!xStyles.Any())
            {
                xStyles = fileDocument.Elements(StyleElement.StyleElementName);
            }
            foreach (var style in xStyles)
            {
                var element = new StyleElement();
                try
                {
                    element.Load(style);
                    _styles.Add(element);
                }
                catch
                {
                    // игнорируется
                }
            }

            LoadDescriptionSection(fileDocument);

            if (!loadHeaderOnly)
            {
                XNamespace namespaceUsed = _fileNameSpace;
                // Загружает элементы тела <body> (сначала основной текст)
                if (fileDocument.Root != null)
                {
                    IEnumerable<XElement> xBodys = fileDocument.Root.Elements(_fileNameSpace + Fb2TextBodyElementName).ToArray();
                    // пытается прочитать некоторые плохо отформатированные файлы FB2
                    if (!xBodys.Any())
                    {
                        namespaceUsed = "";
                        xBodys = fileDocument.Root.Elements(namespaceUsed + Fb2TextBodyElementName);
                    }
                    foreach (var body in xBodys)
                    {
                        var bodyItem = new BodyItem() { NameSpace = namespaceUsed };
                        try
                        {
                            bodyItem.Load(body);
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        _bodiesList.Add(bodyItem);
                    }
                }
                if (_bodiesList.Count > 0)
                {
                    _mainBody = _bodiesList[0];   
                }


                // Загружает разделы двоичных файлов (в настоящее время только изображения)
                if (fileDocument.Root != null)
                {
                    IEnumerable<XElement> xBinaryes = fileDocument.Root.Elements(namespaceUsed + Fb2BinaryElementName).ToArray();
                    if (!xBinaryes.Any())
                    {
                        xBinaryes = fileDocument.Root.Elements(Fb2BinaryElementName);
                    }
                    foreach (var binarye in xBinaryes)
                    {
                        var item = new BinaryItem();
                        try
                        {
                            item.Load(binarye);
                        }
                        catch
                        {                       
                            continue;
                        }
                        // добавляет только уникальные идентификаторы, чтобы исправить некоторые недопустимые FB2
                        if (!_binaryObjects.ContainsKey(item.Id))
                        {
                            _binaryObjects.Add(item.Id, item);                        
                        }
                    }
                }
            }

        }

        private void LoadDescriptionSection(XDocument fileDocument, bool loadBinaryItems = true)
        {
            if (fileDocument == null)
            {
                throw new ArgumentNullException("fileDocument");
            }
            if (fileDocument.Root == null)
            {
                throw new NullReferenceException("LoadDescriptionSection: Root is null");
            }
            XElement xTextDescription = fileDocument.Root.Element(_fileNameSpace + Fb2TextDescriptionElementName);
            // попытка загрузить какой-то плохой FB2 с неправильным пространством имен
            XNamespace namespaceUsed = _fileNameSpace;
            if (xTextDescription == null)
            {
                namespaceUsed = "";
                xTextDescription = fileDocument.Root.Element(Fb2TextDescriptionElementName);
            }
            if (xTextDescription != null)
            {
                // Загружает информацию о заголовке
                XElement xTitleInfo = xTextDescription.Element(namespaceUsed + TitleInfoElementName);
                if (xTitleInfo != null)
                {
                    _titleInfo.Namespace = namespaceUsed;
                    try
                    {
                        _titleInfo.Load(xTitleInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading title info : {0}", ex.Message);
                    }

                }

                // Загружает информацию о заголовке Src
                XElement xSrcTitleInfo = xTextDescription.Element(namespaceUsed + SrcTitleInfoElementName);
                if (xSrcTitleInfo != null)
                {
                    _srcTitleInfo.Namespace = _fileNameSpace;
                    try
                    {
                        _srcTitleInfo.Load(xSrcTitleInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading source title info : {0}", ex.Message);
                    }
                }

                // Загружает информацию о документе
                XElement xDocumentInfo = xTextDescription.Element(namespaceUsed + DocumentInfoElementName);
                if (xDocumentInfo != null)
                {
                    _documentInfo.Namespace = _fileNameSpace;
                    try
                    {
                        _documentInfo.Load(xDocumentInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading document info : {0}", ex.Message);
                    }
                }

                // загружает публикуемую информацию
                XElement xPublishInfo = xTextDescription.Element(namespaceUsed + ItemPublishInfo.PublishInfoElementName);
                if (xPublishInfo != null)
                {
                    _publishInfo.Namespace = _fileNameSpace;
                    try
                    {
                        _publishInfo.Load(xPublishInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading publishing info : {0}", ex.Message);
                    }
                }

                XElement xCustomInfo = xTextDescription.Element(namespaceUsed + ItemCustomInfo.CustomInfoElementName);
                if (xCustomInfo != null)
                {
                    var custElement = new ItemCustomInfo {Namespace = _fileNameSpace};
                    try
                    {
                        custElement.Load(xCustomInfo);
                        _customInfo.Add(custElement);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading custom info : {0}", ex.Message);
                    }
                }

                IEnumerable<XElement> xInstructions = xTextDescription.Elements(xTextDescription.Name.Namespace + "output");
                int outputCount = 0;
                _output.Clear();
                foreach (var xInstruction in xInstructions)
                {
                    // только два элемента разрешены схемой
                    if (outputCount > 1)
                    {
                        break;
                    }
                    var outp = new ShareInstructionType { Namespace = namespaceUsed };
                    try
                    {
                        outp.Load(xInstruction);
                        _output.Add(outp);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error reading output instructions : {0}", ex);
                    }
                    finally
                    {
                        outputCount++;
                    }
                }

                if (loadBinaryItems && _titleInfo.Cover != null)
                {

                    foreach (InlineImageItem coverImag in _titleInfo.Cover.CoverpageImages)
                    {
                        if (string.IsNullOrEmpty(coverImag.HRef))
                        {
                            continue;
                        }
                        string coverref = coverImag.HRef.Substring(0, 1) == "#" ? coverImag.HRef.Substring(1) : coverImag.HRef;
                        IEnumerable<XElement> xBinaryes =
                            fileDocument.Root.Elements(_fileNameSpace + Fb2BinaryElementName).Where(
                                cov => ((cov.Attribute("id") != null) && (cov.Attribute("id").Value == coverref)));
                        foreach (var binarye in xBinaryes)
                        {
                            var item = new BinaryItem();
                            try
                            {
                                item.Load(binarye);
                            }
                            catch (Exception)
                            {

                                continue;
                            }
                            // добавляет только уникальные идентификаторы, чтобы исправить некоторые недопустимые FB2
                            if (!_binaryObjects.ContainsKey(item.Id))
                            {
                                _binaryObjects.Add(item.Id, item);
                            }
                        }
                    }
                }
            }
        }
    }
}
