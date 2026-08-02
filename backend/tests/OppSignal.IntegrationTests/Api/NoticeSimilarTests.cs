using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OppSignal.Application.Notices;
using OppSignal.Domain.Entities;
using OppSignal.Domain.Enums;
using OppSignal.Infrastructure.Persistence;
using OppSignal.IntegrationTests.Support;
using Xunit;

namespace OppSignal.IntegrationTests.Api;

public class NoticeSimilarTests : ApiTestBase
{
    public NoticeSimilarTests(OppSignalWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Similar_ranks_same_naics_first_and_excludes_self_and_inactive()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"sim_{Guid.NewGuid():N}@test.dev");

        var anchorId = $"ANC-{Guid.NewGuid():N}"[..16];
        var sameNaicsId = $"NAI-{Guid.NewGuid():N}"[..16];
        var sameDeptId = $"DEP-{Guid.NewGuid():N}"[..16];
        var inactiveId = $"INA-{Guid.NewGuid():N}"[..16];
        var unrelatedId = $"UNR-{Guid.NewGuid():N}"[..16];

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.AddRange(
                Seed(anchorId, "541512", "DEPT OF DEFENSE"),
                Seed(sameNaicsId, "541512", "DEPT OF ENERGY"),           // NAICS match → score 2
                Seed(sameDeptId, "236220", "DEPT OF DEFENSE"),           // dept match only → score 1
                Seed(inactiveId, "541512", "DEPT OF DEFENSE", active: false), // excluded (inactive)
                Seed(unrelatedId, "999999", "DEPT OF INTERIOR"));        // excluded (no overlap)
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
            var similar = await notices.GetSimilarAsync(userId, anchorId);
            var ids = similar.Select(s => s.NoticeId).ToList();

            ids.Should().NotContain(anchorId, "the anchor itself is excluded");
            ids.Should().NotContain(inactiveId, "inactive notices are excluded");
            ids.Should().NotContain(unrelatedId, "notices with no NAICS/department overlap are excluded");
            // Exactly the two overlapping notices, same-NAICS (score 2) ranked above same-department-only (score 1).
            ids.Should().Equal(new[] { sameNaicsId, sameDeptId });
        }
    }

    [Fact]
    public async Task Similar_returns_empty_when_notice_has_no_naics_or_department()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"sim2_{Guid.NewGuid():N}@test.dev");

        var loneId = $"LON-{Guid.NewGuid():N}"[..16];
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(Seed(loneId, naics: null, dept: null));           // nothing to anchor on
            db.Notices.Add(Seed($"OTH-{Guid.NewGuid():N}"[..16], "111111", "DEPT OF X")); // unrelated, active
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
            (await notices.GetSimilarAsync(userId, loneId)).Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Similar_surfaces_the_callers_saved_and_matched_flags()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"simflags_{Guid.NewGuid():N}@test.dev");
        var anchorId = $"ANC-{Guid.NewGuid():N}"[..16];
        var savedId = $"SAV-{Guid.NewGuid():N}"[..16];
        var matchedId = $"MAT-{Guid.NewGuid():N}"[..16];

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.AddRange(
                Seed(anchorId, "541512", "DEPT OF DEFENSE"),
                Seed(savedId, "541512", "DEPT OF ENERGY"),
                Seed(matchedId, "541512", "DEPT OF ENERGY"));
            var profile = new MatchProfile
            {
                UserId = userId, Name = "P", Naics = new() { "541512" },
                IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            };
            db.MatchProfiles.Add(profile);
            await db.SaveChangesAsync();
            db.SavedNotices.Add(new SavedNotice { UserId = userId, NoticeId = savedId, SavedAt = DateTime.UtcNow });
            db.NoticeMatches.Add(new NoticeMatch { UserId = userId, NoticeId = matchedId, MatchProfileId = profile.Id, MatchedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
            var similar = await notices.GetSimilarAsync(userId, anchorId);

            var saved = similar.Single(s => s.NoticeId == savedId);
            saved.IsSaved.Should().BeTrue();
            saved.IsMatched.Should().BeFalse();
            similar.Single(s => s.NoticeId == matchedId).IsMatched.Should().BeTrue();
        }
    }

    [Fact]
    public async Task Similar_respects_the_requested_limit()
    {
        var (_, _, userId) = await RegisterAndLoginAsync($"simlimit_{Guid.NewGuid():N}@test.dev");
        var anchorId = $"ANC-{Guid.NewGuid():N}"[..16];

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Notices.Add(Seed(anchorId, "541512", "DEPT OF DEFENSE"));
            for (var i = 0; i < 5; i++)
                db.Notices.Add(Seed($"S{i}-{Guid.NewGuid():N}"[..16], "541512", "DEPT OF ENERGY"));
            await db.SaveChangesAsync();
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var notices = scope.ServiceProvider.GetRequiredService<INoticeService>();
            (await notices.GetSimilarAsync(userId, anchorId, limit: 2)).Should().HaveCount(2);
        }
    }

    private static Notice Seed(string id, string? naics, string? dept, bool active = true) => new()
    {
        NoticeId = id,
        Title = $"Opportunity {id}",
        Type = NoticeType.CombinedSynopsis,
        NaicsCode = naics,
        DepartmentName = dept,
        AgencyPath = dept,
        PostedDate = DateTime.UtcNow.Date,
        IsActive = active,
        FirstSeenAt = DateTime.UtcNow,
        LastSeenAt = DateTime.UtcNow,
    };
}
