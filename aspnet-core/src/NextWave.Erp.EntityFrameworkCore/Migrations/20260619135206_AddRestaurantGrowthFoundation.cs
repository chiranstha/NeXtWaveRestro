using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextWave.Erp.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantGrowthFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_RestaurantChangeLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Seq = table.Column<long>(type: "bigint", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByDeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantChangeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantChangeLog_tbl_RestaurantDevice_ChangedByDeviceId",
                        column: x => x.ChangedByDeviceId,
                        principalTable: "tbl_RestaurantDevice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantChannel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChannelType = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DefaultPriceMarkupPercent = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantChannel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantPushToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Token = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantPushToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantPushToken_tbl_RestaurantDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "tbl_RestaurantDevice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantSyncUploadBatch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchGuid = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantSyncUploadBatch", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantSyncUploadBatch_tbl_RestaurantDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "tbl_RestaurantDevice",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantAggregatorOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CustomerPhoneNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    RestaurantDiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DeliveryFeeAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantAggregatorOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantAggregatorOrder_tbl_RestaurantChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "tbl_RestaurantChannel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantAggregatorOrder_tbl_RestaurantOrder_OrderId",
                        column: x => x.OrderId,
                        principalTable: "tbl_RestaurantOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantAggregatorPayout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ExternalPayoutId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PeriodFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DeductionsAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    NetPaidAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantAggregatorPayout", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantAggregatorPayout_tbl_RestaurantChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "tbl_RestaurantChannel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantChannelAccount",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ExternalStoreId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ApiCredentialsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebhookSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastMenuSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastOrderSyncAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantChannelAccount", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantChannelAccount_tbl_RestaurantChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "tbl_RestaurantChannel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantChannelItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalItemId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExternalSku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChannelPrice = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    SyncStatus = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSyncMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantChannelItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantChannelItem_tbl_RestaurantChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "tbl_RestaurantChannel",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantChannelItem_tbl_RestaurantMenuItem_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "tbl_RestaurantMenuItem",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantAggregatorPayoutLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregatorOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExternalOrderId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    RestaurantDiscountAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    DeliveryFeeAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(28,8)", precision: 28, scale: 8, nullable: false),
                    MatchStatus = table.Column<int>(type: "int", nullable: false),
                    MatchMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantAggregatorPayoutLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantAggregatorPayoutLine_tbl_RestaurantAggregatorOrder_AggregatorOrderId",
                        column: x => x.AggregatorOrderId,
                        principalTable: "tbl_RestaurantAggregatorOrder",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantAggregatorPayoutLine_tbl_RestaurantAggregatorPayout_PayoutId",
                        column: x => x.PayoutId,
                        principalTable: "tbl_RestaurantAggregatorPayout",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tbl_RestaurantMenuSyncLog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChannelItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_RestaurantMenuSyncLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuSyncLog_tbl_RestaurantChannelItem_ChannelItemId",
                        column: x => x.ChannelItemId,
                        principalTable: "tbl_RestaurantChannelItem",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tbl_RestaurantMenuSyncLog_tbl_RestaurantChannel_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "tbl_RestaurantChannel",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorOrder_ChannelId",
                table: "tbl_RestaurantAggregatorOrder",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorOrder_OrderId",
                table: "tbl_RestaurantAggregatorOrder",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorOrder_ProviderExternalOrder",
                table: "tbl_RestaurantAggregatorOrder",
                columns: new[] { "TenantId", "Provider", "ExternalOrderId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [ExternalOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorOrder_TenantId_OrderId",
                table: "tbl_RestaurantAggregatorOrder",
                columns: new[] { "TenantId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorOrder_TenantId_Status_ReceivedAt",
                table: "tbl_RestaurantAggregatorOrder",
                columns: new[] { "TenantId", "Status", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayout_ChannelId",
                table: "tbl_RestaurantAggregatorPayout",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayout_TenantId_PeriodFrom_PeriodTo",
                table: "tbl_RestaurantAggregatorPayout",
                columns: new[] { "TenantId", "PeriodFrom", "PeriodTo" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayout_TenantId_Provider_ExternalPayoutId",
                table: "tbl_RestaurantAggregatorPayout",
                columns: new[] { "TenantId", "Provider", "ExternalPayoutId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayoutLine_AggregatorOrderId",
                table: "tbl_RestaurantAggregatorPayoutLine",
                column: "AggregatorOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayoutLine_PayoutId",
                table: "tbl_RestaurantAggregatorPayoutLine",
                column: "PayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayoutLine_TenantId_ExternalOrderId",
                table: "tbl_RestaurantAggregatorPayoutLine",
                columns: new[] { "TenantId", "ExternalOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayoutLine_TenantId_MatchStatus",
                table: "tbl_RestaurantAggregatorPayoutLine",
                columns: new[] { "TenantId", "MatchStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantAggregatorPayoutLine_TenantId_PayoutId",
                table: "tbl_RestaurantAggregatorPayoutLine",
                columns: new[] { "TenantId", "PayoutId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChangeLog_ChangedByDeviceId",
                table: "tbl_RestaurantChangeLog",
                column: "ChangedByDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChangeLog_TenantId_EntityType_Seq",
                table: "tbl_RestaurantChangeLog",
                columns: new[] { "TenantId", "EntityType", "Seq" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChangeLog_TenantSeq",
                table: "tbl_RestaurantChangeLog",
                columns: new[] { "TenantId", "Seq" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannel_TenantId_ChannelType",
                table: "tbl_RestaurantChannel",
                columns: new[] { "TenantId", "ChannelType" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannel_TenantId_Provider",
                table: "tbl_RestaurantChannel",
                columns: new[] { "TenantId", "Provider" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelAccount_ChannelId",
                table: "tbl_RestaurantChannelAccount",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelAccount_TenantId_ChannelId",
                table: "tbl_RestaurantChannelAccount",
                columns: new[] { "TenantId", "ChannelId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelAccount_TenantId_Provider_ExternalStoreId",
                table: "tbl_RestaurantChannelAccount",
                columns: new[] { "TenantId", "Provider", "ExternalStoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelItem_ChannelId",
                table: "tbl_RestaurantChannelItem",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelItem_ChannelMenuItem_Active",
                table: "tbl_RestaurantChannelItem",
                columns: new[] { "TenantId", "ChannelId", "MenuItemId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelItem_MenuItemId",
                table: "tbl_RestaurantChannelItem",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantChannelItem_TenantId_ExternalItemId",
                table: "tbl_RestaurantChannelItem",
                columns: new[] { "TenantId", "ExternalItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuSyncLog_ChannelId",
                table: "tbl_RestaurantMenuSyncLog",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuSyncLog_ChannelItemId",
                table: "tbl_RestaurantMenuSyncLog",
                column: "ChannelItemId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuSyncLog_TenantId_ChannelId_CreatedAt",
                table: "tbl_RestaurantMenuSyncLog",
                columns: new[] { "TenantId", "ChannelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantMenuSyncLog_TenantId_Status",
                table: "tbl_RestaurantMenuSyncLog",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPushToken_DeviceId",
                table: "tbl_RestaurantPushToken",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPushToken_TenantId_DeviceId",
                table: "tbl_RestaurantPushToken",
                columns: new[] { "TenantId", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantPushToken_TenantId_Token",
                table: "tbl_RestaurantPushToken",
                columns: new[] { "TenantId", "Token" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSyncUploadBatch_BatchGuid",
                table: "tbl_RestaurantSyncUploadBatch",
                columns: new[] { "TenantId", "BatchGuid" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [BatchGuid] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSyncUploadBatch_DeviceId",
                table: "tbl_RestaurantSyncUploadBatch",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_RestaurantSyncUploadBatch_TenantId_DeviceId_ReceivedAt",
                table: "tbl_RestaurantSyncUploadBatch",
                columns: new[] { "TenantId", "DeviceId", "ReceivedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tbl_RestaurantAggregatorPayoutLine");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantChangeLog");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantChannelAccount");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantMenuSyncLog");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantPushToken");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantSyncUploadBatch");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantAggregatorOrder");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantAggregatorPayout");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantChannelItem");

            migrationBuilder.DropTable(
                name: "tbl_RestaurantChannel");
        }
    }
}
