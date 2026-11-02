// Copyright (C) axuno gGmbH and Contributors.
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
// https://github.com/axuno/ClubSite

#nullable enable

using ClubSite.Library;
using NUnit.Framework;

namespace ClubSite.Tests.Library;

[TestFixture]
public class EmailValidatorTests
{
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase(" ", false)]
    [TestCase("\t\r\n", false)]
    [TestCase("member@example.org", true)]
    [TestCase("member+training@example.org", true)]
    [TestCase("member@@example.org", false)]
    public void IsValid_ReturnsExpectedResult(string? email, bool expected)
    {
        Assert.That(EmailValidator.IsValid(email), Is.EqualTo(expected));
    }
}
