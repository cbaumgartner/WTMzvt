//--------------------
// Programm zum Ansteuern der LUIS-ePayment.dll von LUTZ GmbH
// Getestet für das Gerät VeriFone H5000
// Date: 18.7.2017
// Author: Romana Pollak
//--------------------

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using de.luis.kioskComponents.ePayment;
using de.luis.kioskComponents.ePayment.exception;

namespace WtmZvt
{
    public partial class Statusmeldung : Form
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //OFF = Offline     -> Login, Betrag, Logout                -> Terminal wieder offline
        //ONL = Online      -> Login (falls offline), Betrag        -> Terminal betriebsbereit, manuelles ausloggen
        //OUT = Ausloggen   -> nur Ausloggen                        -> Terminal wieder offline
        //STO = Strono      -> Login (falls offline), StornoBetrag  -> Für Stornieren von Belegen VOR Kassaschnitt
        //ABR = Abrechnung  -> Kassaschnitt                         -> stoßt nur Kassaabrechnung am Terminal an

        public enum Funktionstyp
        {
            OFF, ONL, OUT, STO, ABR
        };

        private int _amount;
        private string _configPath;
        private Funktionstyp _operation;
        private PayTerminal _terminal;

        //Einloggen, Ausloggen, Betrag_Übermitteln, Betrag_Stornieren, Kassenabschluss

        public Statusmeldung(int amount)
        {
            InitializeComponent();
            _amount = amount;
            _configPath = "zvtLANH5000.cfg";
            _operation = 0; //Default Offline
        }

        public Statusmeldung(int amount, string configPath, string operation = "OFF") //Default Offline
        {
            InitializeComponent();
            _amount = amount;
            _configPath = configPath;
            Enum.TryParse(operation, out _operation);
        }

        private void Statusmeldung_Load(object sender, EventArgs e)
        {
            Logger.Debug("Startup ZVT");
            try
            {
                if ((ModifierKeys & Keys.Shift) == 0)
                {
                    string initLocation = Properties.Settings.Default.InitialLocation;
                    Point pInitLocation = new Point(0, 0);
                    Size sInitSize = Size;
                    if (!string.IsNullOrEmpty(initLocation))
                    {
                        string[] parts = initLocation.Split(',');
                        if (parts.Length >= 2)
                        {
                            pInitLocation = new Point(int.Parse(parts[0]), int.Parse(parts[1]));
                        }
                        if (parts.Length >= 4)
                        {
                            sInitSize = new Size(int.Parse(parts[2]), int.Parse(parts[3]));
                        }
                    }

                    Size = sInitSize;
                    Location = pInitLocation;
                }

                lbl_Status.Text = "Verbindung zum EC-Terminal wird aufgebaut...";
                btn_OK.Enabled = false;
                switch (_operation)
                {
                    case Funktionstyp.OFF:
                        //lbl_Status.Text = "FUNKTION OFFLINE!!!";
                        Logger.Info("Funktionstyp.OFF");
                        OFFline();
                        break;

                    case Funktionstyp.ONL:
                        //lbl_Status.Text = "FUNKTION ONLINE!!!";
                        Logger.Info("Funktionstyp.ON");
                        ONLine();
                        break;

                    case Funktionstyp.OUT:
                        //lbl_Status.Text = "FUNKTION LOGOUT!!!";
                        Logger.Info("Funktionstyp.OUT");
                        LogOUT();
                        lbl_Status.Text = "manuelles Logout!";
                        btn_OK.Enabled = true;
                        break;

                    case Funktionstyp.STO:
                        //lbl_Status.Text = "FUNKTION STORNO!!!";
                        Logger.Info("Funktionstyp.STO");
                        STOrno();
                        break;

                    case Funktionstyp.ABR:
                        //lbl_Status.Text = "FUNKTION ABRECHNUNG!!!";
                        Logger.Info("Funktionstyp.ABR");
                        ABRechnung();
                        break;



                    default:
                        lbl_Status.Text = "FUNKTION DEFAULT!!!";
                        break;
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Application.Exit();
            }
            Logger.Debug("ZVT loaded");
        }

        static PayConfiguration createConfigFromFile(String filename)
        {
            if (!File.Exists(filename))
            {
                MessageBox.Show("Config-Datei fehlt oder falscher Pfad!");
                throw new Exception("Config-Datei fehlt oder falscher Pfad!");
            }
            
            // create/read configuration from a configuration file
            PayConfiguration config = new PayConfiguration(filename);
            return config;
        }


        /////////////////////////////////////////////
        // Funktionen 

        //Einloggen, Zahlung abwickeln, ausloggen -> Funktionalität wie bei Penz
        private void OFFline()
        {
            Thread t = new Thread(() =>
             {
                 try
                 {
                     Logger.Debug("Start Offline");
                     //--------------
                     // create/read configuration from a configuration file
                     PayConfiguration config = createConfigFromFile(_configPath);
                     Logger.Debug("Config loaded.");
                     
                     // start a new session
                     PaySession session = new PaySession();
                     Logger.Debug("Session created.");

                     // we define a message listener for events (optional)
                     MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);
                     Logger.Debug("MyMessageListener created.");

                     session.setListener(msgList);
                     Logger.Debug("Listen to Session.");

                     // login (this is always the first communication to the EFT)
                     _terminal = session.login(config);
                     Logger.Debug("Logged on to Session");

                     try
                     {
                         // Now we start a payment of 1 cent

                         // First we create the result object PayMedia
                         PayMedia media = new PayMedia();
                         Logger.Debug("PayMedia created.");
                         // Then we start the authorisation of the card

                         short payType = PayTerminal.__Fields.PAY_TYPE_AUTOMATIC;
                         Logger.Debug($"Start transaction with terminal: [{_amount}], [{payType}], [{media}]");
                         PayTransaction transaction = _terminal.payment(_amount, payType, media);
                         Logger.Debug("Transaction created.");


                         // When we are here, the given card was accepted. We commit the transaction.
                         // If transaction is null, the device doesn't support commit and we are finished.
                         if (transaction != null)
                         {
                             Logger.Debug("Commit transaction");
                             transaction.commit(media);
                             Logger.Debug("Transaction committed.");
                         }
                     }
                     finally
                     {
                         PayResult result = new PayResult();
                         
                         Logger.Debug("GetCustomerReceipt");
                         string Custreceitpt = result.getCustomerReceipt();

                         // logout at last
                         session.logout();
                         Logger.Debug("Logout from Session");
                     }
                 }
                 catch (PayException ex)
                 {
                     // catch all PayExceptions and write to console
                     Logger.Error(ex);
                 }
             });
            t.Start();
        }

        private void ONLine()
        {
            Thread t = new Thread(() =>
            {
                try
                {

                    //--------------
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(_configPath);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    if (!session.isLoggedIn())
                    {
                        // login (this is always the first communication to the EFT)
                        _terminal = session.login(config);
                    }


                    try
                    {
                        // Now we start a payment of 1 cent

                        // First we create the result object PayMedia
                        PayMedia media = new PayMedia();

                        // Then we start the authorisation of the card

                        short payType = PayTerminal.__Fields.PAY_TYPE_AUTOMATIC;
                        PayTransaction transaction = _terminal.payment(_amount, payType, media);

                        // When we are here, the given card was accepted. We commit the transaction.
                        // If transaction is null, the device doesn't support commit and we are finished.
                        if (transaction != null)
                        {
                            transaction.commit(media);
                        }
                    }
                    finally
                    {

                    }
                }
                catch (PayException ex)
                {
                    // catch all PayExceptions and write to console
                    Logger.Error(ex);
                }
            });
            t.Start();
        }

        private void LogOUT()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(_configPath);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    // login (this is always the first communication to the EFT)
                    _terminal = session.login(config);

                    // logout at last
                    session.logout();

                }
                catch (PayException ex)
                {
                    // catch all PayExceptions and write to console
                    Logger.Error(ex);
                }
            });
            t.Start();
        }

        private void STOrno() //wird noch nicht verwendet
        {

            //NOCH ZU TESTEN!!!
            //terminal.reversal(_amount, payType, media); //Storno
        }
        private void ABRechnung()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(_configPath);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    // login (this is always the first communication to the EFT)
                    _terminal = session.login(config);

                    //NOCH ZU TESTEN!!!

                    _terminal.reconciliation(); //Tageslosung/Kasseschnitt

                    // logout at last
                    session.logout();
                }
                catch (PayException ex)
                {
                    // catch all PayExceptions and write to console
                    Logger.Error(ex);
                }
            });
            t.Start();
        }


        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Statusmeldung_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((ModifierKeys & Keys.Shift) == 0)
            {
                Point location = this.Location;
                Size size = this.Size;

                if (this.WindowState != FormWindowState.Normal)
                {
                    location = this.RestoreBounds.Location;
                    size = this.RestoreBounds.Size;
                }

                string initLocation = string.Join(",", location.X, location.Y, size.Width, size.Height);
                Properties.Settings.Default.InitialLocation = initLocation;
                Properties.Settings.Default.Save();
            }

            Logger.Debug("Close ZVT");
        }
    }

    //////////////////////////////////////
    //Listenerklasse für Anzeige der Statusmeldung
    //////////////////////////////////



    class MyMessageListener : PayMessageListener
    {
        private Label lbl_Status;
        private Button btn_OK;

        public MyMessageListener(Label _lbl, Button _OK)
        {
            lbl_Status = _lbl;
            btn_OK = _OK;
        }

        public void setIntermediateMessage(String message)
        {
            Console.WriteLine("+++++intermediate message+++++");
            Console.WriteLine(message);
            Console.WriteLine("-----intermediate message-----");
        }

        public void setFinalMessage(String message)
        {
            Console.WriteLine("+++++final message+++++");
            Console.WriteLine(message);
            Console.WriteLine("-----final message-----");
        }

        public void setReceiptMessage(String message, short receiptType)
        {
            switch (receiptType)
            {
                case PayTerminal.__Fields.RECEIPT_TYPE_CUSTOMER:
                    Console.WriteLine("+++++customer receipt+++++");
                    Console.WriteLine(message);
                    Console.WriteLine("-----customer receipt-----");
                    break;
                case PayTerminal.__Fields.RECEIPT_TYPE_MERCHANT:
                    Console.WriteLine("+++++merchant receipt+++++");
                    Console.WriteLine(message);
                    Console.WriteLine("-----merchant receipt-----");
                    break;
                case PayTerminal.__Fields.RECEIPT_TYPE_ADMINISTRATOR:
                    Console.WriteLine("+++++administrator receipt+++++");
                    Console.WriteLine(message);
                    Console.WriteLine("-----administrator receipt-----");
                    break;
            }
        }

        public void setDisplayMessage(String message, int code)
        {
            Console.WriteLine("+++++display message (" + code + ")+++++");
            Console.WriteLine(message);
            Console.WriteLine("-----display message (" + code + ")-----");

            //Zahlung erfolgreich
            if (message == "Zahlung erfolgt " || message == "Kassenschnitt ")
            {
                btn_OK.Invoke((MethodInvoker)(() =>
                {
                    btn_OK.Enabled = true;
                }));

                lbl_Status.Invoke((MethodInvoker)(() =>
                {
                    lbl_Status.BackColor = Color.Green;
                }));

            }
            //Zahlung nicht erfolgreich
            else if (message == "Vorgang nicht möglich " || message == "Vorgang abgebrochen " || message == "Keine Genehmigung ")
            {
                btn_OK.Invoke((MethodInvoker)(() =>
                {
                    btn_OK.Enabled = true;
                }));

                lbl_Status.Invoke((MethodInvoker)(() =>
                {
                    lbl_Status.BackColor = Color.Red;
                }));
            }

            lbl_Status.Invoke((MethodInvoker)(() =>
            {
                lbl_Status.Text = message;
            }));
        }
    }
}
