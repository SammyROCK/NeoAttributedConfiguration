using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AttributedConfiguration {
	public abstract class BaseConfiguration {
		private readonly string? path;

		public BaseConfiguration(IConfiguration configuration) {
			this.Configuration = configuration;
			this.path = configuration is IConfigurationSection configurationSection
				? configurationSection.Path
				: null;
		}

		public IConfiguration Configuration { get; }

		protected TEnum GetEnum<TEnum>(string key) where TEnum : struct
			=> Enum.Parse<TEnum>(this.GetString(key), true);

		protected TimeSpan GetTimespan(string key_prefix, TimeSource timeSource) {
			var key = $"{key_prefix}{timeSource}";
			var value = this.GetDouble(key);
			return timeSource switch {
				TimeSource.InMilliseconds => TimeSpan.FromMilliseconds(value),
				TimeSource.InSeconds => TimeSpan.FromSeconds(value),
				TimeSource.InMinutes => TimeSpan.FromMinutes(value),
				TimeSource.InHours => TimeSpan.FromHours(value),
				TimeSource.InDays => TimeSpan.FromDays(value),
				_ => throw new NotImplementedException($"{nameof(TimeSource)} of value {timeSource} has not been implemented"),
			};
		}

		protected TimeSpan? TryGetTimespan(string key_prefix, TimeSource timeSource) {
			var key = $"{key_prefix}{timeSource}";
			var nullableValue = this.TryGetDouble(key);
			if(nullableValue is null) { return null; }
			var value = nullableValue.Value;
			return timeSource switch {
				TimeSource.InMilliseconds => TimeSpan.FromMilliseconds(value),
				TimeSource.InSeconds => TimeSpan.FromSeconds(value),
				TimeSource.InMinutes => TimeSpan.FromMinutes(value),
				TimeSource.InHours => TimeSpan.FromHours(value),
				TimeSource.InDays => TimeSpan.FromDays(value),
				_ => throw new NotImplementedException($"{nameof(TimeSource)} of value {timeSource} has not been implemented"),
			};
		}

		protected double GetDouble(string key)
			=> double.Parse(this.GetString(key), CultureInfo.InvariantCulture);

		protected double? TryGetDouble(string key)
			=> double.TryParse(this.TryGetString(key), out var value) ? value : default(double?);

		protected int GetInt(string key)
			=> int.Parse(this.GetString(key));

		protected double? TryGetInt(string key)
			=> int.TryParse(this.TryGetString(key), out var value) ? value : default(int?);

		protected char GetChar(string key)
			=> this.GetString(key).Single();

		protected char? TryGetChar(string key)
			=> this.TryGetString(key)?.FirstOrDefault();

		protected bool GetBool(string key)
			=> bool.Parse(this.GetString(key));

		protected bool? TryGetBool(string key)
			=> bool.TryParse(this.TryGetString(key), out var value) ? value : default(bool?);

		protected string GetString(string key)
			=> this.TryGetString(key)
				?? throw new ConfigurationNotFoundException(this.BuildConfigurationPath(key));

		protected string? TryGetString(string key)
			=> this.Configuration[key];

		protected string[] GetStrings(string key)
			=> this.GetSections(key)
				.Select(section => section.Value)
				.ToArray();

		protected T? TryGet<T>(string key) where T : BaseConfiguration {
			var section = this.Configuration.GetSection(key);
			if(section.Exists() is false) { return null; }

			return section.Resolve<T>();
		}

		protected T Get<T>(string key) where T : BaseConfiguration
			=> this.GetSection(key).Resolve<T>();

		protected T[] GetMany<T>(string key) where T : BaseConfiguration
			=> this.GetSections(key)
				.Select(section => section.Resolve<T>())
				.ToArray();

		protected IDictionary<string, string> GetDict(string key)
			=> this.GetSections(key)
				.ToDictionary(s => s.Key, s => s.Value);

		protected IDictionary<int, int> GetIntIntDict(string key)
			=> this.GetSections(key)
				.ToDictionary(
					s => int.Parse(s.Key),
					s => int.Parse(s.Value));

		protected IEnumerable<IConfigurationSection> GetSections(string key)
			=> this.GetSection(key)
				.GetChildren();

		protected IConfigurationSection GetSection(string key)
			=> this.Configuration.GetSection(key)
				?? throw new ConfigurationNotFoundException(this.BuildConfigurationPath(key));

		private string BuildConfigurationPath(string key)
			=> this.path is null
				? key
				: $"{this.path}:{key}";
	}
}
