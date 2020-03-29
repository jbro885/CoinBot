﻿using System;
using System.Collections.Generic;
using System.Linq;
using CoinBot.Clients.CoinMarketCap;
using CoinBot.Core;

namespace CoinBot.Discord.Commands
{
    internal sealed class InterpretedCoinInfo : ICoinInfo
    {
        public InterpretedCoinInfo(Currency currency, MarketManager marketManager, Currency? usd, Currency? eth, Currency? btc)
        {
            this.Id = currency.Symbol;
            this.Symbol = currency.Symbol;
            this.Name = currency.Name;

            this.PriceUsd = GetPriceFromMarkets(currency, marketManager, usd);
            this.PriceEth = GetPriceFromMarkets(currency, marketManager, eth);
            this.PriceBtc = GetPriceFromMarkets(currency, marketManager, btc);
        }

        public string Id { get; }

        public string ImageUrl =>
            Helpers.CurrencyImageUrl(this.Symbol)
                   .ToString();

        public string Name { get; }

        public string Symbol { get; }

        public int? Rank { get; }

        public decimal? PriceUsd { get; }

        public decimal? PriceBtc { get; }

        public decimal? PriceEth { get; }

        public double? Volume { get; }

        public double? MarketCap { get; }

        public double? HourChange { get; }

        public double? DayChange { get; }

        public double? WeekChange { get; }

        public DateTime? LastUpdated { get; }

        private static decimal? GetPriceFromMarkets(Currency currency, MarketManager marketManager, Currency? quoteCurrency)
        {
            if (quoteCurrency == null)
            {
                return null;
            }

            IEnumerable<MarketSummaryDto> markets = marketManager.GetPair(currency, quoteCurrency);

            return markets.Where(predicate: x => x.Last != null)
                          .OrderByDescending(keySelector: x => x.LastUpdated ?? DateTime.MinValue)
                          .FirstOrDefault()
                          ?.Last;
        }
    }
}