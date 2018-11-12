using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.IO;
using Microsoft.Win32;

namespace EPEK
{
  class Program
  {
    [STAThread]
    static void Main(string[] args)
    {
      try
      {
        using (Application app = Application.CreateApplication(null, null))
        {
          Execute(app);
        }
      }
      catch (Exception e)
      {
        Console.Error.WriteLine(e.ToString());
      }
    }
    static void Execute(Application app)
    {
        Patient patient = app.OpenPatientById("$20111107");
        Course course = patient.Courses.Where(c => c.Id == "1").Single();
        PlanSetup plan = course.PlanSetups.Where(p => p.Id == "PROS").Single();

        QAProtocol rtQAProtocol = new QAProtocol();

        Stream myStream = null;
        OpenFileDialog openFileDialog1 = new OpenFileDialog();

        string RelativePath = "\\QAProtocols\\";
        openFileDialog1.InitialDirectory =
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + RelativePath;
        openFileDialog1.Filter = "RT Protocol files (*.protocol)|*.protocol|All files (*.*)|*.*";
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
                        rtQAProtocol.ReadinProtocol(myStream);
                    }
                    myStream.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
            }
        }

        rtQAProtocol.ApplyToPlan(plan);

        rtQAProtocol.DisplayResults();

        return;
    }
  }
}
