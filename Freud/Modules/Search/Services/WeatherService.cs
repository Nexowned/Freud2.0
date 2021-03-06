﻿#region USING_DIRECTIVES

using DSharpPlus.Entities;
using Freud.Modules.Search.Common;
using Freud.Modules.Search.Extensions;
using Freud.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion USING_DIRECTIVES

namespace Freud.Modules.Search.Services
{
    public class WeatherService : FreudHttpService
    {
        private static readonly string _url = "http://api.openweathermap.org/data/2.5";

        private readonly string key;

        public WeatherService(string key)
        {
            this.key = key;
        }

        public override bool IsDisabled()
            => string.IsNullOrWhiteSpace(this.key);

        public static string GetCityUrl(City city)
        {
            if (city is null)
                throw new ArgumentException("City missing", nameof(city));

            return $"https://openweathermap.org/city/{ city.Id }";
        }

        public static string GetWeatherIconUrl(Weather weather)
        {
            if (weather is null)
                throw new ArgumentException("Weather missing", nameof(weather));

            return $"https://openweathermap.org/img/w/{ weather.Icon }.png";
        }

        public async Task<DiscordEmbed> GetEmbeddedCurrentWeatherDataAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query missing", nameof(query));
            try
            {
                string response = await _http.GetStringAsync($"{_url}/weather?q={query}&appid={this.key}&units=metric").ConfigureAwait(false);
                var data = JsonConvert.DeserializeObject<WeatherData>(response);

                return data.ToDiscordEmbed(DiscordColor.Aquamarine);
            } catch
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<DiscordEmbedBuilder>> GetEmbeddedWeatherForecastAsync(string query, int amount = 7)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query missing", nameof(query));

            if (amount < 1 || amount > 20)
                throw new ArgumentException("Days amount out of range (max 20)", nameof(amount));
            try
            {
                string response = await _http.GetStringAsync($"{_url}/forecast?q={query}&appid={this.key}&units=metric").ConfigureAwait(false);
                var forecast = JsonConvert.DeserializeObject<Forecast>(response);

                return forecast.ToDiscordEmbedBuilders(amount);
            } catch
            {
                return null;
            }
        }
    }
}
