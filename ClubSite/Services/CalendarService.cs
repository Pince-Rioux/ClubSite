using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ClubSite.Services;

/// <summary>
/// Service to load, parse and filter .ics calendar files using iCal.Net.
/// </summary>
public class CalendarService : ICalendarService
{
    private const string CalendarFileName = "events.ics";
    private const string UploadsDir = "uploads";
    private const int MaxEvents = 300;

    private readonly string _webRoot;
    private readonly ILogger<CalendarService> _logger;
    private Calendar? _cachedCalendar;

    public CalendarService(IWebHostEnvironment env, ILogger<CalendarService> logger)
    {
        _webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _logger = logger;
    }

    public List<CalendarEventItem> GetEvents(DateTime rangeStart, DateTime rangeEnd)
    {
        var calendar = LoadCalendar();
        if (calendar == null) return [];

        var items = new List<CalendarEventItem>();
        int count = 0;
        foreach (var ev in calendar.Events)
        {
            if (ev.DtStart == null || count >= MaxEvents) continue;

            var start = ToCalLocal(ev.DtStart);
            var end = ev.DtEnd != null ? ToCalLocal(ev.DtEnd) : start;

            var (team, category) = ParseCategories(ev);
            // ICal.Net 6 Location property returns only venue name, not full address
            // Use raw property value for the complete address
            string location = ev.Location?.ToString() ?? string.Empty;

            if (IsRecurring(ev))
            {
                ExpandOccurrences(ev, start, end, rangeStart, rangeEnd, team, category, location, items);
            }
            else
            {
                if (end < rangeStart || start > rangeEnd) continue;
                items.Add(MakeItem(ev, start, end, team, category, location));
                count++;
            }
        }
        return [.. items.OrderBy(e => e.StartDate)];
    }

    public List<string> GetAvailableTeams() =>
        CollectDistinct(e => e.Team);

    public List<string> GetAvailableCategories() =>
        CollectDistinct(e => e.Category);

    public bool HasCalendarFiles()
    {
        return LoadCalendar() != null;
    }

    private Calendar? LoadCalendar()
    {
        if (_cachedCalendar != null) return _cachedCalendar;

        var calendarFile = FindCalendarFile();
        if (calendarFile == null) return null;

        try
        {
            var icsContent = File.ReadAllText(calendarFile);
            var calendars = CalendarSerializer.DeserializeCollection<Calendar>(icsContent);
            _cachedCalendar = calendars?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse calendar file {FilePath}", calendarFile);
        }
        return _cachedCalendar;
    }

    private string? FindCalendarFile()
    {
        var uploadsRoot = Path.Combine(_webRoot, UploadsDir);
        if (!Directory.Exists(uploadsRoot)) return null;

        var files = Directory.GetFiles(uploadsRoot, CalendarFileName, SearchOption.AllDirectories);
        if (files.Length == 0) return null;
        if (files.Length <= 1) return files[0];
        return files.OrderByDescending(File.GetLastWriteTime).First();
    }

    private static bool IsRecurring(CalendarEvent ev) =>
        ev.RecurrenceRule != null && ev.RecurrenceRule.Frequency == FrequencyType.Weekly;

    private static void ExpandOccurrences(CalendarEvent ev, DateTime start, DateTime end,
        DateTime rangeStart, DateTime rangeEnd, string team, string category, string location,
        List<CalendarEventItem> items)
    {
        var rule = ev.RecurrenceRule!;
        var duration = end - start;
        var until = rule.Until?.ToDateTimeUnspecified() ?? rangeEnd;
        if (until > rangeEnd) until = rangeEnd;

        var maxOcc = rule.Count > 0 ? rule.Count : 200;
        var interval = rule.Interval > 0 ? rule.Interval : 1;

        List<int> dayNums = [];
        if (rule.ByDay?.Count > 0)
            dayNums = [.. rule.ByDay.Select(wd => (int)wd.DayOfWeek)];
        else
            dayNums = [(int)start.DayOfWeek];

        var current = start > rangeStart ? start : rangeStart;
        var occCount = 0;
        while (current <= until && occCount < maxOcc)
        {
            foreach (var d in dayNums)
            {
                var diff = d - (int)current.DayOfWeek;
                if (diff < 0) diff += 7;
                var occDate = current.AddDays(diff);
                if (occDate < start || occDate > until) continue;
                items.Add(MakeItem(ev, occDate + start.TimeOfDay, occDate + start.TimeOfDay + duration, team, category, location));
                if (++occCount >= maxOcc) break;
            }
            if (occCount >= maxOcc) break;
            current = current.AddDays(7 * interval);
        }
    }

    private static CalendarEventItem MakeItem(CalendarEvent ev, DateTime start, DateTime end,
        string team, string category, string location) => new CalendarEventItem
    {
        Title = ev.Summary?.ToString() ?? "(no title)",
        StartDate = start,
        EndDate = end,
        Location = location,
        Description = ev.Description?.ToString() ?? string.Empty,
        Team = team,
        Category = category,
        IsHomeGame = IsHomeGame(category),
        Url = ev.Url?.ToString()
    };

    private static (string team, string category) ParseCategories(CalendarEvent ev)
    {
        if (ev.Categories is not { Count: > 0 })
            return (string.Empty, string.Empty);
        return (ev.Categories[0], ev.Categories.Count > 1 ? ev.Categories[1] : string.Empty);
    }

    private static DateTime ToLocal(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Utc)
            return TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.Local);
        return dt;
    }

    /// <summary>
    /// Converts a CalDateTime (iCal.Net 6) to a local DateTime.
    /// </summary>
    private static DateTime ToCalLocal(CalDateTime cal)
    {
        var dt = cal.ToDateTimeUnspecified();
        if (cal.IsUtc)
            return TimeZoneInfo.ConvertTimeFromUtc(dt, TimeZoneInfo.Local);
        return dt;
    }

    private static bool IsHomeGame(string category)
    {
        return !string.IsNullOrEmpty(category)
            && category.ToLowerInvariant().Contains("heimspieltag");
    }

    private List<string> CollectDistinct(Func<CalendarEventItem, string> selector)
    {
        var values = new HashSet<string>();
        foreach (var item in GetEvents(DateTime.Now.AddYears(-1), DateTime.Now.AddYears(2)))
        {
            var val = selector(item);
            if (!string.IsNullOrEmpty(val)) values.Add(val);
        }
        return [.. values.OrderBy(v => v)];
    }
}
