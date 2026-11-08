namespace FlashSaleStore.Tests;

internal static class Program
{
    public static async Task<int> Main()
    {
        var domain = new DomainWorkflowTests();
        var validation = new ValidationAndGatewayTests();
        var queryHandlers = new QueryHandlerTests();
        var expiryHandler = new OrderExpiryHandlerTests();
        var tests = new (string Name, Func<Task> Run)[]
        {
            (nameof(domain.Checkout_pricing_splits_sale_quota_and_regular_quantity), () => RunSync(domain.Checkout_pricing_splits_sale_quota_and_regular_quantity)),
            (nameof(domain.Exhausted_or_inactive_flash_sale_uses_regular_price), () => RunSync(domain.Exhausted_or_inactive_flash_sale_uses_regular_price)),
            (nameof(domain.Flash_sale_schedule_policy_rejects_overlap_but_accepts_adjacent_windows), () => RunSync(domain.Flash_sale_schedule_policy_rejects_overlap_but_accepts_adjacent_windows)),
            (nameof(domain.Cart_rejects_other_users_and_quantity_above_available_stock), () => RunSync(domain.Cart_rejects_other_users_and_quantity_above_available_stock)),
            (nameof(domain.Expiry_creates_inventory_release_plan_once_with_sale_quota_split), () => RunSync(domain.Expiry_creates_inventory_release_plan_once_with_sale_quota_split)),
            (nameof(domain.Late_success_records_payment_for_manual_refund_without_restoring_order), () => RunSync(domain.Late_success_records_payment_for_manual_refund_without_restoring_order)),
            (nameof(validation.Catalog_and_cart_validators_accept_valid_inputs_and_reject_invalid_values), () => RunSync(validation.Catalog_and_cart_validators_accept_valid_inputs_and_reject_invalid_values)),
            (nameof(validation.Flash_sale_checkout_and_admin_order_validators_check_required_enums_and_periods), () => RunSync(validation.Flash_sale_checkout_and_admin_order_validators_check_required_enums_and_periods)),
            (nameof(validation.Payos_webhook_is_accepted_only_when_signature_matches_signed_data), validation.Payos_webhook_is_accepted_only_when_signature_matches_signed_data),
            (nameof(validation.Payos_checkout_creates_signed_request_and_returns_provider_link), validation.Payos_checkout_creates_signed_request_and_returns_provider_link),
            (nameof(queryHandlers.Customer_history_and_detail_handlers_keep_the_user_scope), queryHandlers.Customer_history_and_detail_handlers_keep_the_user_scope),
            (nameof(queryHandlers.Admin_order_and_flash_sale_queries_validate_filters_before_accessing_storage), queryHandlers.Admin_order_and_flash_sale_queries_validate_filters_before_accessing_storage),
            (nameof(expiryHandler.Expiry_handler_releases_stock_and_sale_quota_once_and_expires_payment), expiryHandler.Expiry_handler_releases_stock_and_sale_quota_once_and_expires_payment)
        };

        var failures = new List<(string Name, Exception Exception)>();
        foreach (var test in tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception exception)
            {
                failures.Add((test.Name, exception));
                Console.Error.WriteLine($"FAIL {test.Name}: {exception.Message}");
            }
        }

        Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} checks passed.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static Task RunSync(Action test)
    {
        test();
        return Task.CompletedTask;
    }
}
