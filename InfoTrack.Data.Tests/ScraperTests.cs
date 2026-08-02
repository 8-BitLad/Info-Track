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

        public ScraperTests() {
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

        [Theory]
        [InlineData(1)]
        public async Task GIVEN_HTML_CHECK_IF_LAWYER_RATING_CAN_BE_EXTARCTED(int expectedRating)
        {
            var parser = HtmlParser.Parse(htmlFile);
            var searchedRating = parser.FindAll(ScraperConfig.Value.Selectors.Rating);
            Assert.True(expectedRating > searchedRating.Count());
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
    }
}