namespace Core.Settings
{
	public static class GeneralSettingsReader
	{
		public static T ReadSettingsFromData<T>(string jsonData)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonData);
		}
	}
}
