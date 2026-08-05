using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class first : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbpAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MethodName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ReturnValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutionDuration = table.Column<int>(type: "int", nullable: false),
                    ClientIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImpersonatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    ImpersonatorTenantId = table.Column<int>(type: "int", nullable: true),
                    CustomData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpBackgroundJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    JobArgs = table.Column<string>(type: "nvarchar(max)", maxLength: 1048576, nullable: false),
                    TryCount = table.Column<short>(type: "smallint", nullable: false),
                    NextTryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastTryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAbandoned = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<byte>(type: "tinyint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpBackgroundJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpDynamicProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpDynamicProperties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpEditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    ExpiringEditionId = table.Column<int>(type: "int", nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AnnualPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrialDayCount = table.Column<int>(type: "int", nullable: true),
                    WaitingDayAfterExpire = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEditions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpEntityChangeSets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BrowserInfo = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ClientIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtensionData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImpersonatorTenantId = table.Column<int>(type: "int", nullable: true),
                    ImpersonatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEntityChangeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpLanguageTexts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    LanguageName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", maxLength: 67108864, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpLanguageTexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationName = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", maxLength: 1048576, nullable: true),
                    DataTypeName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EntityTypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EntityTypeAssemblyQualifiedName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    UserIds = table.Column<string>(type: "nvarchar(max)", maxLength: 131072, nullable: true),
                    ExcludedUserIds = table.Column<string>(type: "nvarchar(max)", maxLength: 131072, nullable: true),
                    TenantIds = table.Column<string>(type: "nvarchar(max)", maxLength: 131072, nullable: true),
                    TargetNotifiers = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpNotificationSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    NotificationName = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    EntityTypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EntityTypeAssemblyQualifiedName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    TargetNotifiers = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpNotificationSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpOrganizationUnitRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpOrganizationUnitRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(95)", maxLength: 95, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpOrganizationUnits_AbpOrganizationUnits_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpTenantNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    NotificationName = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", maxLength: 1048576, nullable: true),
                    DataTypeName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EntityTypeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    EntityTypeAssemblyQualifiedName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpTenantNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserAccounts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    UserLinkId = table.Column<long>(type: "bigint", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserLoginAttempts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TenancyName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    UserNameOrEmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClientIpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ClientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BrowserInfo = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Result = table.Column<byte>(type: "tinyint", nullable: false),
                    FailReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserLoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TenantNotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TargetNotifiers = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfilePictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShouldChangePasswordOnNextLogin = table.Column<bool>(type: "bit", nullable: false),
                    SignInTokenExpireTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignInToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GoogleAuthenticatorKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecoveryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AuthenticationSource = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EmailConfirmationCode = table.Column<string>(type: "nvarchar(328)", maxLength: 328, nullable: true),
                    PasswordResetCode = table.Column<string>(type: "nvarchar(328)", maxLength: 328, nullable: true),
                    LockoutEndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    IsLockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsPhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IsTwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedEmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUsers_AbpUsers_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpUsers_AbpUsers_DeleterUserId",
                        column: x => x.DeleterUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpUsers_AbpUsers_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpWebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpWebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    WebhookUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Webhooks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Headers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpWebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppBinaryObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bytes = table.Column<byte[]>(type: "varbinary(max)", maxLength: 10240, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBinaryObjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppChatMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetTenantId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Side = table.Column<int>(type: "int", nullable: false),
                    ReadState = table.Column<int>(type: "int", nullable: false),
                    ReceiverReadState = table.Column<int>(type: "int", nullable: false),
                    SharedMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppFriendships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    FriendUserId = table.Column<long>(type: "bigint", nullable: false),
                    FriendTenantId = table.Column<int>(type: "int", nullable: true),
                    FriendUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FriendTenancyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FriendProfilePictureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppFriendships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantLegalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantTaxNo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRecentPasswords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRecentPasswords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppSubscriptionPayments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Gateway = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DayCount = table.Column<int>(type: "int", nullable: false),
                    PaymentPeriodType = table.Column<int>(type: "int", nullable: true),
                    ExternalPaymentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuccessUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: true),
                    IsProrationPayment = table.Column<bool>(type: "bit", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSubscriptionPayments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppUserDelegations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserDelegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientSecret = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConsentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JsonWebKeySet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostLogoutRedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedirectUris = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Settings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClientUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descriptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayNames = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Resources = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_AccountGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    AffectGrossProfit = table.Column<bool>(type: "bit", nullable: false),
                    Nature = table.Column<int>(type: "int", nullable: false),
                    GroupUnder = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_AccountGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_AccountGroup_tbl_AccountGroup_GroupUnder",
                        column: x => x.GroupUnder,
                        principalTable: "tbl_AccountGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Branch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PANumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    PhoneNo1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNo2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    Image1 = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    BranchType = table.Column<int>(type: "int", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Branch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Branch_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_FinancialYear",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    IsOldYear = table.Column<bool>(type: "bit", nullable: false),
                    OldFinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_FinancialYear", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Posting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numbering = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Posting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_ProductGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupUnder = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefult = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ProductGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ProductGroup_tbl_ProductGroup_GroupUnder",
                        column: x => x.GroupUnder,
                        principalTable: "tbl_ProductGroup",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Sizes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Sizes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_Unit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FormalName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Unit", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_VoucherType",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TypeOfVoucher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartIndex = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_VoucherType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbpDynamicEntityProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityFullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DynamicPropertyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpDynamicEntityProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpDynamicEntityProperties_AbpDynamicProperties_DynamicPropertyId",
                        column: x => x.DynamicPropertyId,
                        principalTable: "AbpDynamicProperties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpDynamicPropertyValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    DynamicPropertyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpDynamicPropertyValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpDynamicPropertyValues_AbpDynamicProperties_DynamicPropertyId",
                        column: x => x.DynamicPropertyId,
                        principalTable: "AbpDynamicProperties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpFeatures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    EditionId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpFeatures_AbpEditions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "AbpEditions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpEntityChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChangeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeType = table.Column<byte>(type: "tinyint", nullable: false),
                    EntityChangeSetId = table.Column<long>(type: "bigint", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: true),
                    EntityTypeFullName = table.Column<string>(type: "nvarchar(192)", maxLength: 192, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEntityChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpEntityChanges_AbpEntityChangeSets_EntityChangeSetId",
                        column: x => x.EntityChangeSetId,
                        principalTable: "AbpEntityChangeSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsStatic = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpRoles_AbpUsers_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpRoles_AbpUsers_DeleterUserId",
                        column: x => x.DeleterUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpRoles_AbpUsers_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpSettings_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpTenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionEndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsInTrialPeriod = table.Column<bool>(type: "bit", nullable: false),
                    CustomCssId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DarkLogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DarkLogoFileType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DarkLogoMinimalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DarkLogoMinimalFileType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LightLogoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LightLogoFileType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LightLogoMinimalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LightLogoMinimalFileType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SubscriptionPaymentType = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenancyName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EditionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpTenants_AbpEditions_EditionId",
                        column: x => x.EditionId,
                        principalTable: "AbpEditions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpTenants_AbpUsers_CreatorUserId",
                        column: x => x.CreatorUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpTenants_AbpUsers_DeleterUserId",
                        column: x => x.DeleterUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpTenants_AbpUsers_LastModifierUserId",
                        column: x => x.LastModifierUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserClaims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserClaims_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserLogins",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserLogins_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserOrganizationUnits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserOrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserOrganizationUnits_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserRoles_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserTokens",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Value = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExpireDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserTokens_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpWebhookSendAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpWebhookSendAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpWebhookSendAttempts_AbpWebhookEvents_WebhookEventId",
                        column: x => x.WebhookEventId,
                        principalTable: "AbpWebhookEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AppSubscriptionPaymentProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionPaymentId = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSubscriptionPaymentProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppSubscriptionPaymentProducts_AppSubscriptionPayments_SubscriptionPaymentId",
                        column: x => x.SubscriptionPaymentId,
                        principalTable: "AppSubscriptionPayments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictAuthorizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictAuthorizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictAuthorizations_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_AccountLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CrOrDr = table.Column<int>(type: "int", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditPeriod = table.Column<int>(type: "int", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsBillByBill = table.Column<bool>(type: "bit", nullable: false),
                    Pan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    IsCompany = table.Column<bool>(type: "bit", nullable: false),
                    OpeningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_AccountLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_AccountLedger_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AccountLedger_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AccountLedger_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AccountLedger_tbl_AccountGroup_AccountGroupId",
                        column: x => x.AccountGroupId,
                        principalTable: "tbl_AccountGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AccountLedger_tbl_AccountLedger_ParentId",
                        column: x => x.ParentId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpUserBranch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpUserBranch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpUserBranch_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpUserBranch_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_FinancialYearSelect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_FinancialYearSelect", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_FinancialYearSelect_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_FinancialYearSelect_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Documents_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_JournalMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DebitTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_JournalMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_JournalMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_JournalMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_JournalMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_JournalMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_VoucherNumbering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartingIndex = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Postfix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherGenerateType = table.Column<int>(type: "int", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_VoucherNumbering", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_VoucherNumbering_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_VoucherNumbering_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_VoucherPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    ChangedFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Image = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_VoucherPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_VoucherPhotos_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_VoucherPhotos_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpDynamicEntityPropertyValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DynamicEntityPropertyId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpDynamicEntityPropertyValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpDynamicEntityPropertyValues_AbpDynamicEntityProperties_DynamicEntityPropertyId",
                        column: x => x.DynamicEntityPropertyId,
                        principalTable: "AbpDynamicEntityProperties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpEntityPropertyChanges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityChangeId = table.Column<long>(type: "bigint", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OriginalValue = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PropertyName = table.Column<string>(type: "nvarchar(96)", maxLength: 96, nullable: true),
                    PropertyTypeFullName = table.Column<string>(type: "nvarchar(192)", maxLength: 192, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    NewValueHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginalValueHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpEntityPropertyChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpEntityPropertyChanges_AbpEntityChanges_EntityChangeId",
                        column: x => x.EntityChangeId,
                        principalTable: "AbpEntityChanges",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpPermissions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpPermissions_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AbpPermissions_AbpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbpRoleClaims",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbpRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbpRoleClaims_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OpenIddictTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RedemptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenIddictTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "OpenIddictApplications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OpenIddictTokens_OpenIddictAuthorizations_AuthorizationId",
                        column: x => x.AuthorizationId,
                        principalTable: "OpenIddictAuthorizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_AdditionalCost",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CashOrBankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_AdditionalCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_AdditionalCost_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AdditionalCost_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_AdditionalCost_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContraMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContraMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ContraMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ContraMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ContraMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ContraMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ContraMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_LedgerPosting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VendorVoucherNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetailIds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostingNumber = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_LedgerPosting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_LedgerPosting_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_LedgerPosting_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_LedgerPosting_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_NewPartyBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgainstVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgainstVoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    AgainstVoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IsMain = table.Column<bool>(type: "bit", nullable: false),
                    IsFullySettled = table.Column<bool>(type: "bit", nullable: false),
                    IsPartiallySettled = table.Column<bool>(type: "bit", nullable: false),
                    MasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterPartyBalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_NewPartyBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_NewPartyBalance_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_NewPartyBalance_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_NewPartyBalance_tbl_NewPartyBalance_MasterPartyBalanceId",
                        column: x => x.MasterPartyBalanceId,
                        principalTable: "tbl_NewPartyBalance",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_NewPartyBalance_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PartyBalance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AgainstVoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgainstVoucherNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AgainstInvoiceNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditPeriod = table.Column<int>(type: "int", nullable: false),
                    IsAgainst = table.Column<bool>(type: "bit", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterVoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasterVoucherNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PartyBalance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PartyBalance_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PartyBalance_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PartyBalance_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PartyBalance_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PaymentMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PaymentMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PaymentMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PaymentMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PaymentMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PaymentMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PaymentMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PDCPayable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChequeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PDCPayable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PDCPayable_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCPayable_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCPayable_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCPayable_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCPayable_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PDCReceivable",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChequeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PDCReceivable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PDCReceivable_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCReceivable_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCReceivable_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCReceivable_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PDCReceivable_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseOrderMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseOrderMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ReceiptMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RefVoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ReceiptMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditPeriod = table.Column<int>(type: "int", nullable: false),
                    CreditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsPrint = table.Column<bool>(type: "bit", nullable: false),
                    NoOfPrint = table.Column<int>(type: "int", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SyncwithIrd = table.Column<bool>(type: "bit", nullable: false),
                    IrdSyncDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintedTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRealTime = table.Column<bool>(type: "bit", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentMethodLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDelete = table.Column<bool>(type: "bit", nullable: false),
                    VatRefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LrNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgainstId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesModeType = table.Column<int>(type: "int", nullable: false),
                    AgainstVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PINumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    SalesType = table.Column<int>(type: "int", nullable: false),
                    PrintUserId = table.Column<int>(type: "int", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerPhoneNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VatNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceDeliveryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsErrorFixed = table.Column<bool>(type: "bit", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesProductCancelMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesProductCancelMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelMaster_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Tax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Tax", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Tax_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_JournalDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChequeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JournalMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_JournalDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_JournalDetails_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_JournalDetails_tbl_JournalMaster_JournalMasterId",
                        column: x => x.JournalMasterId,
                        principalTable: "tbl_JournalMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContraDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChequeNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContraMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContraDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ContraDetails_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ContraDetails_tbl_ContraMaster_ContraMasterId",
                        column: x => x.ContraMasterId,
                        principalTable: "tbl_ContraMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PaymentDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChequeNo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Forex = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PaymentDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PaymentDetails_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PaymentDetails_tbl_PaymentMaster_PaymentMasterId",
                        column: x => x.PaymentMasterId,
                        principalTable: "tbl_PaymentMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PdcClearance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgainstMode = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChequeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgainstLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PDCPayableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PDCReceivableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PdcClearance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_tbl_PDCPayable_PDCPayableId",
                        column: x => x.PDCPayableId,
                        principalTable: "tbl_PDCPayable",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_tbl_PDCReceivable_PDCReceivableId",
                        column: x => x.PDCReceivableId,
                        principalTable: "tbl_PDCReceivable",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PdcClearance_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VendorInvoiceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditPeriod = table.Column<int>(type: "int", nullable: false),
                    Narration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgainstId = table.Column<int>(type: "int", nullable: false),
                    LrNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseMaster_tbl_PurchaseOrderMaster_PurchaseOrderMasterId",
                        column: x => x.PurchaseOrderMasterId,
                        principalTable: "tbl_PurchaseOrderMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseProductCancelMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseProductCancelMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelMaster_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelMaster_tbl_PurchaseOrderMaster_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "tbl_PurchaseOrderMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ReceiptDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChequeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Forex = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiptMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ReceiptDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptDetails_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ReceiptDetails_tbl_ReceiptMaster_ReceiptMasterId",
                        column: x => x.ReceiptMasterId,
                        principalTable: "tbl_ReceiptMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesReturnMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SalesAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BillDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LrNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransportationCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValueAddedTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebitOrCreditNote = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    PostingNumbering = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    ReturnType = table.Column<int>(type: "int", nullable: false),
                    ReturnTaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesReturnMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnMaster_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Product",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HsCode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Mrp = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalesRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaximumStock = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Margin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsOpeningStock = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Product_tbl_ProductGroup_ProductGroupId",
                        column: x => x.ProductGroupId,
                        principalTable: "tbl_ProductGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_Product_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_Product_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ImportTaxMaster",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalImportTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ImportTaxMaster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxMaster_tbl_Branch_BranchId",
                        column: x => x.BranchId,
                        principalTable: "tbl_Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxMaster_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxMaster_tbl_PurchaseMaster_PurchaseMasterId",
                        column: x => x.PurchaseMasterId,
                        principalTable: "tbl_PurchaseMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseReturn",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchaseAccount = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DebitOrCreditNote = table.Column<bool>(type: "bit", nullable: false),
                    TotalTaxableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LrNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransportationCompany = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceType = table.Column<int>(type: "int", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreateUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdateUserId = table.Column<long>(type: "bigint", nullable: true),
                    PostingNumber = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ReturnType = table.Column<int>(type: "int", nullable: false),
                    ReturnTaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseReturn", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturn_AbpUsers_CreateUserId",
                        column: x => x.CreateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturn_AbpUsers_UpdateUserId",
                        column: x => x.UpdateUserId,
                        principalTable: "AbpUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturn_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturn_tbl_PurchaseMaster_PurchaseMasterId",
                        column: x => x.PurchaseMasterId,
                        principalTable: "tbl_PurchaseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturn_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_Bom",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RawMaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_Bom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_Bom_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_Bom_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseOrderDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PurchaseOrderMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseOrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderDetails_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderDetails_tbl_PurchaseOrderMaster_PurchaseOrderMasterId",
                        column: x => x.PurchaseOrderMasterId,
                        principalTable: "tbl_PurchaseOrderMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseOrderDetails_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgainstDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalesMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesDetails_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesDetails_tbl_SalesMaster_SalesMasterId",
                        column: x => x.SalesMasterId,
                        principalTable: "tbl_SalesMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesDetails_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesDetails_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesProductCancelDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalesProductCancelMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesProductCancelDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelDetail_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesProductCancelDetail_tbl_SalesProductCancelMaster_SalesProductCancelMasterId",
                        column: x => x.SalesProductCancelMasterId,
                        principalTable: "tbl_SalesProductCancelMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_StockFIFOTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_StockFIFOTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_StockFIFOTables_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockFIFOTables_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_StockMaintains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpeningQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InwardQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutwardQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InwardRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutwardRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InwardAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutwardAmt = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_StockMaintains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_StockMaintains_tbl_FinancialYear_FinancialYearId",
                        column: x => x.FinancialYearId,
                        principalTable: "tbl_FinancialYear",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockMaintains_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockMaintains_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_StockPosting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNumbering = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateMiti = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AgainstVoucherTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgainstVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InWardQty = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    OutWardQty = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    IsValueIncrease = table.Column<bool>(type: "bit", nullable: false),
                    FinancialYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorVoucherNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_StockPosting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_StockPosting_tbl_AccountLedger_LedgerId",
                        column: x => x.LedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockPosting_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockPosting_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_StockPosting_tbl_VoucherType_VoucherTypeId",
                        column: x => x.VoucherTypeId,
                        principalTable: "tbl_VoucherType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_UnitConversion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ConversionRate = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrimaryQty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_UnitConversion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_UnitConversion_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_UnitConversion_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgainstDetalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurchaseMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderDetailsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseDetails_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseDetails_tbl_PurchaseMaster_PurchaseMasterId",
                        column: x => x.PurchaseMasterId,
                        principalTable: "tbl_PurchaseMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseDetails_tbl_PurchaseOrderDetails_PurchaseOrderDetailsId",
                        column: x => x.PurchaseOrderDetailsId,
                        principalTable: "tbl_PurchaseOrderDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseDetails_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseDetails_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseProductCancelDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseOrderDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseProductCancelMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseProductCancelDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelDetail_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelDetail_tbl_PurchaseOrderDetails_PurchaseOrderDetailId",
                        column: x => x.PurchaseOrderDetailId,
                        principalTable: "tbl_PurchaseOrderDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseProductCancelDetail_tbl_PurchaseProductCancelMaster_PurchaseProductCancelMasterId",
                        column: x => x.PurchaseProductCancelMasterId,
                        principalTable: "tbl_PurchaseProductCancelMaster",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_SalesReturnDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalesDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SalesReturnMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_SalesReturnDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnDetails_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnDetails_tbl_SalesDetails_SalesDetailId",
                        column: x => x.SalesDetailId,
                        principalTable: "tbl_SalesDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnDetails_tbl_SalesReturnMaster_SalesReturnMasterId",
                        column: x => x.SalesReturnMasterId,
                        principalTable: "tbl_SalesReturnMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnDetails_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_SalesReturnDetails_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ImportTaxDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountLedgerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ImportTaxMasterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ImportTaxDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxDetails_tbl_AccountLedger_AccountLedgerId",
                        column: x => x.AccountLedgerId,
                        principalTable: "tbl_AccountLedger",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxDetails_tbl_ImportTaxMaster_ImportTaxMasterId",
                        column: x => x.ImportTaxMasterId,
                        principalTable: "tbl_ImportTaxMaster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_ImportTaxDetails_tbl_PurchaseDetails_PurchaseDetailId",
                        column: x => x.PurchaseDetailId,
                        principalTable: "tbl_PurchaseDetails",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_PurchaseReturnDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseReturnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseDetailsId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_PurchaseReturnDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturnDetails_tbl_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tbl_Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturnDetails_tbl_PurchaseDetails_PurchaseDetailsId",
                        column: x => x.PurchaseDetailsId,
                        principalTable: "tbl_PurchaseDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturnDetails_tbl_PurchaseReturn_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "tbl_PurchaseReturn",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturnDetails_tbl_Tax_TaxId",
                        column: x => x.TaxId,
                        principalTable: "tbl_Tax",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_PurchaseReturnDetails_tbl_Unit_UnitId",
                        column: x => x.UnitId,
                        principalTable: "tbl_Unit",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogs_TenantId_ExecutionDuration",
                table: "AbpAuditLogs",
                columns: new[] { "TenantId", "ExecutionDuration" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogs_TenantId_ExecutionTime",
                table: "AbpAuditLogs",
                columns: new[] { "TenantId", "ExecutionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpAuditLogs_TenantId_UserId",
                table: "AbpAuditLogs",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpBackgroundJobs_IsAbandoned_NextTryTime",
                table: "AbpBackgroundJobs",
                columns: new[] { "IsAbandoned", "NextTryTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpDynamicEntityProperties_DynamicPropertyId",
                table: "AbpDynamicEntityProperties",
                column: "DynamicPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpDynamicEntityProperties_EntityFullName_DynamicPropertyId_TenantId",
                table: "AbpDynamicEntityProperties",
                columns: new[] { "EntityFullName", "DynamicPropertyId", "TenantId" },
                unique: true,
                filter: "[EntityFullName] IS NOT NULL AND [TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AbpDynamicEntityPropertyValues_DynamicEntityPropertyId",
                table: "AbpDynamicEntityPropertyValues",
                column: "DynamicEntityPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpDynamicProperties_PropertyName_TenantId",
                table: "AbpDynamicProperties",
                columns: new[] { "PropertyName", "TenantId" },
                unique: true,
                filter: "[PropertyName] IS NOT NULL AND [TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AbpDynamicPropertyValues_DynamicPropertyId",
                table: "AbpDynamicPropertyValues",
                column: "DynamicPropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChanges_EntityChangeSetId",
                table: "AbpEntityChanges",
                column: "EntityChangeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChanges_EntityTypeFullName_EntityId",
                table: "AbpEntityChanges",
                columns: new[] { "EntityTypeFullName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChangeSets_TenantId_CreationTime",
                table: "AbpEntityChangeSets",
                columns: new[] { "TenantId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChangeSets_TenantId_Reason",
                table: "AbpEntityChangeSets",
                columns: new[] { "TenantId", "Reason" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityChangeSets_TenantId_UserId",
                table: "AbpEntityChangeSets",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpEntityPropertyChanges_EntityChangeId",
                table: "AbpEntityPropertyChanges",
                column: "EntityChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatures_EditionId_Name",
                table: "AbpFeatures",
                columns: new[] { "EditionId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpFeatures_TenantId_Name",
                table: "AbpFeatures",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpLanguages_TenantId_Name",
                table: "AbpLanguages",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpLanguageTexts_TenantId_Source_LanguageName_Key",
                table: "AbpLanguageTexts",
                columns: new[] { "TenantId", "Source", "LanguageName", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpNotificationSubscriptions_NotificationName_EntityTypeName_EntityId_UserId",
                table: "AbpNotificationSubscriptions",
                columns: new[] { "NotificationName", "EntityTypeName", "EntityId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpNotificationSubscriptions_TenantId_NotificationName_EntityTypeName_EntityId_UserId",
                table: "AbpNotificationSubscriptions",
                columns: new[] { "TenantId", "NotificationName", "EntityTypeName", "EntityId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnitRoles_TenantId_OrganizationUnitId",
                table: "AbpOrganizationUnitRoles",
                columns: new[] { "TenantId", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnitRoles_TenantId_RoleId",
                table: "AbpOrganizationUnitRoles",
                columns: new[] { "TenantId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnits_ParentId",
                table: "AbpOrganizationUnits",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpOrganizationUnits_TenantId_Code",
                table: "AbpOrganizationUnits",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissions_RoleId",
                table: "AbpPermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissions_TenantId_Name",
                table: "AbpPermissions",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpPermissions_UserId",
                table: "AbpPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoleClaims_RoleId",
                table: "AbpRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoleClaims_TenantId_ClaimType",
                table: "AbpRoleClaims",
                columns: new[] { "TenantId", "ClaimType" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoles_CreatorUserId",
                table: "AbpRoles",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoles_DeleterUserId",
                table: "AbpRoles",
                column: "DeleterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoles_LastModifierUserId",
                table: "AbpRoles",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpRoles_TenantId_NormalizedName",
                table: "AbpRoles",
                columns: new[] { "TenantId", "NormalizedName" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpSettings_TenantId_Name_UserId",
                table: "AbpSettings",
                columns: new[] { "TenantId", "Name", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpSettings_UserId",
                table: "AbpSettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenantNotifications_TenantId",
                table: "AbpTenantNotifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_CreationTime",
                table: "AbpTenants",
                column: "CreationTime");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_CreatorUserId",
                table: "AbpTenants",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_DeleterUserId",
                table: "AbpTenants",
                column: "DeleterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_EditionId",
                table: "AbpTenants",
                column: "EditionId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_LastModifierUserId",
                table: "AbpTenants",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_SubscriptionEndDateUtc",
                table: "AbpTenants",
                column: "SubscriptionEndDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AbpTenants_TenancyName",
                table: "AbpTenants",
                column: "TenancyName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserAccounts_EmailAddress",
                table: "AbpUserAccounts",
                column: "EmailAddress");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserAccounts_TenantId_EmailAddress",
                table: "AbpUserAccounts",
                columns: new[] { "TenantId", "EmailAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserAccounts_TenantId_UserId",
                table: "AbpUserAccounts",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserAccounts_TenantId_UserName",
                table: "AbpUserAccounts",
                columns: new[] { "TenantId", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserAccounts_UserName",
                table: "AbpUserAccounts",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserBranch_BranchId",
                table: "AbpUserBranch",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserBranch_UserId",
                table: "AbpUserBranch",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserClaims_TenantId_ClaimType",
                table: "AbpUserClaims",
                columns: new[] { "TenantId", "ClaimType" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserClaims_UserId",
                table: "AbpUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLoginAttempts_TenancyName_UserNameOrEmailAddress_Result",
                table: "AbpUserLoginAttempts",
                columns: new[] { "TenancyName", "UserNameOrEmailAddress", "Result" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLoginAttempts_UserId_TenantId",
                table: "AbpUserLoginAttempts",
                columns: new[] { "UserId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_ProviderKey_TenantId",
                table: "AbpUserLogins",
                columns: new[] { "ProviderKey", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_TenantId_LoginProvider_ProviderKey",
                table: "AbpUserLogins",
                columns: new[] { "TenantId", "LoginProvider", "ProviderKey" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_TenantId_UserId",
                table: "AbpUserLogins",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserLogins_UserId",
                table: "AbpUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserNotifications_UserId_State_CreationTime",
                table: "AbpUserNotifications",
                columns: new[] { "UserId", "State", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserOrganizationUnits_TenantId_OrganizationUnitId",
                table: "AbpUserOrganizationUnits",
                columns: new[] { "TenantId", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserOrganizationUnits_TenantId_UserId",
                table: "AbpUserOrganizationUnits",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserOrganizationUnits_UserId",
                table: "AbpUserOrganizationUnits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserRoles_TenantId_RoleId",
                table: "AbpUserRoles",
                columns: new[] { "TenantId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserRoles_TenantId_UserId",
                table: "AbpUserRoles",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserRoles_UserId",
                table: "AbpUserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_CreatorUserId",
                table: "AbpUsers",
                column: "CreatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_DeleterUserId",
                table: "AbpUsers",
                column: "DeleterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_LastModifierUserId",
                table: "AbpUsers",
                column: "LastModifierUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_NormalizedEmailAddress",
                table: "AbpUsers",
                columns: new[] { "TenantId", "NormalizedEmailAddress" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_TenantId_NormalizedUserName",
                table: "AbpUsers",
                columns: new[] { "TenantId", "NormalizedUserName" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserTokens_TenantId_UserId",
                table: "AbpUserTokens",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AbpUserTokens_UserId",
                table: "AbpUserTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AbpWebhookSendAttempts_WebhookEventId",
                table: "AbpWebhookSendAttempts",
                column: "WebhookEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBinaryObjects_TenantId",
                table: "AppBinaryObjects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_TargetTenantId_TargetUserId_ReadState",
                table: "AppChatMessages",
                columns: new[] { "TargetTenantId", "TargetUserId", "ReadState" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_TargetTenantId_UserId_ReadState",
                table: "AppChatMessages",
                columns: new[] { "TargetTenantId", "UserId", "ReadState" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_TenantId_TargetUserId_ReadState",
                table: "AppChatMessages",
                columns: new[] { "TenantId", "TargetUserId", "ReadState" });

            migrationBuilder.CreateIndex(
                name: "IX_AppChatMessages_TenantId_UserId_ReadState",
                table: "AppChatMessages",
                columns: new[] { "TenantId", "UserId", "ReadState" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFriendships_FriendTenantId_FriendUserId",
                table: "AppFriendships",
                columns: new[] { "FriendTenantId", "FriendUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFriendships_FriendTenantId_UserId",
                table: "AppFriendships",
                columns: new[] { "FriendTenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFriendships_TenantId_FriendUserId",
                table: "AppFriendships",
                columns: new[] { "TenantId", "FriendUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppFriendships_TenantId_UserId",
                table: "AppFriendships",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptionPaymentProducts_SubscriptionPaymentId",
                table: "AppSubscriptionPaymentProducts",
                column: "SubscriptionPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptionPayments_ExternalPaymentId_Gateway",
                table: "AppSubscriptionPayments",
                columns: new[] { "ExternalPaymentId", "Gateway" });

            migrationBuilder.CreateIndex(
                name: "IX_AppSubscriptionPayments_Status_CreationTime",
                table: "AppSubscriptionPayments",
                columns: new[] { "Status", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserDelegations_TenantId_SourceUserId",
                table: "AppUserDelegations",
                columns: new[] { "TenantId", "SourceUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserDelegations_TenantId_TargetUserId",
                table: "AppUserDelegations",
                columns: new[] { "TenantId", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictApplications_ClientId",
                table: "OpenIddictApplications",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictAuthorizations_ApplicationId_Status_Subject_Type",
                table: "OpenIddictAuthorizations",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictScopes_Name",
                table: "OpenIddictScopes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ApplicationId_Status_Subject_Type",
                table: "OpenIddictTokens",
                columns: new[] { "ApplicationId", "Status", "Subject", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_AuthorizationId",
                table: "OpenIddictTokens",
                column: "AuthorizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenIddictTokens_ReferenceId",
                table: "OpenIddictTokens",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountGroup_GroupUnder",
                table: "tbl_AccountGroup",
                column: "GroupUnder");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountLedger_AccountGroupId",
                table: "tbl_AccountLedger",
                column: "AccountGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountLedger_CreateUserId",
                table: "tbl_AccountLedger",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountLedger_ParentId",
                table: "tbl_AccountLedger",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountLedger_UpdateUserId",
                table: "tbl_AccountLedger",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AccountLedger_UserId",
                table: "tbl_AccountLedger",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AdditionalCost_BranchId",
                table: "tbl_AdditionalCost",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AdditionalCost_LedgerId",
                table: "tbl_AdditionalCost",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_AdditionalCost_VoucherTypeId",
                table: "tbl_AdditionalCost",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Bom_ProductId",
                table: "tbl_Bom",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Bom_UnitId",
                table: "tbl_Bom",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Branch_BranchId",
                table: "tbl_Branch",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraDetails_ContraMasterId",
                table: "tbl_ContraDetails",
                column: "ContraMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraDetails_LedgerId",
                table: "tbl_ContraDetails",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraMaster_CreateUserId",
                table: "tbl_ContraMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraMaster_FinancialYearId",
                table: "tbl_ContraMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraMaster_LedgerId",
                table: "tbl_ContraMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraMaster_UpdateUserId",
                table: "tbl_ContraMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContraMaster_VoucherTypeId",
                table: "tbl_ContraMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Documents_VoucherTypeId",
                table: "tbl_Documents",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_FinancialYearSelect_FinancialYearId",
                table: "tbl_FinancialYearSelect",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_FinancialYearSelect_UserId",
                table: "tbl_FinancialYearSelect",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxDetails_AccountLedgerId",
                table: "tbl_ImportTaxDetails",
                column: "AccountLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxDetails_ImportTaxMasterId",
                table: "tbl_ImportTaxDetails",
                column: "ImportTaxMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxDetails_PurchaseDetailId",
                table: "tbl_ImportTaxDetails",
                column: "PurchaseDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxMaster_BranchId",
                table: "tbl_ImportTaxMaster",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxMaster_FinancialYearId",
                table: "tbl_ImportTaxMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ImportTaxMaster_PurchaseMasterId",
                table: "tbl_ImportTaxMaster",
                column: "PurchaseMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalDetails_JournalMasterId",
                table: "tbl_JournalDetails",
                column: "JournalMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalDetails_LedgerId",
                table: "tbl_JournalDetails",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalMaster_CreateUserId",
                table: "tbl_JournalMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalMaster_FinancialYearId",
                table: "tbl_JournalMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalMaster_UpdateUserId",
                table: "tbl_JournalMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_JournalMaster_VoucherTypeId",
                table: "tbl_JournalMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_LedgerPosting_FinancialYearId",
                table: "tbl_LedgerPosting",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_LedgerPosting_LedgerId",
                table: "tbl_LedgerPosting",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_LedgerPosting_VoucherTypeId",
                table: "tbl_LedgerPosting",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_NewPartyBalance_FinancialYearId",
                table: "tbl_NewPartyBalance",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_NewPartyBalance_LedgerId",
                table: "tbl_NewPartyBalance",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_NewPartyBalance_MasterPartyBalanceId",
                table: "tbl_NewPartyBalance",
                column: "MasterPartyBalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_NewPartyBalance_VoucherTypeId",
                table: "tbl_NewPartyBalance",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PartyBalance_BranchId",
                table: "tbl_PartyBalance",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PartyBalance_FinancialYearId",
                table: "tbl_PartyBalance",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PartyBalance_LedgerId",
                table: "tbl_PartyBalance",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PartyBalance_VoucherTypeId",
                table: "tbl_PartyBalance",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentDetails_LedgerId",
                table: "tbl_PaymentDetails",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentDetails_PaymentMasterId",
                table: "tbl_PaymentDetails",
                column: "PaymentMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentMaster_CreateUserId",
                table: "tbl_PaymentMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentMaster_FinancialYearId",
                table: "tbl_PaymentMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentMaster_LedgerId",
                table: "tbl_PaymentMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentMaster_UpdateUserId",
                table: "tbl_PaymentMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PaymentMaster_VoucherTypeId",
                table: "tbl_PaymentMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_CreateUserId",
                table: "tbl_PdcClearance",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_FinancialYearId",
                table: "tbl_PdcClearance",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_LedgerId",
                table: "tbl_PdcClearance",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_PDCPayableId",
                table: "tbl_PdcClearance",
                column: "PDCPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_PDCReceivableId",
                table: "tbl_PdcClearance",
                column: "PDCReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_UpdateUserId",
                table: "tbl_PdcClearance",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PdcClearance_VoucherTypeId",
                table: "tbl_PdcClearance",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCPayable_CreateUserId",
                table: "tbl_PDCPayable",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCPayable_FinancialYearId",
                table: "tbl_PDCPayable",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCPayable_LedgerId",
                table: "tbl_PDCPayable",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCPayable_UpdateUserId",
                table: "tbl_PDCPayable",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCPayable_VoucherTypeId",
                table: "tbl_PDCPayable",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCReceivable_CreateUserId",
                table: "tbl_PDCReceivable",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCReceivable_FinancialYearId",
                table: "tbl_PDCReceivable",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCReceivable_LedgerId",
                table: "tbl_PDCReceivable",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCReceivable_UpdateUserId",
                table: "tbl_PDCReceivable",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PDCReceivable_VoucherTypeId",
                table: "tbl_PDCReceivable",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Product_ProductGroupId",
                table: "tbl_Product",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Product_TaxId",
                table: "tbl_Product",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Product_UnitId",
                table: "tbl_Product",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ProductGroup_GroupUnder",
                table: "tbl_ProductGroup",
                column: "GroupUnder");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseDetails_ProductId",
                table: "tbl_PurchaseDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseDetails_PurchaseMasterId",
                table: "tbl_PurchaseDetails",
                column: "PurchaseMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseDetails_PurchaseOrderDetailsId",
                table: "tbl_PurchaseDetails",
                column: "PurchaseOrderDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseDetails_TaxId",
                table: "tbl_PurchaseDetails",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseDetails_UnitId",
                table: "tbl_PurchaseDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseMaster_CreateUserId",
                table: "tbl_PurchaseMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseMaster_LedgerId",
                table: "tbl_PurchaseMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseMaster_PurchaseOrderMasterId",
                table: "tbl_PurchaseMaster",
                column: "PurchaseOrderMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseMaster_UpdateUserId",
                table: "tbl_PurchaseMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseMaster_VoucherTypeId",
                table: "tbl_PurchaseMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderDetails_ProductId",
                table: "tbl_PurchaseOrderDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderDetails_PurchaseOrderMasterId",
                table: "tbl_PurchaseOrderDetails",
                column: "PurchaseOrderMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderDetails_UnitId",
                table: "tbl_PurchaseOrderDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderMaster_CreateUserId",
                table: "tbl_PurchaseOrderMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderMaster_LedgerId",
                table: "tbl_PurchaseOrderMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderMaster_UpdateUserId",
                table: "tbl_PurchaseOrderMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseOrderMaster_VoucherTypeId",
                table: "tbl_PurchaseOrderMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelDetail_ProductId",
                table: "tbl_PurchaseProductCancelDetail",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelDetail_PurchaseOrderDetailId",
                table: "tbl_PurchaseProductCancelDetail",
                column: "PurchaseOrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelDetail_PurchaseProductCancelMasterId",
                table: "tbl_PurchaseProductCancelDetail",
                column: "PurchaseProductCancelMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelMaster_BranchId",
                table: "tbl_PurchaseProductCancelMaster",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelMaster_FinancialYearId",
                table: "tbl_PurchaseProductCancelMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelMaster_LedgerId",
                table: "tbl_PurchaseProductCancelMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelMaster_PurchaseOrderId",
                table: "tbl_PurchaseProductCancelMaster",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseProductCancelMaster_VoucherTypeId",
                table: "tbl_PurchaseProductCancelMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturn_CreateUserId",
                table: "tbl_PurchaseReturn",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturn_LedgerId",
                table: "tbl_PurchaseReturn",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturn_PurchaseMasterId",
                table: "tbl_PurchaseReturn",
                column: "PurchaseMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturn_UpdateUserId",
                table: "tbl_PurchaseReturn",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturn_VoucherTypeId",
                table: "tbl_PurchaseReturn",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturnDetails_ProductId",
                table: "tbl_PurchaseReturnDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturnDetails_PurchaseDetailsId",
                table: "tbl_PurchaseReturnDetails",
                column: "PurchaseDetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturnDetails_PurchaseReturnId",
                table: "tbl_PurchaseReturnDetails",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturnDetails_TaxId",
                table: "tbl_PurchaseReturnDetails",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_PurchaseReturnDetails_UnitId",
                table: "tbl_PurchaseReturnDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptDetails_LedgerId",
                table: "tbl_ReceiptDetails",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptDetails_ReceiptMasterId",
                table: "tbl_ReceiptDetails",
                column: "ReceiptMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptMaster_CreateUserId",
                table: "tbl_ReceiptMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptMaster_FinancialYearId",
                table: "tbl_ReceiptMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptMaster_LedgerId",
                table: "tbl_ReceiptMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptMaster_UpdateUserId",
                table: "tbl_ReceiptMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ReceiptMaster_VoucherTypeId",
                table: "tbl_ReceiptMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesDetails_ProductId",
                table: "tbl_SalesDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesDetails_SalesMasterId",
                table: "tbl_SalesDetails",
                column: "SalesMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesDetails_TaxId",
                table: "tbl_SalesDetails",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesDetails_UnitId",
                table: "tbl_SalesDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesMaster_CreateUserId",
                table: "tbl_SalesMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesMaster_FinancialYearId",
                table: "tbl_SalesMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesMaster_LedgerId",
                table: "tbl_SalesMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesMaster_UpdateUserId",
                table: "tbl_SalesMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesMaster_VoucherTypeId",
                table: "tbl_SalesMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelDetail_ProductId",
                table: "tbl_SalesProductCancelDetail",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelDetail_SalesProductCancelMasterId",
                table: "tbl_SalesProductCancelDetail",
                column: "SalesProductCancelMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelMaster_BranchId",
                table: "tbl_SalesProductCancelMaster",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelMaster_FinancialYearId",
                table: "tbl_SalesProductCancelMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelMaster_LedgerId",
                table: "tbl_SalesProductCancelMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesProductCancelMaster_VoucherTypeId",
                table: "tbl_SalesProductCancelMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnDetails_ProductId",
                table: "tbl_SalesReturnDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnDetails_SalesDetailId",
                table: "tbl_SalesReturnDetails",
                column: "SalesDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnDetails_SalesReturnMasterId",
                table: "tbl_SalesReturnDetails",
                column: "SalesReturnMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnDetails_TaxId",
                table: "tbl_SalesReturnDetails",
                column: "TaxId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnDetails_UnitId",
                table: "tbl_SalesReturnDetails",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_CreateUserId",
                table: "tbl_SalesReturnMaster",
                column: "CreateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_FinancialYearId",
                table: "tbl_SalesReturnMaster",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_LedgerId",
                table: "tbl_SalesReturnMaster",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_SalesMasterId",
                table: "tbl_SalesReturnMaster",
                column: "SalesMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_UpdateUserId",
                table: "tbl_SalesReturnMaster",
                column: "UpdateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_SalesReturnMaster_VoucherTypeId",
                table: "tbl_SalesReturnMaster",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockFIFOTables_ProductId",
                table: "tbl_StockFIFOTables",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockFIFOTables_UnitId",
                table: "tbl_StockFIFOTables",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockMaintains_FinancialYearId",
                table: "tbl_StockMaintains",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockMaintains_ProductId",
                table: "tbl_StockMaintains",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockMaintains_UnitId",
                table: "tbl_StockMaintains",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockPosting_LedgerId",
                table: "tbl_StockPosting",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockPosting_ProductId",
                table: "tbl_StockPosting",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockPosting_UnitId",
                table: "tbl_StockPosting",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_StockPosting_VoucherTypeId",
                table: "tbl_StockPosting",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_Tax_LedgerId",
                table: "tbl_Tax",
                column: "LedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_UnitConversion_ProductId",
                table: "tbl_UnitConversion",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_UnitConversion_UnitId",
                table: "tbl_UnitConversion",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_VoucherNumbering_FinancialYearId",
                table: "tbl_VoucherNumbering",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_VoucherNumbering_VoucherTypeId",
                table: "tbl_VoucherNumbering",
                column: "VoucherTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_VoucherPhotos_FinancialYearId",
                table: "tbl_VoucherPhotos",
                column: "FinancialYearId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_VoucherPhotos_VoucherTypeId",
                table: "tbl_VoucherPhotos",
                column: "VoucherTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbpAuditLogs");

            migrationBuilder.DropTable(
                name: "AbpBackgroundJobs");

            migrationBuilder.DropTable(
                name: "AbpDynamicEntityPropertyValues");

            migrationBuilder.DropTable(
                name: "AbpDynamicPropertyValues");

            migrationBuilder.DropTable(
                name: "AbpEntityPropertyChanges");

            migrationBuilder.DropTable(
                name: "AbpFeatures");

            migrationBuilder.DropTable(
                name: "AbpLanguages");

            migrationBuilder.DropTable(
                name: "AbpLanguageTexts");

            migrationBuilder.DropTable(
                name: "AbpNotifications");

            migrationBuilder.DropTable(
                name: "AbpNotificationSubscriptions");

            migrationBuilder.DropTable(
                name: "AbpOrganizationUnitRoles");

            migrationBuilder.DropTable(
                name: "AbpOrganizationUnits");

            migrationBuilder.DropTable(
                name: "AbpPermissions");

            migrationBuilder.DropTable(
                name: "AbpRoleClaims");

            migrationBuilder.DropTable(
                name: "AbpSettings");

            migrationBuilder.DropTable(
                name: "AbpTenantNotifications");

            migrationBuilder.DropTable(
                name: "AbpTenants");

            migrationBuilder.DropTable(
                name: "AbpUserAccounts");

            migrationBuilder.DropTable(
                name: "AbpUserBranch");

            migrationBuilder.DropTable(
                name: "AbpUserClaims");

            migrationBuilder.DropTable(
                name: "AbpUserLoginAttempts");

            migrationBuilder.DropTable(
                name: "AbpUserLogins");

            migrationBuilder.DropTable(
                name: "AbpUserNotifications");

            migrationBuilder.DropTable(
                name: "AbpUserOrganizationUnits");

            migrationBuilder.DropTable(
                name: "AbpUserRoles");

            migrationBuilder.DropTable(
                name: "AbpUserTokens");

            migrationBuilder.DropTable(
                name: "AbpWebhookSendAttempts");

            migrationBuilder.DropTable(
                name: "AbpWebhookSubscriptions");

            migrationBuilder.DropTable(
                name: "AppBinaryObjects");

            migrationBuilder.DropTable(
                name: "AppChatMessages");

            migrationBuilder.DropTable(
                name: "AppFriendships");

            migrationBuilder.DropTable(
                name: "AppInvoices");

            migrationBuilder.DropTable(
                name: "AppRecentPasswords");

            migrationBuilder.DropTable(
                name: "AppSubscriptionPaymentProducts");

            migrationBuilder.DropTable(
                name: "AppUserDelegations");

            migrationBuilder.DropTable(
                name: "OpenIddictScopes");

            migrationBuilder.DropTable(
                name: "OpenIddictTokens");

            migrationBuilder.DropTable(
                name: "tbl_AdditionalCost");

            migrationBuilder.DropTable(
                name: "tbl_Bom");

            migrationBuilder.DropTable(
                name: "tbl_ContraDetails");

            migrationBuilder.DropTable(
                name: "tbl_Documents");

            migrationBuilder.DropTable(
                name: "tbl_FinancialYearSelect");

            migrationBuilder.DropTable(
                name: "tbl_ImportTaxDetails");

            migrationBuilder.DropTable(
                name: "tbl_JournalDetails");

            migrationBuilder.DropTable(
                name: "tbl_LedgerPosting");

            migrationBuilder.DropTable(
                name: "tbl_NewPartyBalance");

            migrationBuilder.DropTable(
                name: "tbl_PartyBalance");

            migrationBuilder.DropTable(
                name: "tbl_PaymentDetails");

            migrationBuilder.DropTable(
                name: "tbl_PdcClearance");

            migrationBuilder.DropTable(
                name: "tbl_Posting");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseProductCancelDetail");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseReturnDetails");

            migrationBuilder.DropTable(
                name: "tbl_ReceiptDetails");

            migrationBuilder.DropTable(
                name: "tbl_SalesProductCancelDetail");

            migrationBuilder.DropTable(
                name: "tbl_SalesReturnDetails");

            migrationBuilder.DropTable(
                name: "tbl_Sizes");

            migrationBuilder.DropTable(
                name: "tbl_StockFIFOTables");

            migrationBuilder.DropTable(
                name: "tbl_StockMaintains");

            migrationBuilder.DropTable(
                name: "tbl_StockPosting");

            migrationBuilder.DropTable(
                name: "tbl_UnitConversion");

            migrationBuilder.DropTable(
                name: "tbl_VoucherNumbering");

            migrationBuilder.DropTable(
                name: "tbl_VoucherPhotos");

            migrationBuilder.DropTable(
                name: "AbpDynamicEntityProperties");

            migrationBuilder.DropTable(
                name: "AbpEntityChanges");

            migrationBuilder.DropTable(
                name: "AbpRoles");

            migrationBuilder.DropTable(
                name: "AbpEditions");

            migrationBuilder.DropTable(
                name: "AbpWebhookEvents");

            migrationBuilder.DropTable(
                name: "AppSubscriptionPayments");

            migrationBuilder.DropTable(
                name: "OpenIddictAuthorizations");

            migrationBuilder.DropTable(
                name: "tbl_ContraMaster");

            migrationBuilder.DropTable(
                name: "tbl_ImportTaxMaster");

            migrationBuilder.DropTable(
                name: "tbl_JournalMaster");

            migrationBuilder.DropTable(
                name: "tbl_PaymentMaster");

            migrationBuilder.DropTable(
                name: "tbl_PDCPayable");

            migrationBuilder.DropTable(
                name: "tbl_PDCReceivable");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseProductCancelMaster");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseDetails");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseReturn");

            migrationBuilder.DropTable(
                name: "tbl_ReceiptMaster");

            migrationBuilder.DropTable(
                name: "tbl_SalesProductCancelMaster");

            migrationBuilder.DropTable(
                name: "tbl_SalesDetails");

            migrationBuilder.DropTable(
                name: "tbl_SalesReturnMaster");

            migrationBuilder.DropTable(
                name: "AbpDynamicProperties");

            migrationBuilder.DropTable(
                name: "AbpEntityChangeSets");

            migrationBuilder.DropTable(
                name: "OpenIddictApplications");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseOrderDetails");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseMaster");

            migrationBuilder.DropTable(
                name: "tbl_Branch");

            migrationBuilder.DropTable(
                name: "tbl_SalesMaster");

            migrationBuilder.DropTable(
                name: "tbl_Product");

            migrationBuilder.DropTable(
                name: "tbl_PurchaseOrderMaster");

            migrationBuilder.DropTable(
                name: "tbl_FinancialYear");

            migrationBuilder.DropTable(
                name: "tbl_ProductGroup");

            migrationBuilder.DropTable(
                name: "tbl_Tax");

            migrationBuilder.DropTable(
                name: "tbl_Unit");

            migrationBuilder.DropTable(
                name: "tbl_VoucherType");

            migrationBuilder.DropTable(
                name: "tbl_AccountLedger");

            migrationBuilder.DropTable(
                name: "AbpUsers");

            migrationBuilder.DropTable(
                name: "tbl_AccountGroup");
        }
    }
}
