using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClubSite.Models;
using ClubSite.Services;

namespace ClubSite.Pages;

[Authorize(Roles = "admin")]
public class ManageMembersModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly IMembershipService _membershipService;
    private readonly ILogger<ManageMembersModel> _logger;

    public ManageMembersModel(
        UserManager<User> userManager,
        IMembershipService membershipService,
        ILogger<ManageMembersModel> logger)
    {
        _userManager = userManager;
        _membershipService = membershipService;
        _logger = logger;
    }

    public List<MemberListItem> Members { get; set; } = new();
    public string? StatusMessage { get; set; }
    public bool IsSuccess { get; set; }

    public class MemberListItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MemberSince { get; set; }
        public string? SubscriptionLevel { get; set; }
        public string? MembershipNumber { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

            Members = users.Select(u => new MemberListItem
            {
                Id = u.Id,
                Name = u.UserName ?? u.Email ?? "Unbekannt",
                Email = u.Email ?? string.Empty,
                Phone = u.PhoneNumber,
                Status = u.EndDate < DateTime.UtcNow ? "Abgelaufen" : "Aktiv",
                MemberSince = u.StartDate.ToString("dd.MM.yyyy"),
                SubscriptionLevel = u.SubscriptionLevel,
                MembershipNumber = u.MembershipNumber
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load members list");
            StatusMessage = "Fehler beim Laden der Mitgliederliste.";
            IsSuccess = false;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostToggleStatusAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage();

        try
        {
            await _membershipService.CancelMembershipAsync(userId);
            StatusMessage = "Mitgliedsstatus wurde aktualisiert.";
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle status for user {UserId}", userId);
            StatusMessage = "Fehler beim Aktualisieren des Status.";
            IsSuccess = false;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    StatusMessage = "Mitglied wurde gelöscht.";
                    IsSuccess = true;
                }
                else
                {
                    StatusMessage = "Fehler beim Löschen des Mitglieds.";
                    IsSuccess = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            StatusMessage = "Fehler beim Löschen des Mitglieds.";
            IsSuccess = false;
        }

        return RedirectToPage();
    }
}