using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories
{
	public interface IAppSetting
	{
		string Key { get; }
		string Value { get; set; }
	}

	public class AppSetting : IAppSetting
	{
		public string Key { get; set; }
		public string Value { get; set; }
	}

	public interface IAppSettingsRepository
	{
		Task SetSettingAsync(string key, string value);

		Task<string> GetSettingAsync(string key);
	}
}
