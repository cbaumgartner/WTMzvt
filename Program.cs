//--------------------
// Programm zum Ansteuern der LUIS-ePayment.dll von LUTZ GmbH
// Getestet für das Gerät VeriFone H5000
// Date: 18.7.2017
// Author: Romana Pollak
// Version 2: Anmeldung erfolgt nur bei ersten Aufruf. Abmelden manuell
//--------------------

using System;
using System.Globalization;
using System.Windows.Forms;
//using System.Threading.Tasks;

namespace WtmZvt
{
    
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            
            if (args.Length == 0)
            {
                MessageBox.Show("Kein Betrag mitgeschickt!!!");
                MessageBox.Show("Aufruf: 'ZVT.exe' 'Betrag in Cent' '[optional Pfad und Configdatei] - default: zvtLANH5000.cfg'");
                return;
            }

            if (!int.TryParse(args[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount))
            {
                MessageBox.Show($"{args[0]} ist kein gültiger Betrag!");
                return;
            }

            switch (args.Length)
            {
                case 2:
                    Application.Run(new Statusmeldung(amount, args[1]));
                    break;
                case 3:
                    Application.Run(new Statusmeldung(amount, args[1], args[2]));
                    break;
                default:
                    Application.Run(new Statusmeldung(amount));
                    break;
            }
        }
    }
}