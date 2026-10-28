// Copyright (C) axuno gGmbH and Contributors.
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
// https://github.com/axuno/ClubSite

using ClubSite.Membership.Services;
using ClubSite.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace ClubSite;

/// <summary>
/// Host implementation of <see cref="IEmailSender"/> that delegates to ClubSite's existing
/// <see cref="IMailService"/> (MailKit + SMTP/File-based mail sending).
/// </summary>
public class ClubSiteEmailSender : IEmailSender
{
    private readonly IMailService _mailService;
    private readonly ILogger<ClubSiteEmailSender> _logger;

    public ClubSiteEmailSender(IMailService mailService, ILogger<ClubSiteEmailSender> logger)
    {
        _mailService = mailService;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        await _mailService.SendTextEmailAsync(to, to, subject, body, default);

        _logger.LogInformation(
            "Email sent via ClubSite MailService to <{To}>: {Subject}",
            to, subject);
    }
}