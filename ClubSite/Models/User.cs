using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace ClubSite.Models;

public class User : IdentityUser<string>
{
    [Required]
    [StringLength(50)]
    public string MembershipNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string SubscriptionLevel { get; set; } = "Basic";

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Now;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(12);

    [StringLength(200)]
    public string? EmergencyContact { get; set; }

    [StringLength(1000)]
    public string? Preferences { get; set; }

    [StringLength(200)]
    public string? SpecialNeeds { get; set; }

    // Navigation properties
    public virtual ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();

    public virtual MemberProfile? Profile { get; set; }

    public virtual ICollection<SubscriptionHistory> SubscriptionHistory { get; set; } = new List<SubscriptionHistory>();
}