using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OppSignal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agencies",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    ParentCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agencies", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    TimeZoneId = table.Column<string>(type: "text", nullable: false),
                    IsAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "awards",
                columns: table => new
                {
                    AwardId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientName = table.Column<string>(type: "text", nullable: true),
                    RecipientUei = table.Column<string>(type: "text", nullable: true),
                    AwardingAgency = table.Column<string>(type: "text", nullable: true),
                    NaicsCode = table.Column<string>(type: "text", nullable: true),
                    PscCode = table.Column<string>(type: "text", nullable: true),
                    ObligatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PotentialTotalValue = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PeriodOfPerformanceStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodOfPerformanceEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PopState = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    IngestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_awards", x => x.AwardId);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToAddress = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ingest_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WindowFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PagesFetched = table.Column<int>(type: "integer", nullable: false),
                    NoticesSeen = table.Column<int>(type: "integer", nullable: false),
                    NoticesInserted = table.Column<int>(type: "integer", nullable: false),
                    NoticesUpdated = table.Column<int>(type: "integer", nullable: false),
                    MatchesCreated = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingest_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "match_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Naics = table.Column<List<string>>(type: "text[]", nullable: false),
                    Psc = table.Column<List<string>>(type: "text[]", nullable: false),
                    Keywords = table.Column<List<string>>(type: "text[]", nullable: false),
                    AgencyPaths = table.Column<List<string>>(type: "text[]", nullable: false),
                    SetAsides = table.Column<int[]>(type: "integer[]", nullable: false),
                    States = table.Column<List<string>>(type: "text[]", nullable: false),
                    NoticeTypes = table.Column<int[]>(type: "integer[]", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPriority = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "naics_codes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    ParentCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_naics_codes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "notices",
                columns: table => new
                {
                    NoticeId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SolicitationNumber = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    BaseType = table.Column<string>(type: "text", nullable: true),
                    AgencyPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DepartmentName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SubTierName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    OfficeName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NaicsCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PscCode = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    SetAside = table.Column<int>(type: "integer", nullable: false),
                    SetAsideDescription = table.Column<string>(type: "text", nullable: true),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PopState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    PopCity = table.Column<string>(type: "text", nullable: true),
                    PopZip = table.Column<string>(type: "text", nullable: true),
                    PopCountry = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    UiLink = table.Column<string>(type: "text", nullable: true),
                    DescriptionLink = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PrimaryContactName = table.Column<string>(type: "text", nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "text", nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notices", x => x.NoticeId);
                });

            migrationBuilder.CreateTable(
                name: "psc_codes",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsService = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_psc_codes", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "set_asides",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_set_asides", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    StripePriceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notice_matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NoticeId = table.Column<string>(type: "character varying(64)", nullable: false),
                    MatchProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MatchReason = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notice_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notice_matches_match_profiles_MatchProfileId",
                        column: x => x.MatchProfileId,
                        principalTable: "match_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_notice_matches_notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "notices",
                        principalColumn: "NoticeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_notices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoticeId = table.Column<string>(type: "character varying(64)", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_notices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_notices_notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "notices",
                        principalColumn: "NoticeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agencies_Tier",
                table: "agencies",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_awards_NaicsCode",
                table: "awards",
                column: "NaicsCode");

            migrationBuilder.CreateIndex(
                name: "IX_awards_PeriodOfPerformanceEnd",
                table: "awards",
                column: "PeriodOfPerformanceEnd");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_SentAt",
                table: "email_logs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_UserId_Kind",
                table: "email_logs",
                columns: new[] { "UserId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ingest_runs_StartedAt",
                table: "ingest_runs",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_match_profiles_UserId",
                table: "match_profiles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_match_profiles_UserId_IsActive",
                table: "match_profiles",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_naics_codes_Level",
                table: "naics_codes",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_notice_matches_MatchProfileId",
                table: "notice_matches",
                column: "MatchProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_notice_matches_NoticeId_MatchProfileId",
                table: "notice_matches",
                columns: new[] { "NoticeId", "MatchProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notice_matches_UserId_NotifiedAt",
                table: "notice_matches",
                columns: new[] { "UserId", "NotifiedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notices_NaicsCode",
                table: "notices",
                column: "NaicsCode");

            migrationBuilder.CreateIndex(
                name: "IX_notices_PopState",
                table: "notices",
                column: "PopState");

            migrationBuilder.CreateIndex(
                name: "IX_notices_PostedDate",
                table: "notices",
                column: "PostedDate");

            migrationBuilder.CreateIndex(
                name: "IX_notices_PscCode",
                table: "notices",
                column: "PscCode");

            migrationBuilder.CreateIndex(
                name: "IX_notices_ResponseDeadline",
                table: "notices",
                column: "ResponseDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_notices_SetAside",
                table: "notices",
                column: "SetAside");

            migrationBuilder.CreateIndex(
                name: "IX_notices_Type",
                table: "notices",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_notices_NoticeId",
                table: "saved_notices",
                column: "NoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_notices_UserId",
                table: "saved_notices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_notices_UserId_NoticeId",
                table: "saved_notices",
                columns: new[] { "UserId", "NoticeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeCustomerId",
                table: "subscriptions",
                column: "StripeCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                table: "subscriptions",
                column: "StripeSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                table: "subscriptions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agencies");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "awards");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "ingest_runs");

            migrationBuilder.DropTable(
                name: "naics_codes");

            migrationBuilder.DropTable(
                name: "notice_matches");

            migrationBuilder.DropTable(
                name: "psc_codes");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "saved_notices");

            migrationBuilder.DropTable(
                name: "set_asides");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "match_profiles");

            migrationBuilder.DropTable(
                name: "notices");
        }
    }
}
