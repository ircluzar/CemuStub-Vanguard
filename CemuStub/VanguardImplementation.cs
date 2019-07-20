﻿using RTCV.NetCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTCV;
using RTCV.CorruptCore;
using static RTCV.NetCore.NetcoreCommands;
using CemuStub;
using RTCV.NetCore.StaticTools;

namespace Vanguard
{
    public static class VanguardImplementation
    {
        public static RTCV.Vanguard.VanguardConnector connector = null;


        public static void StartClient()
        {
            try
            {
                ConsoleEx.WriteLine("Starting Vanguard Client");
                Thread.Sleep(500); //When starting in Multiple Startup Project, the first try will be uncessful since
                                   //the server takes a bit more time to start then the client.

                var spec = new NetCoreReceiver();
                spec.Attached = VanguardCore.attached;
                spec.MessageReceived += OnMessageReceived;

                connector = new RTCV.Vanguard.VanguardConnector(spec);
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();
            }
        }

        public static void RestartClient()
        {
            connector?.Kill();
            connector = null;
            StartClient();
        }

        private static void OnMessageReceived(object sender, NetCoreEventArgs e)
        {
            try
            {
                // This is where you implement interaction.
                // Warning: Any error thrown in here will be caught by NetCore and handled by being displayed in the console.

                var message = e.message;
                var simpleMessage = message as NetCoreSimpleMessage;
                var advancedMessage = message as NetCoreAdvancedMessage;

                ConsoleEx.WriteLine(message.Type);
                switch (message.Type) //Handle received messages here
                {

                    case REMOTE_ALLSPECSSENT:
                        {
                            //We still need to set the emulator's path
                            AllSpec.VanguardSpec.Update(VSPEC.EMUDIR, CemuWatch.currentGameInfo.cemuExeFile.Directory.FullName);
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                CemuWatch.UpdateDomains();
                            });
                        }
                        break;
                    case SAVESAVESTATE:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            e.setReturnValue(VanguardCore.SaveSavestate_NET(advancedMessage.objectValue as string));
                            //e.setReturnValue("");
                        });
                        break;

                    case LOADSAVESTATE:
                        {
                            
                            var cmd = advancedMessage.objectValue as object[];
                            var path = cmd[0] as string;
                            var location = (StashKeySavestateLocation)cmd[1];
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                e.setReturnValue(VanguardCore.LoadSavestate_NET(path, location));
                            });
                            

                            e.setReturnValue(true);
                            break;
                        }

                    case REMOTE_PRECORRUPTACTION:
                        CemuWatch.KillCemuProcess();
                        CemuWatch.RestoreBackup();
                        break;

                    case REMOTE_POSTCORRUPTACTION:
                        {
                            //var fileName = advancedMessage.objectValue as String;
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                CemuWatch.StartRpx();
                            });

                        }
                        break;

                    case REMOTE_CLOSEGAME:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            CemuWatch.KillCemuProcess();
                        });

                        break;

                    case REMOTE_DOMAIN_GETDOMAINS:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            e.setReturnValue(CemuWatch.GetInterfaces());
                        });
                        break;
                    case REMOTE_EVENT_EMU_MAINFORM_CLOSE:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            Environment.Exit(0);
                        });
                        break;
                    case REMOTE_ISNORMALADVANCE:
                        e.setReturnValue(true);
                        //e.setReturnValue(Hooks.isNormalAdvance);
                        break;

                    case REMOTE_EVENT_CLOSEEMULATOR:
                        Environment.Exit(-1);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();
            }
        }

    }
}
