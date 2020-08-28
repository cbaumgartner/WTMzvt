//--------------------
// Programm zum Ansteuern der LUIS-ePayment.dll von LUTZ GmbH
// Getestet für das Gerät VeriFone H5000
// Date: 18.7.2017
// Author: Romana Pollak
// Version 2: Anmeldung erfolgt nur bei ersten Aufruf. Abmelden manuell
//--------------------

using System;
using System.Collections.Generic;
using System.Linq;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZVT
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
            }
            else if ((Convert.ToInt32(args[0])) < 0) //für Stornofunktion ändern!!!
            {
                MessageBox.Show("Kein negativer Betrag möglich!!!");
            }
            else if (args.Length == 2) //Pfad mit angegeben
            {
                Application.Run(new Statusmeldung(Convert.ToInt32(args[0]), args[1]));
            }
            else if (args.Length == 3) //Pfad mit angegeben
            {
                Application.Run(new Statusmeldung(Convert.ToInt32(args[0]), args[1], args[2]));
            }
            else
            {
                Application.Run(new Statusmeldung(Convert.ToInt32(args[0])));
            }
        }
    }
}