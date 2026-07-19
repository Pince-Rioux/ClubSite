using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ClubSite.Data;
using ClubSite.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ClubSite.Services;

public class DefaultMembershipService : IMembershipService
{
    private readonly ClubContext _context;
    private readonly ILogger<DefaultMembershipService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IMailService _mailService;
    private readonly ClubMembershipSettings _settings;

    public DefaultMembershipService(
        ClubContext context,
        ILogger<DefaultMembershipService> logger,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IMailService mailService,
        IOptions<ClubMembershipSettings> settings)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
        _mailService = mailService;
        _settings = settings.Value;
    }

    public async Task<bool> RegisterMemberAsync(User user, string password, string roleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for duplicate membership number
            var existing = await _context.Users
                .FirstOrDefaultAsync(u => u.MembershipNumber == user.MembershipNumber, cancellationToken);

            if (existing != null)
            {
                _logger.LogWarning("Membership number already exists: {MembershipNumber}", user.MembershipNumber);
                return false;
            }

            user.UserName ??= user.Email;

            // Create the user
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                _logger.LogError("Error creating user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }

            // Assign role
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogError("Role does not exist: {Role}", roleName);
                await _userManager.DeleteAsync(user);
                return false;
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Error assigning role {Role}: {Errors}", roleName,
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                await _userManager.DeleteAsync(user);
                return false;
            }

            // Create ClubMembership record
            var membership = new ClubMembership
            {
                UserId = user.Id,
                RoleId = (await _roleManager.FindByNameAsync(roleName))!.Id,
                JoinDate = DateTime.UtcNow,
                Status = "Active",
                StatusChangeDate = DateTime.UtcNow,
                RenewalDueDate = DateTime.UtcNow.AddMonths(_settings.DefaultMembershipDurationInMonths)
            };

            _context.ClubMemberships.Add(membership);
            await _context.SaveChangesAsync(cancellationToken);

            // Send welcome email
            if (_settings.SendWelcomeEmailOnRegistration)
            {
                await SendWelcomeEmailAsync(user.Email!, user.UserName ?? user.Email, roleName,
                    cancellationToken);
            }

            _logger.LogInformation("Registered member {UserId} with role {Role}", user.Id, roleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering member");
            return false;
        }
    }

    public async Task<bool> RenewMembershipAsync(string userId, string newRoleName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            var activeMembership = user.Memberships
                .FirstOrDefault(m => m.Status == "Active");

            if (activeMembership == null)
            {
                _logger.LogWarning("No active membership for user {UserId}", userId);
                return false;
            }

            var newRole = await _roleManager.FindByNameAsync(newRoleName);
            if (newRole == null)
            {
                _logger.LogWarning("Role not found: {Role}", newRoleName);
                return false;
            }

            // Mark old membership
            activeMembership.Status = "Renewed";
            activeMembership.StatusChangeDate = DateTime.UtcNow;

            // Create new membership
            var newMembership = new ClubMembership
            {
                UserId = userId,
                RoleId = newRole.Id,
                JoinDate = DateTime.UtcNow,
                Status = "Active",
                StatusChangeDate = DateTime.UtcNow,
                RenewalDueDate = DateTime.UtcNow.AddMonths(_settings.DefaultMembershipDurationInMonths)
            };

            _context.ClubMemberships.Add(newMembership);

            // Switch roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRoleName);

            // History entry
            _context.SubscriptionHistories.Add(new SubscriptionHistory
            {
                UserId = userId,
                RoleId = newRole.Id,
                EffectiveDate = DateTime.UtcNow,
                ChangeType = "Renewal",
                Details = $"Renewed to {newRoleName}",
                ClubMembershipId = newMembership.Id
            });

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Renewed membership for user {UserId} to {Role}", userId, newRoleName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing membership for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> CancelMembershipAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Memberships)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            var activeMembership = user.Memberships
                .FirstOrDefault(m => m.Status == "Active");

            if (activeMembership == null)
            {
                _logger.LogWarning("No active membership for user {UserId}", userId);
                return false;
            }

            activeMembership.Status = "Cancelled";
            activeMembership.StatusChangeDate = DateTime.UtcNow;

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
                await _userManager.RemoveFromRolesAsync(user, roles);

            _context.SubscriptionHistories.Add(new SubscriptionHistory
            {
                UserId = userId,
                RoleId = activeMembership.RoleId,
                EffectiveDate = DateTime.UtcNow,
                ChangeType = "Cancellation",
                Details = "Membership cancelled",
                ClubMembershipId = activeMembership.Id
            });

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled membership for user {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling membership for user {UserId}", userId);
            return false;
        }
    }

    public async Task<List<ClubMembership>> GetMembershipHistoryAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClubMemberships
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.JoinDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetUserWithMembershipAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Memberships)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<bool> UpdateMemberProfileAsync(MemberProfile profile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.MemberProfiles
                .FirstOrDefaultAsync(p => p.UserId == profile.UserId, cancellationToken);

            if (existing == null)
            {
                _context.MemberProfiles.Add(profile);
            }
            else
            {
                existing.HomeAddress = profile.HomeAddress;
                existing.Phone = profile.Phone;
                existing.City = profile.City;
                existing.State = profile.State;
                existing.ZipCode = profile.ZipCode;
                existing.EmergencyContactInfo = profile.EmergencyContactInfo;
                existing.Preferences = profile.Preferences;
                existing.SpecialNeeds = profile.SpecialNeeds;
                existing.ProfileUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated profile for user {UserId}", profile.UserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", profile.UserId);
            return false;
        }
    }

    public async Task<bool> ValidateMembershipAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _context.ClubMemberships
            .Where(m => m.UserId == userId && m.Status == "Active")
            .OrderByDescending(m => m.JoinDate)
            .FirstOrDefaultAsync(cancellationToken);

        return membership != null &&
               (membership.EndDate == null || membership.EndDate > DateTime.UtcNow);
    }

    public async Task<string> GenerateMembershipNumberAsync(
        CancellationToken cancellationToken = default)
    {
        var prefix = "M-";
        var suffix = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = new Random();
        string number;

        do
        {
            number = $"{prefix}{suffix}-{random.Next(1000, 9999)}";
        } while (await _context.Users.AnyAsync(u => u.MembershipNumber == number, cancellationToken));

        return number;
    }

    private async Task SendWelcomeEmailAsync(string email, string userName, string roleName,
        CancellationToken cancellationToken)
    {
        var subject = _settings.WelcomeEmailSubject;
        var body = $"""
                    Liebe(r) {userName},

                    herzlich willkommen im {roleName}-Programm unseres Clubs!

                    Ihre Mitgliedschaft ist jetzt aktiv.
                    Vielen Dank für Ihr Vertrauen.

                    Mit freundlichen Grüßen,
                    Ihr Club-Team
                    """;

        await _mailService.SendTextEmailAsync(userName, email, subject, body, cancellationToken);
    }
}