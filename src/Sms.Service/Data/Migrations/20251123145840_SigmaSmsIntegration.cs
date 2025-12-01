using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sms.Service.Migrations
{
    /// <inheritdoc />
    public partial class SigmaSmsIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "sms_notifications",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "sms_notifications",
                newName: "message");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sms_notifications",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ServiceType",
                table: "sms_notifications",
                newName: "service_type");

            migrationBuilder.RenameColumn(
                name: "SentAt",
                table: "sms_notifications",
                newName: "sent_at");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "sms_notifications",
                newName: "retry_count");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "sms_notifications",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "NotificationId",
                table: "sms_notifications",
                newName: "notification_id");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "sms_notifications",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sms_notifications",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_sms_notifications_Status",
                table: "sms_notifications",
                newName: "IX_sms_notifications_status");

            migrationBuilder.RenameIndex(
                name: "IX_sms_notifications_NotificationId",
                table: "sms_notifications",
                newName: "IX_sms_notifications_notification_id");

            migrationBuilder.AddColumn<Guid>(
                name: "external_id",
                table: "sms_notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_status",
                table: "sms_notifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_status_check",
                table: "sms_notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sms_notifications_external_id",
                table: "sms_notifications",
                column: "external_id");

            migrationBuilder.CreateIndex(
                name: "IX_sms_notifications_last_status_check",
                table: "sms_notifications",
                column: "last_status_check");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sms_notifications_external_id",
                table: "sms_notifications");

            migrationBuilder.DropIndex(
                name: "IX_sms_notifications_last_status_check",
                table: "sms_notifications");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "sms_notifications");

            migrationBuilder.DropColumn(
                name: "final_status",
                table: "sms_notifications");

            migrationBuilder.DropColumn(
                name: "last_status_check",
                table: "sms_notifications");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "sms_notifications",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "message",
                table: "sms_notifications",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sms_notifications",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "service_type",
                table: "sms_notifications",
                newName: "ServiceType");

            migrationBuilder.RenameColumn(
                name: "sent_at",
                table: "sms_notifications",
                newName: "SentAt");

            migrationBuilder.RenameColumn(
                name: "retry_count",
                table: "sms_notifications",
                newName: "RetryCount");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "sms_notifications",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "notification_id",
                table: "sms_notifications",
                newName: "NotificationId");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "sms_notifications",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "sms_notifications",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_sms_notifications_status",
                table: "sms_notifications",
                newName: "IX_sms_notifications_Status");

            migrationBuilder.RenameIndex(
                name: "IX_sms_notifications_notification_id",
                table: "sms_notifications",
                newName: "IX_sms_notifications_NotificationId");
        }
    }
}
