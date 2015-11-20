using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Remote.JADE.Resources;
using System.Reflection;
using System.IO;
using Windows.Storage;
using Newtonsoft.Json;
using Windows.Media;
using System.Threading.Tasks;
using Windows.Phone.Speech.VoiceCommands;
using System.Windows.Shapes;
using System.Windows.Media;

namespace Remote.JADE
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            RegisterVoiceCommands();
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        /// <summary>
        ///  Load VCD file and install voice commands into system. After that, you can use Cortana as entry to trigger your app
        /// </summary>
        /// <param name="vcdPath"></param>
        private async void RegisterVoiceCommands()
        {
            string log = "LOAD_OK";

            try
            {
                Uri uriVoiceCommands = new Uri("ms-appx:///Assets/JADE.ChineseVCD.WP8.1.xml", UriKind.Absolute);
                await VoiceCommandService.InstallCommandSetsFromFileAsync(uriVoiceCommands);
                //VoiceCommandSet voicecmdset = VoiceCommandService.InstalledCommandSets["JADE.VCD"];
            }
            catch (Exception err)
            {
                log = "S_FAIL - err:" + err.Message + " stack:" + err.StackTrace; 
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            //KonekeSocket ks = new KonekeSocket();
            //string authcode = ks.getAuthCode();

            //string accessToken = await ks.GetAccessToken(authcode);
            //string userId = await ks.GetUserId(accessToken);

            //GetKList_Result gr = await ks.GetKList(accessToken, userId);

            //for (int i = 0; i < gr.kIDs.Count; i++)
            //{
            //    ks.SwitchKLight(accessToken, userId, gr.kIDs[i].kid, "close");
            //}

            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {
                //
                // Which command was recognized in the VCD.XML file? e.g. "Open all socket light"
                //
                string voiceCommandName = null;
                NavigationContext.QueryString.TryGetValue("voiceCommandName", out voiceCommandName);

                if (voiceCommandName == null)
                {
                    return;
                }

                string strAppAction;
                string strSocketAction;
                string strResult = "";

                KonekeSocket ks = new KonekeSocket();
                string authcode = ks.getAuthCode();

                string accessToken = await ks.GetAccessToken(authcode);
                this.verboswindows.Text = "accessToken:" + accessToken + "\r\n";

                string userId = await ks.GetUserId(accessToken);
                this.verboswindows.Text += "userID:" + userId + "\r\n";

                GetKList_Result gr = await ks.GetKList(accessToken, userId);
                if (!gr.successful)
                {
                    this.verboswindows.Text += "GetKList error:" + gr.strErrorMessage + "\r\n";
                }

                // What did the user say, for named phrase topic or list "slots"? e.g. "卧室插座"
                string socketName = "";
                NavigationContext.QueryString.TryGetValue("dictatedSearchTerms", out socketName);
                this.verboswindows.Text += "Send voice command for socket:'" + socketName + "'\r\n";

                switch (voiceCommandName)
                {
                    case "TurnOff_Light":
                    case "TurnOn_Light":
                        {
                            if (voiceCommandName == "TurnOff_Light")
                            {
                                strSocketAction = "close";   // keyword for 3rd REST API parameter
                                strAppAction    = "Closing"; // for showing debug verbose on UI,nothing else
                            }
                            else
                            {
                                strSocketAction = "open";
                                strAppAction    = "Opening";
                            }                            

                            //
                            // User wants to turn all sockets' light or just one of them?
                            //
                            if (socketName.Contains("所有") || socketName.Contains("全部"))
                            {
                                for (int i = 0; i < gr.kIDs.Count; i++)
                                {
                                    this.verboswindows.Text += strAppAction + " " + gr.kIDs[i].device_name + "'s light\r\n";
                                    strResult = await ks.SwitchKLight(accessToken, userId, gr.kIDs[i].kid, gr.kIDs[i].device_name, strSocketAction);
                                    this.verboswindows.Text += strResult + "\r\n";
                                }
                            }
                            else
                            {
                                int i;
                                bool getone = false;

                                // Find which one socket light user wants to open/close
                                for (i = 0; i < gr.kIDs.Count; i++)
                                {
                                    if (gr.kIDs[i].device_name.Contains(socketName))
                                    {
                                        getone = true;
                                        break;
                                    }
                                }

                                if (getone)
                                {
                                    this.verboswindows.Text += strAppAction + " " + gr.kIDs[i].device_name + "'s light\r\n";
                                    strResult = await ks.SwitchKLight(accessToken, userId, gr.kIDs[i].kid, gr.kIDs[i].device_name, strSocketAction);
                                    this.verboswindows.Text += strResult + "\r\n";
                                }
                            }
                        }
                        break;
                    default:
                        this.verboswindows.Text += "Unknown voice command\r\n";
                        break;
                }               

            }
        }
        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}