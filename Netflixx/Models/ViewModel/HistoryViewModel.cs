using Netflixx.Models;
using System;
using System.Collections.Generic;

namespace Netflixx.Models.ViewModel
{
    public class HistoryViewModel
    {
        // when the user registered (for the live counter)
        public DateTime RegisteredAt { get; set; }

        // distinct “YYYY-MM” strings for the dropdown
        public List<string> AvailablePeriods { get; set; }

        // which period is currently selected
        public string SelectedPeriod { get; set; }

        // the films watched in that period
        public List<FilmsModel> FilmsThisPeriod { get; set; }

        // placeholder for the “based on watched” section
        public List<FilmsModel> RecommendedFilms { get; set; }
    }
}
