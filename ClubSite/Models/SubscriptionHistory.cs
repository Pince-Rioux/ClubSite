using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace ClubSite.Models;

[Table("SubscriptionHistory")]
public class SubscriptionHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string RoleId { get; set; } = string.Empty;

    [Required]
    public DateTime EffectiveDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(50)]
    public string ChangeType { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Details { get; set; }

    public bool IsProcessed { get; set; }

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey(nameof(ClubMembershipId))]
    public virtual ClubMembership? ClubMembership { get; set; }

    public Guid? ClubMembershipId { get; set; }
}