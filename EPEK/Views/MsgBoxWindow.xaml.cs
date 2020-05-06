using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;


namespace EPEK.Views
{
    public enum FontFormat { Normal = 0, Bold, Italic, Red, Yellow, Blue };

    /// <summary>
    /// Interaction logic for DisplayWindow.xaml
    /// </summary>
    public partial class MsgBoxWindow : Window
    {
        internal string Caption
        {
            get
            {
                return Title;
            }
            set
            {
                Title = value;
            }
        }

        internal MsgBoxWindow(string mymessage, string mycaption="TextBlock")
        {
            InitializeComponent();
            Caption = mycaption;
            TextBlock tb = new TextBlock();

            tb.TextWrapping = TextWrapping.Wrap;
            tb.Margin = new Thickness(30, 20, 10, 10);
            tb.Background = new SolidColorBrush(Colors.WhiteSmoke);
            tb.Height = Double.NaN;
            tb.Width = Double.NaN;
            tb.TextWrapping = TextWrapping.Wrap;

            FontFormat textDec = FontFormat.Normal;

            string pattern = "#";
            string[] messageSplitted = Regex.Split(mymessage, pattern, RegexOptions.IgnoreCase);

            // Fo
            for (int i=0; i<messageSplitted.Length; i++)
            {
                if (messageSplitted[i].ToLower() == "bold") { textDec = FontFormat.Bold; continue; }
                else if (messageSplitted[i].ToLower() == "italic") { textDec = FontFormat.Italic; continue; }
                else if (messageSplitted[i].ToLower() == "red") { textDec = FontFormat.Red; continue; }
                else if (messageSplitted[i].ToLower() == "yellow") { textDec = FontFormat.Yellow; continue; }
                else if (messageSplitted[i].ToLower() == "blue") { textDec = FontFormat.Blue; continue; }
                else if (messageSplitted[i].ToLower() == "normal") { textDec = FontFormat.Normal; continue; }

                if (textDec == FontFormat.Normal) tb.Inlines.Add(messageSplitted[i]);
                else if (textDec == FontFormat.Bold)
                    tb.Inlines.Add(new Run(messageSplitted[i]) { FontWeight = FontWeights.Bold });
                else if (textDec == FontFormat.Italic)
                    tb.Inlines.Add(new Run(messageSplitted[i]) { FontStyle = FontStyles.Italic });
                else if (textDec == FontFormat.Red)
                    tb.Inlines.Add(new Run(messageSplitted[i]) { Foreground = Brushes.Red });
                else if (textDec == FontFormat.Yellow)
                    tb.Inlines.Add(new Run(messageSplitted[i]) { Foreground = Brushes.Yellow });
                else if (textDec == FontFormat.Blue)
                    tb.Inlines.Add(new Run(messageSplitted[i]) { Foreground = Brushes.Blue });

                textDec = FontFormat.Normal;
                this.Content = tb;
            }
        }
    }
}
