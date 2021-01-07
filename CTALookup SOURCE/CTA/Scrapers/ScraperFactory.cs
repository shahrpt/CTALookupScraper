using System;
using System.Collections.Generic;
using CTALookup.Arizona;

namespace CTALookup.Scrapers {
    public class ScraperFactory {
        private IList<Scraper> _scrapers;

        public ScraperFactory() {
            _scrapers = new List<Scraper>();
            AddScrapers();
        }

        public Scraper GetScraper(string county) {
            foreach (var scraper in _scrapers) {
                if (scraper.CanScrape(county)) {
                    return scraper;
                }
            }
            throw new Exception(string.Format("No scraper found for county {0}", county));
        }

        public void Add(Scraper scraper) {
            _scrapers.Add(scraper);
        }

        private void AddScrapers() {
            _scrapers = new List<Scraper>
                            {
                                new ScraperKershaw(),
                                new ScraperBeaufort(),
                                new ScraperSpartanburg(),
                                new ScraperHorry(),
                                new ScraperCharleston(),
                                new ScraperFairfield(),
                                new ScraperDorchester(),
                                new ScraperRichland(),
                                new ScraperLaurens(),
                                new ScraperGreenville(),
                                new ScraperOconee(),
                                new ScraperAnderson(),
                                new ScraperOrangeburg(),
                                new ScraperPickens(),
                                new ScraperLexington(),
                                new ScraperGreenwood(),
                                new ScraperYork(),
                                new ScraperGeorgetown(),
                                new ScraperAiken(),

                                //Arizona

                                new ScraperApache(),
                                new ScraperMohave(),
                                new ScraperMaricopa(),
                                new ScraperCoconino(),
                                new ScraperNavajo(),
                                new ScraperYavapai(),
                                new ScraperYuma(),
                                new ScraperLaPaz(),
                                new ScraperSantaCruz(),
                                new ScraperGraham(),
                                new ScraperGila(),
                                new ScraperPima(),
                                new ScraperPinal(),
                            };
        }
    }
}