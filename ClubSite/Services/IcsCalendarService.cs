// Copyright (C) axuno gGmbH and Contributors.
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
// https://github.com/axuno/ClubSite

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Evaluation;
using Microsoft.AspNetCore.Hosting;

namespace ClubSite.Services;

/// <summary>
/// Service to load, parse and filter .ics calendar files using iCal.net.
/// </summary>
public class IcsCalendarService
{
    private readonly string _calendarDirectory;

    /// <summary>
    /// Represents a single calendar event ready for display.
    /// </summary>
    public class CalendarEventItem
    {
        public string Title { get; init; } = string.Empty;
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public bool IsAllDay => StartDate.TimeOfDay == TimeSpan.Zero && EndDate.TimeOfDay == TimeSpan.Zero;
        public string Location { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Team { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    public IcsCalendarService(IWebHostEnvironment env)
    {
        _calendarDirectory = Path.Combine(env.ContentRootPath, "App_Data", "calendar");
    }

    /// <summary>
    /// Loads all .ics files from the calendar directory and returns events
    /// within the specified date range (evaluating RRULE for recurring events).
    /// </summary>
    public List<CalendarEventItem> GetEvents(DateTime rangeStart, DateTime rangeEnd)
    {
        var items = new List<CalendarEventItem>();

        if (!Directory.Exists(_calendarDirectory))
            return items;

        foreach (var filePath in Directory.GetFiles(_calendarDirectory, "*.ics"))
        {
            try
            {
                var icsContent = File.ReadAllText(filePath);
                var calendar = Ical.Net.Calendar.Load(icsContent);
                if (calendar != null)
                    items.AddRange(ParseCalendar(calendar, rangeStart, rangeEnd));
            }
            catch (Exception)
            {
                // Skip malformed files (log could be added later)
            }
        }

        return items.OrderBy(e => e.StartDate).ToList();
    }

    /// <summary>
    /// Returns distinct team values from all .ics files (for filter dropdowns).
    /// </summary>
    public List<string> GetAvailableTeams()
    {
        return GetDistinctFieldValues(e => e.Team);
    }

    /// <summary>
    /// Returns distinct category values from all .ics files (for filter dropdowns).
    /// </summary>
    public List<string> GetAvailableCategories()
    {
        return GetDistinctFieldValues(e => e.Category);
    }

    public bool HasCalendarFiles()
    {
        return Directory.Exists(_calendarDirectory) &&
               Directory.GetFiles(_calendarDirectory, "*.ics").Length > 0;
    }

    private List<CalendarEventItem> ParseCalendar(Ical.Net.Calendar calendar, DateTime rangeStart, DateTime rangeEnd)
    {
        var allOccurrences = calendar.GetOccurrences(
            new CalDateTime(rangeStart),
            new EvaluationOptions());

        var items = new List<CalendarEventItem>();

        foreach (var occurrence in allOccurrences)
        {
            if (occurrence.Source is not CalendarEvent sourceEvent)
                continue;

            var startDt = occurrence.Period.StartTime.AsUtc;
            startDt = DateTime.SpecifyKind(startDt, DateTimeKind.Utc);

            var endDt = occurrence.Period.EndTime?.AsUtc;
            if (endDt == null && occurrence.Period.Duration.HasValue)
            {
                var duration = occurrence.Period.Duration.Value.ToTimeSpanUnspecified();
                endDt = startDt + duration;
            }
            endDt ??= startDt;
            endDt = DateTime.SpecifyKind(endDt.Value, DateTimeKind.Utc);

            // Filter by endDate range - occurrences before rangeStart or after rangeEnd
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(startDt, TimeZoneInfo.Local);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(endDt.Value, TimeZoneInfo.Local);

            if (localStart > rangeEnd)
                continue;

            var (team, category) = ParseCategories(sourceEvent.Categories);

            items.Add(new CalendarEventItem
            {
                Title = sourceEvent.Summary ?? "(kein Titel)",
                StartDate = localStart,
                EndDate = localEnd,
                Location = sourceEvent.Location ?? string.Empty,
                Description = sourceEvent.Description ?? string.Empty,
                Team = team,
                Category = category
            });
        }

        return items;
    }

    private static (string team, string category) ParseCategories(IList<string> categories)
    {
        var team = string.Empty;
        var category = string.Empty;

        if (categories is not { Count: > 0 })
            return (team, category);

        team = categories[0];

        if (categories.Count > 1)
            category = categories[1];

        return (team, category);
    }

    private List<string> GetDistinctFieldValues(Func<CalendarEventItem, string> selector)
    {
        var values = new HashSet<string>();
        var rangeEnd = DateTime.Now.AddYears(2);
        var rangeStart = DateTime.Now.AddYears(-1);

        foreach (var item in GetEvents(rangeStart, rangeEnd))
        {
            var val = selector(item);
            if (!string.IsNullOrEmpty(val))
                values.Add(val);
        }

        return values.OrderBy(v => v).ToList();
    }
}