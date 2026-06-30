namespace RandomLoadout
{
    internal sealed class CurrencyNoConsumeToggleService : NoConsumeToggleServiceBase
    {
        protected override string EnableResultKey
        {
            get { return "result.currency_no_consume.enable.success"; }
        }

        protected override string DisableResultKey
        {
            get { return "result.currency_no_consume.disable.success"; }
        }

        protected override bool TryPrepareForEnable(PlayerController player, out GrantCommandExecutionResult failureResult)
        {
            if (player == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.player_not_ready");
                return false;
            }

            PlayerConsumables consumables = player.carriedConsumables;
            if (consumables == null)
            {
                failureResult = GrantCommandExecutionResult.Localized(false, "result.common.consumables_not_ready");
                return false;
            }

            failureResult = null;
            return true;
        }

        public bool ShouldOverrideAffordability(ShopItemController item)
        {
            if (!IsEnabled || item == null)
            {
                return false;
            }

            // Resourceful Rat Key uses a staged contribution flow backed by its own progression stat.
            // Keep that purchase path untouched so casing no-consume only affects normal coin-priced buys.
            if (item.IsResourcefulRatKey)
            {
                return false;
            }

            return item.CurrencyType == ShopItemController.ShopCurrencyType.COINS ||
                   item.CurrencyType == ShopItemController.ShopCurrencyType.BLANKS;
        }

        protected override bool TryGetCurrentValue(PlayerController player, out float currentValue)
        {
            currentValue = 0f;
            if (player == null || player.carriedConsumables == null)
            {
                return false;
            }

            currentValue = player.carriedConsumables.Currency;
            return true;
        }

        protected override void SetCurrentValue(PlayerController player, float value)
        {
            if (player != null && player.carriedConsumables != null)
            {
                player.carriedConsumables.Currency = UnityEngine.Mathf.RoundToInt(value);
            }
        }
    }
}
