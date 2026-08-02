using InfoTrack.Scraper;
using InfoTrack.Scraper.Html;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace InfoTrack.Tests
{
    public class ScraperTests
    {
        IOptions<ScraperConfigOptions> ScraperConfig;
        string htmlFilePath = "../../../sample.html";
        string htmlFile = string.Empty;

        public ScraperTests()
        {
            ScraperConfig = Options.Create(new ScraperConfigOptions
            {
                TargetUrl = "https://www.solicitors.com/london-solicitors.html",
                Selectors = new ScraperSelectorOptions
                {
                    Listing = new HtmlSelector { Tag = "div", ClassContains = "result-item" },
                    Rating = new HtmlSelector { Tag = "div", ClassContains = "star-full rating-lrg" },
                    Name = new HtmlSelector { Tag = "span", ClassContains = "h2" },
                    Phone = new HtmlSelector { Tag = "a", HrefStartsWith = "tel:" },
                    Email = new HtmlSelector { Tag = "a", HrefStartsWith = "mailto:" },
                    Address = new HtmlSelector { Tag = "address" },
                    Website = new HtmlSelector { Tag = "a", ClassContains = "link-map" },
                }
            });

            htmlFile = File.ReadAllText(htmlFilePath);
        }

        [Theory]
        [InlineData("Horne Engall & Freeman")]
        public async Task GIVEN_HTML_CHECK_IF_LAWYER_NAME_CAN_BE_FOUND(string name)
        {
            var parser = HtmlParser.Parse(htmlFile);
            var searchedName = parser.FindFirst(ScraperConfig.Value.Selectors.Name);
            Assert.Equal(name, searchedName.Children[0].InnerText);
        }

        [Theory]
        [InlineData("08082782864")]
        public async Task GIVEN_HTML_CHECK_IF_LAWYER_PHONE_EXISTS(string phone)
        {
            var parser = HtmlParser.Parse(htmlFile);
            var searchedPhone = parser.FindAll(ScraperConfig.Value.Selectors.Phone);
            Assert.Equal(phone, searchedPhone.FirstOrDefault(p => p.Attributes.ContainsKey("href") &&
                p.Attributes["href"].StartsWith("tel:") && p.Attributes["href"].Substring(4) == phone)?.Attributes["href"].Substring(4));
        }

        [Fact]
        public async Task GIVEN_HTML_CHECK_IF_SELECTORS_WORK()
        {
            var parser = HtmlParser.Parse(htmlFile);

            var searchedName = parser.FindFirst(ScraperConfig.Value.Selectors.Name);
            var searchedWebSite = parser.FindFirst(ScraperConfig.Value.Selectors.Website).GetAttribute("href");
            var searchedAddress = parser.FindFirst(ScraperConfig.Value.Selectors.Address);
            var searchedReviews = parser.FindAll(ScraperConfig.Value.Selectors.Rating);

            Assert.True(!string.IsNullOrEmpty(searchedName.Children[0].InnerText) && !string.IsNullOrEmpty(searchedAddress.InnerText) && searchedReviews.Count() > 0);
        }

        [Theory]
        [InlineData("57 Staines Road West, Sunbury-on-thames, Surrey TW16 7AU")]
        public async Task GIVEN_HTML_CHECK_IF_LAWYER_ADDRESS_CAN_BE_FOUND(string address)
        {
            var parser = HtmlParser.Parse(htmlFile);
            var searchedAddress = parser.FindFirst(ScraperConfig.Value.Selectors.Address);

            Assert.Equal(address, searchedAddress.InnerText);
        }

        [Theory]
        [InlineData("/horne-engall-and-freeman.html")]
        public async Task GIVEN_HTML_CHECK_IF_LAWYER_WEBSITE_LINK_CAN_BE_FOUND(string href)
        {
            var parser = HtmlParser.Parse(htmlFile);
            var searchedWebsite = parser.FindFirst(ScraperConfig.Value.Selectors.Website).GetAttribute("href");

            Assert.Equal(href, searchedWebsite);
        }

        [Fact]
        public async Task GIVEN_HTML_CHECK_IF_ALL_LAWYER_NAMES_CAN_BE_FOUND()
        {
            var parser = HtmlParser.Parse(htmlFile);
            var names = parser.FindAll(ScraperConfig.Value.Selectors.Name)
                .Select(n => n.Children[0].InnerText)
                .ToList();

            Assert.Equal(new List<string>
            {
                "Horne Engall & Freeman",
                "Horne Engall & Freeman",
                "QualitySolicitors",
                "BARON GREY SOLICITORS"
            }, names);
        }

        
        [Fact]
        public async Task GIVEN_HTML_CHECK_IF_ALL_LAWYER_PHONE_NUMBERS_CAN_BE_FOUND()
        {
            var parser = HtmlParser.Parse(htmlFile);
            var phones = parser.FindAll(ScraperConfig.Value.Selectors.Phone)
                .Select(p => p.Attributes["href"])
                .ToList();

            Assert.Equal(5, phones.Count);
        }

        [Fact]
        public void PARSE_SAMPLE_HTML_SKIPSDOCTYPE_AND_ROOT_HAS_SINGLEHTMLELEMENT()
        {
            var root = HtmlParser.Parse(htmlFile);

            Assert.Single(root.Children);
            Assert.Equal("html", root.Children[0].TagName);
            Assert.Equal("en", root.Children[0].Attributes["lang"]);
        }

        [Theory]
        [InlineData("img")]
        [InlineData("br")]
        [InlineData("link")]
        [InlineData("meta")]
        [InlineData("input")]
        public void PARSE_SAMPLE_HTML_VOID_ELEMENTS_ARE_NEVER_ADDED_TO_TREE(string voidTag)
        {
            var root = HtmlParser.Parse(htmlFile);
            var found = root.FindAll(new HtmlSelector { Tag = voidTag });

            Assert.Empty(found);
        }

        [Fact]
        public void PARSE_SAMPLE_HTML_SCRIPT_TAGS_ARE_KEPT_IN_TREE_BUT_BODY_IS_NOT_PARSED()
        {
            var root = HtmlParser.Parse(htmlFile);
            var scripts = root.FindAll(new HtmlSelector { Tag = "script" });

            Assert.Equal(9, scripts.Count());
            Assert.All(scripts, s => Assert.Empty(s.Children));
        }
        
        [Fact]
        public void PARSE_SAMPLE_HTML_HANDLES_SINGLE_QUOTED_ATTRIBUTE_WITH_EMBEDDED_DOUBLE_QUOTES()
        {
            var root = HtmlParser.Parse(htmlFile);
            var select = root.FindAll(new HtmlSelector { Tag = "select" })
                .First(s => s.Attributes.ContainsKey("data-jcf"));

            Assert.NotEmpty(select.Attributes["data-jcf"]);
            Assert.Equal("Select area of law", select.Attributes["title"]);
        }
    }
}