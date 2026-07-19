using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace ClubSite.Models;

[Table("ClubMemberships")]
public class ClubMembership
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
    public DateTime JoinDate { get; set; } = DateTime.Now;

    [Required]
    public DateTime StatusChangeDate { get; set; } = DateTime.Now;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public DateTime? RenewalDueDate { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    public DateTime? EndDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SubscriptionHistory> SubscriptionHistory { get; set; } = new List<SubscriptionHistory>();
}