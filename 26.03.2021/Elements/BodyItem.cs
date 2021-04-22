using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace FB2Library.Elements
{
    /// <summary>
    /// Основное содержание книги, несколько тел используются для дополнительной информации, такой как сноски, которые не появляются в основном потоке книги.
    /// Первое тело представляется читателю по умолчанию, а содержимое других тел должно быть доступно по гиперссылкам.
    /// Атрибут имени должен описывать значение этого тела, это необязательно для основного тела.
    /// </summary>
    public class BodyItem : IFb2TextItem
    {
        private readonly XNamespace linkNamespace = @"http://www.w3.org/1999/xlink";

        private readonly List<SectionItem> sections = new List<SectionItem>();
        private readonly List<EpigraphItem> epigraphs = new List<EpigraphItem>();
        private readonly TitleItem title = new TitleItem();

        private const string Fb2NameAttributeName = "name";
        private const string Fb2ImageElementName = "image";

        public XNamespace NameSpace { get; set; }

        public string Name { get; set; }
        public TitleItem Title { get { return title; } }
        public ImageItem ImageName { get; set; }
        public List<SectionItem> Sections { get { return sections; } }
        public List<EpigraphItem> Epigraphs { get { return epigraphs; } }
        public string Lang { get; set; }

        internal const string Fb2BodyItemName = "body";

        internal void Load(XElement xBody)
        {
            if (xBody == null)
            {
                throw new ArgumentNullException("xBody");
            }

            if (xBody.Name.LocalName != Fb2BodyItemName)
            {
                throw new ArgumentException("Element of wrong type passed", "xBody");
            }


            ImageName = null;
            XElement xImage = xBody.Element(NameSpace + Fb2ImageElementName);
            if ((xImage != null))
            {
                ImageName = new ImageItem();
                try
                {
                    ImageName.Load(xImage);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Error reading body image: {0}", ex.Message));
                }
            }


            XElement xTitle = xBody.Element(NameSpace + TitleItem.Fb2TitleElementName);
            if ((xTitle != null) && (xTitle.Value != null))
            {
                try
                {
                    title.Load(xTitle);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Error reading body title: {0}", ex.Message));
                }
            }


            // Загружает элементы эпиграфа
            IEnumerable<XElement> xEpigraphs = xBody.Elements(NameSpace + EpigraphItem.Fb2EpigraphElementName);
            epigraphs.Clear();
            foreach (var xEpigraph in xEpigraphs)
            {
                EpigraphItem item = new EpigraphItem();
                try
                {
                    item.Load(xEpigraph);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Error reading body epigraph: {0}", ex.Message));
                    continue;
                }
                epigraphs.Add(item);
            }

            // Загружает элементы тела (сначала основной текст)
            IEnumerable<XElement> xSections = xBody.Elements(NameSpace + SectionItem.Fb2TextSectionElementName);
            sections.Clear();
            foreach (var section in xSections)
            {
                SectionItem item = new SectionItem();
                try
                {
                    item.Load(section);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Error reading body sections: {0}", ex.Message));
                    continue;
                }
                sections.Add(item);
            }

            Name = string.Empty;
            XAttribute xName = xBody.Attribute(Fb2NameAttributeName);
            if ((xName != null) && (xName.Value != null))
            {
                Name = xName.Value;
            }

            Lang = null;
            XAttribute xLang = xBody.Attribute(XNamespace.Xml + "lang");
            if ((xLang != null) && (xLang.Value != null))
            {
                Lang = xLang.Value;
            }
        }

        public XNode ToXML()
        {
            XElement xBody = new XElement(Fb2Const.fb2DefaultNamespace + Fb2BodyItemName);
            if (!string.IsNullOrEmpty(Name))
            {
                xBody.Add(new XAttribute(Fb2NameAttributeName, Name));
            }
            if (!string.IsNullOrEmpty(Lang))
            {
                xBody.Add(new XAttribute(XNamespace.Xml + "lang", Lang));
            }
            if (ImageName != null)
            {
                xBody.Add(ImageName.ToXML());
            }
            if (Title != null)
            {
                xBody.Add(Title.ToXML());
            }
            foreach (EpigraphItem EpItem in epigraphs)
            {
                xBody.Add(EpItem.ToXML());
            }
            foreach (SectionItem SecItem in sections)
            {
                xBody.Add(SecItem.ToXML());
            }

            return xBody;
        }
    }
}
