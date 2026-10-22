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
    private readonly string _webRoot;

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
        // WebRoot (wwwroot) fuer Media-Uploads merken
        _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
    }

    /// <summary>
    /// Loads all calendar files from the calendar directory and returns events
    /// within the specified date range (evaluating RRULE for recurring events).
    /// Die Dateien koennen die Endung .ics oder .pdf haben – Piranha
    /// erlaubt im Media-Manager nur Bilder und PDFs hochzuladen, daher
    /// wird die Kalenderdatei als .pdf abgelegt.
    /// </summary>
    public List<CalendarEventItem> GetEvents(DateTime rangeStart, DateTime rangeEnd)
    {
        var items = new List<CalendarEventItem>();

        // Quelle: Media-Uploads (wwwroot/uploads/<guid>/events.pdf) rekursiv finden
        var calendarFiles = new List<string>();
        var uploadsRoot = Path.Combine(_webRoot, "uploads");
        if (Directory.Exists(uploadsRoot))
        {
            calendarFiles.AddRange(Directory.GetFiles(uploadsRoot, "events.pdf", SearchOption.AllDirectories));
            calendarFiles.AddRange(Directory.GetFiles(uploadsRoot, "events.ics", SearchOption.AllDirectories));
        }

        calendarFiles = calendarFiles.Distinct().ToList();

        // Wenn mehrere vorhanden: nur die neueste Datei verwenden
        if (calendarFiles.Count > 1)
        {
            calendarFiles = new List<string>
            {
                calendarFiles.OrderByDescending(f => System.IO.File.GetLastWriteTime(f)).First()
            };
        }

        foreach (var filePath in calendarFiles)
        {
            try
            {
                var icsContent = System.IO.File.ReadAllText(filePath);
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

    /// <summary>
    /// Das Verzeichnis, in dem .ics-Dateien gespeichert werden.
    /// </summary>
    public bool HasCalendarFiles()
    {
        var uploadsRoot = Path.Combine(_webRoot, "uploads");
        if (!Directory.Exists(uploadsRoot))
            return false;

        return Directory.GetFiles(uploadsRoot, "events.pdf", SearchOption.AllDirectories).Length > 0 ||
               Directory.GetFiles(uploadsRoot, "events.ics", SearchOption.AllDirectories).Length > 0;
    }

    private List<CalendarEventItem> ParseCalendar(Ical.Net.Calendar calendar, DateTime rangeStart, DateTime rangeEnd)
    {
        var items = new List<CalendarEventItem>();

        foreach (var component in calendar.Events)
        {
            CalendarEvent? ev = component as CalendarEvent;
            if (ev == null) continue;

            string team = string.Empty, category = string.Empty;
            if (ev.Categories != null && ev.Categories.Count > 0)
            {
                team = ev.Categories[0];
                if (ev.Categories.Count > 1)
                    category = ev.Categories[1];
            }

            if (ev.DtStart == null) continue;
            var start = ToLocal(ev.DtStart);
            var end = ev.DtEnd != null ? ToLocal(ev.DtEnd) : start;
            var duration = end - start;

            if (ev.RecurrenceRule != null &&
                ev.RecurrenceRule.Frequency == FrequencyType.Weekly)
            {
                var until = rangeEnd;
                if (ev.RecurrenceRule.Until != null)
                    until = ToLocal(ev.RecurrenceRule.Until);
                if (until > rangeEnd) until = rangeEnd;

                var maxOcc = ev.RecurrenceRule.Count > 0 ? ev.RecurrenceRule.Count : 200;
                var interval = ev.RecurrenceRule.Interval > 0 ? ev.RecurrenceRule.Interval : 1;

                // Welche Wochentage? (0=Monday ... 6=Sunday)
                var dayNums = new List<int>();
                if (ev.RecurrenceRule.ByDay != null && ev.RecurrenceRule.ByDay.Count > 0)
                {
                    foreach (var wd in ev.RecurrenceRule.ByDay)
                    {
                        dayNums.Add((int)wd.DayOfWeek);
                    }
                }
                else
                {
                    dayNums.Add((int)start.DayOfWeek);
                }

                var cur = start > rangeStart ? start : rangeStart;
                var occCount = 0;

                while (cur <= until && occCount < maxOcc)
                {
                    foreach (var target in dayNums)
                    {
                        var diff = target - (int)cur.DayOfWeek;
                        if (diff < 0) diff += 7;
                        var occDate = cur.AddDays(diff);

                        if (occDate < start) continue;
                        if (occDate > until) break;

                        items.Add(new CalendarEventItem
                        {
                            Title = ev.Summary ?? "(kein Titel)",
                            StartDate = occDate + start.TimeOfDay,
                            EndDate = occDate + start.TimeOfDay + duration,
                            Location = ev.Location ?? string.Empty,
                            Description = ev.Description ?? string.Empty,
                            Team = team,
                            Category = category
                        });
                        if (++occCount >= maxOcc) break;
                    }
                    if (occCount >= maxOcc) break;
                    cur = cur.AddDays(7 * interval);
                }
            }
            else
            {
                // Einmaliges Event (oder andere RRULE-Frequenz)
                items.Add(new CalendarEventItem
                {
                    Title = ev.Summary ?? "(kein Titel)",
                    StartDate = start,
                    EndDate = end,
                    Location = ev.Location ?? string.Empty,
                    Description = ev.Description ?? string.Empty,
                    Team = team,
                    Category = category
                });
            }
        }
        return items;
    }

    private DateTime ToLocal(CalDateTime cal)
    {
        if (cal == null) return DateTime.Now;
        if (cal.IsUtc)
            return TimeZoneInfo.ConvertTimeFromUtc(cal.Value, TimeZoneInfo.Local);
        return cal.Value;
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