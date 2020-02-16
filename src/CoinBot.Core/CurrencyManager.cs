﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CoinBot.Core
{
    public sealed class CurrencyManager : TickingService
    {
        /// <summary>
        ///     The <see cref="ICoinClient" />s.
        /// </summary>
        private readonly IReadOnlyList<ICoinClient> _coinClients;

        /// <summary>
        ///     The <see cref="Currency" /> list.
        /// </summary>
        private IReadOnlyCollection<Currency> _coinInfoCollection = Array.Empty<Currency>();

        /// <summary>
        ///     The <see cref="IGlobalInfo" />.
        /// </summary>
        private IGlobalInfo? _globalInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">Logging</param>
        /// <param name="coinClients">Clients</param>
        public CurrencyManager(ILogger<CurrencyManager> logger, IEnumerable<ICoinClient> coinClients)
            : base(TimeSpan.FromSeconds(value: 10), logger)
        {
            this._coinClients = coinClients.ToArray();
        }

        protected override async Task TickAsync()
        {
            try
            {
                await Task.WhenAll(this.UpdateCoinsAsync(), this.UpdateGlobalInfoAsync());
            }
            catch (Exception e)
            {
                this.Logger.LogError(new EventId(e.HResult), e, e.Message);
            }
        }

        public IGlobalInfo? GetGlobalInfo()
        {
            return this._globalInfo;
        }

        public Currency? Get(string nameOrSymbol)
        {
            return this.GetCoinBySymbol(nameOrSymbol) ?? this.GetCoinByName(nameOrSymbol);
        }

        private Currency? GetCoinBySymbol(string symbol)
        {
            return this._coinInfoCollection.FirstOrDefault(predicate: c => string.Compare(c.Symbol, symbol, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private Currency? GetCoinByName(string name)
        {
            return this._coinInfoCollection.FirstOrDefault(predicate: c => string.Compare(c.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public IEnumerable<Currency> Get(Func<Currency, bool> predicate)
        {
            return this._coinInfoCollection.Where(predicate);
        }

        private async Task UpdateCoinsAsync()
        {
            static Currency CreateCurrency(IReadOnlyList<ICoinInfo> cryptoInfo)
            {
                ICoinInfo first = cryptoInfo.First();

                Currency currency = new Currency(symbol: first.Symbol, name: first.Name) {ImageUrl = first.ImageUrl};

                foreach (ICoinInfo info in cryptoInfo)
                {
                    currency.AddDetails(info);
                }

                return currency;
            }

            IReadOnlyCollection<ICoinInfo>[] allCoinInfos = await Task.WhenAll(this._coinClients.Select(client => client.GetCoinInfoAsync()));

            List<Currency> currencies = new List<Currency> {new Currency(symbol: "EUR", name: "Euro"), new Currency(symbol: "USD", name: "United States dollar")};

            currencies.AddRange(allCoinInfos.SelectMany(ci => ci)
                                            .GroupBy(c => c.Symbol)
                                            .Select(selector: info => CreateCurrency(info.ToArray())));

            this._coinInfoCollection = new ReadOnlyCollection<Currency>(currencies);
        }

        private async Task UpdateGlobalInfoAsync()
        {
            ICoinClient client = this._coinClients.First();

            this._globalInfo = await client.GetGlobalInfoAsync();
        }
    }
}