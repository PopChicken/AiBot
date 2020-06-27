using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.ComponentModel;
using vip.popop.pcr.GHelper.Modules;
using Native.Sdk.Cqp;
using vip.popop.pcr.GHelper.Modules.Auth;

namespace vip.popop.pcr.GHelper {

    public class Configuration {

        public long Master { get; set; }

        public bool IsLinkGeneratorEnabled { get; set; }

        public bool IsPhonographEnabled { get; set; }

        public bool IsSleepHelperEnabled { get; set; }

        public bool IsRepeaterEnabled { get; set; }

        public bool IsSoloEnabled { get; set; }

        public bool IsForestEnabled { get; set; }

        public PhonographConf Phonograph { get; set; }

        public RepeaterConf Repeater { get; set; }

        public SleepHelperConf SleepHelper { get; set; }

        public SoloConf Solo { get; set; }
    }

    public class PhonographConf {

        public string Platform { get; set; }

        public bool WithLink { get; set; }

    }

    public class RepeaterConf {

        public double RepeatChance { get; set; }

        public int Cooldown { get; set; }

        public List<string> BanWords { get; set; }

    }

    public class SleepHelperConf {
        public class Time {

            public int Hour { get; set; }

            public int Minute { get; set; }

            public string GetUp { get; set; }

        }

        public string LaunchTime { get; set; }

        public Time TimeSpan { get; set; }
    }

    public class SoloConf {

        public int ValidSpan { get; set; }

        public int CheckTime { get; set; }

        public int MaxStack { get; set; }

    }

    public class ForestConf {

    }

    public static class Main {

        private static Configuration Config;

        private static Phonograph phonograph;

        private static Repeater repeater;

        private static SleepHelper sleepHelper;

        private static LinkGenerator linkGenerator;

        private static Solo solo;

        private static Forest forest;

        private static Account accountManager;

        private static LetThemOut letTheyOut;

        public static void Initialize(CQLog log) {
            if (!File.Exists("config.json")) {
                FileStream configFile = new FileStream("config.json", FileMode.OpenOrCreate);
                StreamReader reader = new StreamReader(
                    System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("vip.popop.pcr.GHelper.Resources.default.json")
                );
                StreamWriter writer = new StreamWriter(configFile);
                string defaultJson = reader.ReadToEnd();
                writer.Write(defaultJson);
                Config = JsonConvert.DeserializeObject<Configuration>(defaultJson);
                reader.Close();
                writer.Close();
                configFile.Close();
            } else {
                FileStream configFileO = new FileStream("config.json", FileMode.Open);
                StreamReader configReader = new StreamReader(configFileO);
                Config = JsonConvert.DeserializeObject<Configuration>(configReader.ReadToEnd());
                configReader.Close();
                configReader.Dispose();
                configFileO.Close();
            }

            Event_GroupMessage.ClearHandler();
            Event_PrivateMessage.ClearHandler();

            accountManager = new Account {
                logger = log
            };
            accountManager.OnInitialize();

            if (Config.IsPhonographEnabled) {
                phonograph = new Phonograph {
                    Client = (Phonograph.ClientType)Enum.Parse(typeof(Phonograph.ClientType), Config.Phonograph.Platform),
                    IsWithLink = Config.Phonograph.WithLink
                };
                phonograph.OnInitialize();
            }

            if (Config.IsRepeaterEnabled) {
                repeater = new Repeater {
                    repeatChance = Config.Repeater.RepeatChance,
                    cooldown = Config.Repeater.Cooldown,
                    banWords = Config.Repeater.BanWords
                };
                repeater.OnInitialize();
            }

            if (Config.IsSleepHelperEnabled) {
                sleepHelper = new SleepHelper {

                };
                sleepHelper.OnInitialize();
            }

            if (Config.IsLinkGeneratorEnabled) {
                linkGenerator = new LinkGenerator {

                };
                linkGenerator.OnInitialize();
            }

            if (Config.IsSoloEnabled) {
                solo = new Solo {
                    validSpan = Config.Solo.ValidSpan,
                    checkTime = Config.Solo.CheckTime,
                    maxStack = Config.Solo.MaxStack
                };
                solo.OnInitialize();
            }

            if (Config.IsForestEnabled) {
                forest = new Forest() {
                    Logger = log
                };
                forest.OnInitialize();
            }

            //letTheyOut = new LetThemOut {

            //};
            //letTheyOut.OnInitialize();
        }
    }
}
