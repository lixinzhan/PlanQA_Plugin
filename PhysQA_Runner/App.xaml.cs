﻿using EsapiEssentials.PluginRunner;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS;
using VMS.TPS.Common.Model.API;

namespace PhysQA_Runner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            ScriptRunner.Run(new Script());
        }
        public void DoNothing(PlanSetup plan) { }
    }
}
