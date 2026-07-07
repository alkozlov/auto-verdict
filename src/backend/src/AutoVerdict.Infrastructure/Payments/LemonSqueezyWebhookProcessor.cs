using System.Text.Json;
using AutoVerdict.Application.Payments;
using AutoVerdict.Contracts.Configuration;
using AutoVerdict.Domain.Entities;
using AutoVerdict.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutoVerdict.Infrastructure.Payments;

public enum WebhookOutcome { Processed, Ignored, InvalidSignature, DuplicateOrder }

public sealed class LemonSqueezyWebhookProcessor(AppDbContext db, IPaymentService paymentService)
{
    public async Task<WebhookOutcome> ProcessAsync(string body, string signature, CancellationToken ct)
    {
        if (!paymentService.ValidateWebhookSignature(body, signature))
            return WebhookOutcome.InvalidSignature;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(body); }
        catch { return WebhookOutcome.Ignored; }

        using (doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("meta", out var meta)) return WebhookOutcome.Ignored;
            if (!meta.TryGetProperty("event_name", out var eventNameProp)) return WebhookOutcome.Ignored;
            if (eventNameProp.GetString() != "order_created") return WebhookOutcome.Ignored;

            if (!root.TryGetProperty("data", out var data)) return WebhookOutcome.Ignored;

            var externalOrderId = data.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrEmpty(externalOrderId)) return WebhookOutcome.Ignored;

            // Only handle paid orders
            var status = data.TryGetProperty("attributes", out var attrs)
                      && attrs.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString() : null;
            if (status != "paid") return WebhookOutcome.Ignored;

            if (!meta.TryGetProperty("custom_data", out var customData)) return WebhookOutcome.Ignored;
            if (!customData.TryGetProperty("user_id", out var userIdProp)) return WebhookOutcome.Ignored;
            if (!customData.TryGetProperty("package", out var packageProp)) return WebhookOutcome.Ignored;

            if (!Guid.TryParse(userIdProp.GetString(), out var userId)) return WebhookOutcome.Ignored;
            var packageKey = packageProp.GetString();
            if (string.IsNullOrEmpty(packageKey)) return WebhookOutcome.Ignored;

            var package = CreditPackage.FindByKey(packageKey);
            if (package is null) return WebhookOutcome.Ignored;

            // Idempotency check
            var alreadyProcessed = await db.PaymentOrders
                .AnyAsync(o => o.ExternalOrderId == externalOrderId, ct);
            if (alreadyProcessed) return WebhookOutcome.DuplicateOrder;

            var now = DateTimeOffset.UtcNow;
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await db.UserCredits
                .Where(c => c.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Balance, c => c.Balance + package.Credits)
                    .SetProperty(c => c.UpdatedAt, now), ct);

            db.CreditLedgerEntries.Add(new CreditLedgerEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = package.Credits,
                Reason = "credit_purchase",
                CreatedAt = now,
            });

            db.PaymentOrders.Add(new PaymentOrder
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PackageKey = packageKey,
                CreditsGranted = package.Credits,
                ExternalOrderId = externalOrderId,
                CreatedAt = now,
            });

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Concurrent duplicate delivery lost the race on the ExternalOrderId
                // unique index; the transaction rolled back the credit grant with it.
                return WebhookOutcome.DuplicateOrder;
            }
        }

        return WebhookOutcome.Processed;
    }
}
