﻿#region USING_DIRECTIVES

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Freud.Common.Configuration;
using Freud.Modules.Search.Common;
using Freud.Modules.Search.Services;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion USING_DIRECTIVES

namespace Freud.Modules.Search.Extensions
{
    public static class CommonExtensions
    {
        private static readonly string _unknown = Formatter.Italic("Unknown");

        public static Page ToDiscordPage(this MovieInfo info, DiscordColor? color = null)
            => new Page(embed: info.ToDiscordEmbed(color));

        public static IReadOnlyList<Page> ToDiscordPages(this GoodreadsSearchInfo info)
        {
            var pages = new List<Page>();

            foreach (var work in info.Results)
            {
                var emb = new DiscordEmbedBuilder
                {
                    Title = work.Book.Title,
                    ThumbnailUrl = work.Book.ImageUrl,
                    Color = DiscordColor.DarkGray
                };

                emb.AddField("Author", work.Book.Author.Name, inline: true);
                emb.AddField("Rating", $"{work.AverageRating} out of {work.RatingsCount} votes", inline: true);
                emb.AddField("Date", $"{work.PublicationDayString}/{work.PublicationMonthString}/{work.PublicationYearString}", inline: true);
                emb.AddField("Books count", work.BooksCount.ToString(), inline: true);
                emb.AddField("Work ID", work.Id.ToString(), inline: true);
                emb.AddField("Book ID", work.Book.Id.ToString(), inline: true);

                emb.WithFooter($"Fethed results using Goodreads API in {info.QueryTime}s");

                pages.Add(new Page(embed: emb));
            }

            return pages.AsReadOnly();
        }

        public static DiscordEmbedBuilder ToDiscordEmbed(this IpInfo info, DiscordColor? color = null)
        {
            var emb = new DiscordEmbedBuilder
            {
                Title = $"IP geolocation info for {info.Ip}",
            };

            if (!(color is null))
                emb.WithColor(color.Value);

            emb.AddField("Location", $"{info.City}, {info.RegionName} {info.RegionCode}, {info.CountryName} {info.CountryCode}");
            emb.AddField("Exact location", $"({info.Latitude}, {info.Longitude})", inline: true);
            emb.AddField("ISP", string.IsNullOrWhiteSpace(info.Isp) ? _unknown : info.Isp, inline: true);
            emb.AddField("Organization", string.IsNullOrWhiteSpace(info.Organization) ? _unknown : info.Organization, inline: true);
            emb.AddField("AS number", string.IsNullOrWhiteSpace(info.As) ? _unknown : info.As, inline: true);

            emb.WithFooter("Powered by ip-api.");

            return emb;
        }

        public static DiscordEmbedBuilder ToDiscordEmbed(this MovieInfo info, DiscordColor? color = null)
        {
            var emb = new DiscordEmbedBuilder
            {
                Title = info.Title,
                Description = info.Plot,
                Url = $"http://www.imdb.com/title/{ info.IMDbId }",
            };

            if (!(color is null))
                emb.WithColor(color.Value);

            if (!string.IsNullOrWhiteSpace(info.Type))
                emb.AddField("Type", info.Type, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Year))
                emb.AddField("Air time", info.Year, inline: true);

            if (!string.IsNullOrWhiteSpace(info.IMDbId))
                emb.AddField("IMDb ID", info.IMDbId, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Genre))
                emb.AddField("Genre", info.Genre, inline: true);

            if (!string.IsNullOrWhiteSpace(info.ReleaseDate))
                emb.AddField("Release date", info.ReleaseDate, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Rated))
                emb.AddField("Rated", info.Rated, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Duration))
                emb.AddField("Duration", info.Duration, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Actors))
                emb.AddField("Actors", info.Actors, inline: true);

            if (!string.IsNullOrWhiteSpace(info.IMDbRating) && !string.IsNullOrWhiteSpace(info.IMDbVotes))
                emb.AddField("IMDb rating", $"{info.IMDbRating} out of {info.IMDbVotes} votes", inline: true);

            if (!string.IsNullOrWhiteSpace(info.Writer))
                emb.AddField("Writer", info.Writer, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Director))
                emb.AddField("Director", info.Director, inline: true);

            if (!string.IsNullOrWhiteSpace(info.Poster) && info.Poster != "N/A")
                emb.WithThumbnailUrl(info.Poster);

            emb.WithFooter("Powered by OMDb.");

            return emb;
        }

        public static DiscordEmbedBuilder ToDiscordEmbed(this Quote quote, string altTitle = null)
        {
            var emb = new DiscordEmbedBuilder
            {
                Title = string.IsNullOrWhiteSpace(altTitle) ? "Quote" : altTitle,
                Description = Formatter.Italic($"\"{quote.Content}\""),
                Color = DiscordColor.SpringGreen,
                ThumbnailUrl = quote.BackgroundImageUrl,
                Url = quote.Permalink
            };
            emb.AddField("Author", quote.Author);

            emb.WithFooter("Powered by theysaidso.com");

            return emb;
        }

        public static DiscordEmbedBuilder ToDiscordEmbed(this WeatherData data, DiscordColor? color = null)
        {
            var emb = new DiscordEmbedBuilder();

            if (!(color is null))
                emb.WithColor(color.Value);

            emb.AddField($"{StaticDiscordEmoji.Globe} Location", $"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{ data.Id })", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Ruler} Coordinates", $"{data.Coord.Latitude}, {data.Coord.Longitude}", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Cloud} Condition", string.Join(", ", data.Weather.Select(w => w.Main)), inline: true);
            emb.AddField($"{StaticDiscordEmoji.Drops} Humidity", $"{data.Main.Humidity}%", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Thermometer} Temperature", $"{data.Main.Temperature:F1}°C", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Thermometer} Min/Max Temp", $"{data.Main.TemperatureMin:F1}°C / {data.Main.TemperatureMax:F1}°C", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Wind} Wind speed", data.Wind.Speed + " m/s", inline: true);

            emb.WithThumbnailUrl(WeatherService.GetWeatherIconUrl(data.Weather.FirstOrDefault()));

            emb.WithFooter("Powered by openweathermap.org");

            return emb;
        }

        public static IReadOnlyList<DiscordEmbedBuilder> ToDiscordEmbedBuilders(this Forecast forecast, int amount = 7)
        {
            var embeds = new List<DiscordEmbedBuilder>();

            for (int i = 0; i < forecast.WeatherDataList.Count && i < amount; i++)
            {
                var data = forecast.WeatherDataList[i];
                var date = DateTime.UtcNow.AddDays(i + 1);

                var emb = new DiscordEmbedBuilder
                {
                    Title = $"Forecast for {date.DayOfWeek}, {date.Date.ToUniversalTime().ToShortDateString()}",
                    Color = DiscordColor.Aquamarine
                };

                emb.AddField($"{StaticDiscordEmoji.Globe} Location", $"[{forecast.City.Name + ", " + forecast.City.Country}]({WeatherService.GetCityUrl(forecast.City)})", inline: true);
                emb.AddField($"{StaticDiscordEmoji.Ruler} Coordinates", $"{forecast.City.Coord.Latitude}, {forecast.City.Coord.Longitude}", inline: true);
            }

            return embeds.AsReadOnly();
        }

        public static DiscordEmbedBuilder AddToDiscordEmbed(this PartialWeatherData data, DiscordEmbedBuilder emb)
        {
            emb.AddField($"{StaticDiscordEmoji.Cloud} Condition", string.Join(", ", data.Weather.Select(w => w.Main)), inline: true);
            emb.AddField($"{StaticDiscordEmoji.Drops} Humidity", $"{data.Main.Humidity}%", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Thermometer} Temperature", $"{data.Main.Temperature:F1}°C", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Thermometer} Min/Max Temp", $"{data.Main.TemperatureMin:F1}°C / {data.Main.TemperatureMax:F1}°C", inline: true);
            emb.AddField($"{StaticDiscordEmoji.Wind} Wind speed", data.Wind.Speed + " m/s", inline: true);
            emb.WithThumbnailUrl(WeatherService.GetWeatherIconUrl(data.Weather.FirstOrDefault()));
            emb.WithFooter("Powered by openweathermap.org");

            return emb;
        }

        public static string ToInfoString(this UrbanDictionaryList res)
        {
            var sb = new StringBuilder("Definition by ");
            sb.Append(Formatter.Bold(res.Author)).AppendLine().AppendLine();
            sb.Append(Formatter.Bold(res.Word)).Append(" :");
            sb.AppendLine(Formatter.BlockCode(res.Definition.Trim().Truncate(1000)));

            if (!string.IsNullOrWhiteSpace(res.Example))
                sb.Append("Examples:").AppendLine(Formatter.BlockCode(res.Example.Trim().Truncate(250)));
            sb.Append(res.Permalink);

            return sb.ToString();
        }
    }
}
