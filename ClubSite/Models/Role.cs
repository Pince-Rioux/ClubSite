using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace ClubSite.Models;

public class Role : IdentityRole<string>
{
    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Rank { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedOn { get; set; }

    public DateTime? LastModified { get; set; }

    // Navigation
    public virtual ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
}