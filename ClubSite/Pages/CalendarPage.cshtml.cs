// Copyright (C) axuno gGmbH and Contributors.
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
// https://github.com/axuno/ClubSite

using System;
using System.Collections.Generic;
using System.Linq;
using ClubSite.Services;
using Microsoft.AspNetCore.Mvc;
using Piranha.AspNetCore.Models;
using Piranha.AspNetCore.Services;

namespace ClubSite.Pages;

[BindProperties]
public class CalendarPageModel : SinglePage<Models.CalendarPage>
{
    private readonly IcsCalendarService _calendarService;

    public CalendarPageModel(IcsCalendarService calendarService, Piranha.IApi api, IModelLoader loader)
        : base(api, loader)
    {
        _calendarService = calendarService;
    }

    /// <summary>
    /// Filter: Team (z.B. Damen, Herren, Jugend, Mixed, Alle).
    /// Empty = alle Teams.
    /// </summary>
    public string TeamFilter { get; set; } = string.Empty;

    /// <summary>
    /// Filter: Kategorie (z.B. Training, Spieltag, Turnier, Sonstiges).
    /// Empty = alle Kategorien.
    /// </summary>
    public string CategoryFilter { get; set; } = string.Empty;

    /// <summary>
    /// Anzeigemodus: "upcoming" (Standard) oder "past".
    /// </summary>
    public string Mode { get; set; } = "upcoming";

    /// <summary>
    /// Die gefilterten Events für die aktuelle Ansicht.
    /// </summary>
    public List<IcsCalendarService.CalendarEventItem> Events { get; set; } = new();

    /// <summary>
    /// Verfügbare Teams für den Filter-Dropdown.
    /// </summary>
    public List<string> AvailableTeams { get; set; } = new();

    /// <summary>
    /// Verfügbare Kategorien für den Filter-Dropdown.
    /// </summary>
    public List<string> AvailableCategories { get; set; } = new();

    public bool HasIcsFiles => _calendarService.HasCalendarFiles();

    /// <summary>
    /// Initialisiert die Daten beim ersten Zugriff (lazy).
    /// Wird vom CalendarPage.cshtml-Template aufgerufen.
    /// </summary>
    public void InitializePage()
    {
        if (Events.Any()) return; // bereits initialisiert
        LoadFilterFromQueryString();
        LoadEvents();
    }

    private void LoadFilterFromQueryString()
    {
        // Namen muessen mit den asp-for-Attributen im Formular uebereinstimmen
        TeamFilter = Request.Query["TeamFilter"].FirstOrDefault() ?? string.Empty;
        CategoryFilter = Request.Query["CategoryFilter"].FirstOrDefault() ?? string.Empty;
        Mode = Request.Query["Mode"].FirstOrDefault() ?? "upcoming";
    }

    private void LoadEvents()
    {
        // Determine date range
        var now = DateTime.Now;

        DateTime rangeStart, rangeEnd;

        if (Mode == "past")
        {
            rangeStart = now.AddMonths(-6);
            rangeEnd = now;
        }
        else
        {
            rangeStart = now.AddMonths(-1);
            rangeEnd = now.AddMonths(6);
        }

        var allEvents = _calendarService.GetEvents(rangeStart, rangeEnd);

        AvailableTeams = _calendarService.GetAvailableTeams();
        AvailableCategories = _calendarService.GetAvailableCategories();

        // Apply filters
        var filtered = allEvents.AsEnumerable();

        if (!string.IsNullOrEmpty(TeamFilter) && TeamFilter != "Alle")
            filtered = filtered.Where(e => e.Team == TeamFilter);

        if (!string.IsNullOrEmpty(CategoryFilter) && CategoryFilter != "Alle")
            filtered = filtered.Where(e => e.Category == CategoryFilter);

        if (Mode == "past")
            filtered = filtered.Where(e => e.StartDate < now);
        else
            filtered = filtered.Where(e => e.StartDate >= now);

        Events = filtered.OrderBy(e => e.StartDate).ToList();
    }
}