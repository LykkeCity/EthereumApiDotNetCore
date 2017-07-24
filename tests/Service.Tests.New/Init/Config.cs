using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Core.Repositories;
using Core.Settings;
using EthereumJobs.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Services;
using Common.Log;

// ReSharper disable once CheckNamespace
namespace Tests
{
    public class Config
    {
        public static IServiceProvider Services { get; set; }
        public static ILog Logger => Services.GetService<ILog>();

        private SettingsWrapper ReadSettings()
        {
            try
            {
                var url = File.ReadAllText(@"..\..\..\configurationUrl.url");
                if (string.IsNullOrWhiteSpace(url))
                {

                    return null;
                }
                SettingsWrapper settings = GeneralSettingsReader.ReadGeneralSettings<SettingsWrapper>(url);

                return settings;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        //private TestSettings ReadTestSettings()
        //{
        //    try
        //    {
        //        var json = File.ReadAllText(@"..\..\..\generalsettings.json");
        //        if (string.IsNullOrWhiteSpace(json))
        //        {

        //            return null;
        //        }
        //        TestSettings settings = GeneralSettingsReader.ReadSettingsFromData<TestSettings>(json);

        //        return settings;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }
        //}

        public async Task Initialize()
        {
            //Constants.StoragePrefix = "tests";

            IServiceCollection collection = new ServiceCollection();
            var settings = ReadSettings();

            //var testSetting = ReadTestSettings();
            Assert.IsNotNull(settings, "Please, provide generalsettings.json file");

            collection.InitJobDependencies(settings.EthereumCore, settings.SlackNotifications);

            Services = collection.BuildServiceProvider();
            Services.ActivateRequestInterceptor();
        }

        public class TestSettings
        {
            public Dictionary<string, TestContract> CoinContracts { get; set; }
        }

        public class TestContract : Core.Settings.EthereumContract
        {
            public int Multiplier { get; set; }
            public bool Payable { get; set; }
        }
    }
}
