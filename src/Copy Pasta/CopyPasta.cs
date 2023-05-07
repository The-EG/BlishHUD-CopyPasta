using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;

namespace CopyPasta
{
    [Export(typeof(Module))]
    public class CopyPasta : Module
    {
        private static readonly Logger logger = Logger.GetLogger<CopyPasta>();

        internal ContentsManager contentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager directoriesManager => this.ModuleParameters.DirectoriesManager;

        [ImportingConstructor]
        public CopyPasta([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters) {
            moduleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings) {
            /*
            settings.DefineSetting(
                "ExampleSetting",
                "Default value",
                () => "Display Name",
                () => "Description");
            */
        }

        protected override async Task LoadAsync() {
            pastaMenu = new ContextMenuStrip();
            await LoadPasta();
        }

        protected override void Update(GameTime gameTime) {
            
        }

        protected override void OnModuleLoaded(EventArgs e) {
            moduleIcon = new CornerIcon() {
                Icon = contentsManager.GetTexture(@"copy-pasta.png"),
                Priority = "CopyPasta".GetHashCode(),
                BasicTooltipText = "Copy Pasta"
            };

            moduleIcon.Click += delegate {
                pastaMenu.Show(moduleIcon);
            };

            
            contextMenu = new ContextMenuStrip();
            ContextMenuStripItem reload = contextMenu.AddMenuItem("Reload CopyPastas");
            reload.Click += delegate {
                LoadPasta();
            };
            moduleIcon.Menu = contextMenu;
            
            base.OnModuleLoaded(e);
        }

        protected override void Unload() {
            moduleInstance = null;
        }

        private void parsePasta(JObject json, ContextMenuStrip menu) {
            foreach (JProperty prop in json.Properties()) {
                string name = prop.Name;
                ContextMenuStripItem item = new ContextMenuStripItem(name);

                JToken val = prop.Value;
                if (val.Type == JTokenType.Object) {
                    ContextMenuStrip subMenu = new ContextMenuStrip();
                    parsePasta((JObject)val, subMenu);
                    item.Submenu = subMenu;
                } else if (val.Type == JTokenType.String) {
                    item.BasicTooltipText = val.ToString();

                    item.Click += delegate {
                        ClipboardUtil.WindowsClipboardService.SetUnicodeBytesAsync(System.Text.Encoding.Unicode.GetBytes(val.ToString()));
                    };

                } else {
                    logger.Error("Can't parse json, expected array or string.");
                    return;
                }

                menu.AddMenuItem(item);
            }
        }

        private async Task LoadPasta() {
            pastaMenu.ClearChildren();

            string pastaFolder = directoriesManager.GetFullDirectoryPath("copypasta");

            logger.Debug($"Loading Pasta from {pastaFolder}...");

            string[] files = Directory.GetFiles(pastaFolder, "*.json");

            foreach ( string file in files ) {
                using (StreamReader reader = File.OpenText(file)) {
                    try {
                        JObject json = (JObject)JToken.ReadFrom(new JsonTextReader(reader));

                        parsePasta(json, pastaMenu);
                    } catch (Exception ex) {
                        logger.Warn($"Couldn't load json from {file}.");
                    }
                }
            }
        }

        internal static CopyPasta moduleInstance;

        private CornerIcon moduleIcon;
        private ContextMenuStrip pastaMenu;
        private ContextMenuStrip contextMenu;
    }
}
