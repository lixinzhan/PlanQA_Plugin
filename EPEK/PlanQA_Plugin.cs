using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Reflection;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using Microsoft.Win32;

using EPEK.Models;

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }

        public void Execute(ScriptContext scriptContext, Window mainWindow)
        {
            Run(scriptContext.CurrentUser,
                scriptContext.Patient,
                scriptContext.Image,
                scriptContext.StructureSet,
                scriptContext.PlanSetup,
                scriptContext.PlansInScope,
                scriptContext.PlanSumsInScope,
                mainWindow);
        }

        public void Run(
            User user, 
            Patient patient, 
            Image image, 
            StructureSet structureSet, 
            PlanSetup planSetup,
            IEnumerable<PlanSetup> planSetupInScope, 
            IEnumerable<PlanSum> planSumInScope, 
            Window mainWindow)
        {
            PlanSetup plan = planSetup;
            QAProtocol rtQAProtocol = new QAProtocol();

            Stream myStream = null;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // Locate protocols in the subfolder of running DLL directory.
            string RelativePath = "\\QAProtocols\\";
            openFileDialog1.InitialDirectory =
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + RelativePath;
            openFileDialog1.Filter =
                "RT Protocol files (*.protocol)|*.protocol|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == true)
            {
                try
                {
                    if ((myStream = openFileDialog1.OpenFile()) != null)
                    {
                        using (myStream)
                        {
                            // reading in and parsing the protocol file
                            rtQAProtocol.ReadinProtocol(myStream);
                        }
                        myStream.Close();

                        // Plan check and display results
                        rtQAProtocol.ApplyToPlan(plan);
                        rtQAProtocol.DisplayResults();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: "
                        + ex.Message);
                }
            }
            return;
        }
    }
}
