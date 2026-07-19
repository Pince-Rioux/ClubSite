using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace ClubSite.Models;

public class MemberProfile
{
    [Key]
    [MaxLength(128)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string HomeAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? EmergencyContactInfo { get; set; }

    [MaxLength(1000)]
    public string? Preferences { get; set; }

    [MaxLength(500)]
    public string? SpecialNeeds { get; set; }

    public DateTime? ProfileUpdated { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}