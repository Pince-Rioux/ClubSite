// Copyright (C) axuno gGmbH and Contributors.
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
// https://github.com/axuno/ClubSite

using ClubSite.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace ClubSite.Helpers;

/// <summary>
/// Shared option definitions for form dropdowns.
/// Single source of truth shared across ContactPage, TrialTrainingPage, etc.
/// </summary>
public static class FormOptions
{
    /// <summary>
    /// Returns the Gender (salutation) option list used by ContactPage.
    /// </summary>
    public static List<SelectListItem> GenderList()
    {
        return
        [
            new SelectListItem { Value = string.Empty, Text = "Bitte auswählen" },
            new SelectListItem { Value = "f", Text = "Frau" },
            new SelectListItem { Value = "m", Text = "Herr" },
            new SelectListItem { Value = "u", Text = "Keine Angabe" }
        ];
    }
}