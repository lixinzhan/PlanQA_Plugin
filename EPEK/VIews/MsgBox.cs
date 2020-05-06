//
// MessageBox like display window.
//

namespace EPEK.Views
{
    using System.Windows;

    public static class MsgBox
    {
        public static void Show(string mymessage)
        {
            MsgBoxWindow msgBox = new MsgBoxWindow(mymessage);
            msgBox.ShowDialog();
        }

        public static void Show(string mymessage, string mycaption)
        {
            MsgBoxWindow msgBox = new MsgBoxWindow(mymessage, mycaption);
            msgBox.ShowDialog();
        }
    }
}
