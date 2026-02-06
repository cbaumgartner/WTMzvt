//--------------------
// Programm zum Ansteuern der LUIS-ePayment.dll von LUTZ GmbH
// Getestet für das Gerät VeriFone H5000
// Date: 18.7.2017
// Author: Romana Pollak
//--------------------

using de.luis.kioskComponents.ePayment;
using de.luis.kioskComponents.ePayment.exception;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Windows.Forms;

namespace WtmZvt
{
    public partial class Statusmeldung : Form
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("ZVTLogger");

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
            _logger.Debug("Startup ZVT");
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
                        _logger.Info("Funktionstyp.OFF");
                        OFFline();
                        break;

                    case Funktionstyp.ONL:
                        //lbl_Status.Text = "FUNKTION ONLINE!!!";
                        _logger.Info("Funktionstyp.ON");
                        ONLine();
                        break;

                    case Funktionstyp.OUT:
                        //lbl_Status.Text = "FUNKTION LOGOUT!!!";
                        _logger.Info("Funktionstyp.OUT");
                        LogOUT();
                        lbl_Status.Text = "manuelles Logout!";
                        btn_OK.Enabled = true;
                        break;

                    case Funktionstyp.STO:
                        //lbl_Status.Text = "FUNKTION STORNO!!!";
                        _logger.Info("Funktionstyp.STO");
                        STOrno();
                        break;

                    case Funktionstyp.ABR:
                        //lbl_Status.Text = "FUNKTION ABRECHNUNG!!!";
                        _logger.Info("Funktionstyp.ABR");
                        ABRechnung();
                        break;



                    default:
                        lbl_Status.Text = "FUNKTION DEFAULT!!!";
                        break;
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                Application.Exit();
            }
            _logger.Debug("ZVT loaded");
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
                     _logger.Debug("Start Offline");
                     //--------------
                     // create/read configuration from a configuration file
                     PayConfiguration config = createConfigFromFile(_configPath);
                     _logger.Debug("Config loaded.");

                     // start a new session
                     PaySession session = new PaySession();
                     _logger.Debug("Session created.");

                     // we define a message listener for events (optional)
                     WtmMessageListener msgList = new WtmMessageListener(lbl_Status, btn_OK);
                     _logger.Debug("MyMessageListener created.");

                     session.Listener = msgList;
                     _logger.Debug("Listen to Session.");

                     // login (this is always the first communication to the EFT)
                     _terminal = session.login(config);
                     _logger.Debug("Logged on to Session");

                     try
                     {
                         // Now we start a payment of 1 cent

                         // First we create the result object PayMedia
                         PayMedia media = new PayMedia();
                         _logger.Debug("PayMedia created.");
                         // Then we start the authorisation of the card

                         PayTransaction transaction = CreatePayTransaction(media);

                         // When we are here, the given card was accepted. We commit the transaction.
                         // If transaction is null, the device doesn't support commit and we are finished.
                         if (transaction != null)
                         {
                             _logger.Debug("Commit transaction");
                             transaction.commit(media);
                             _logger.Debug("Transaction committed.");
                         }
                     }
                     finally
                     {
                         PayResult result = new PayResult();

                         _logger.Debug("GetCustomerReceipt");
                         string customerReceipt = result.CustomerReceipt;
                         _logger.Info($"Customer receipt: {customerReceipt}");

                         // logout at last
                         session.logout();
                         _logger.Debug("Logout from Session");

                         _terminal.logout();
                         _logger.Debug("Logout from Terminal");
                     }
                 }
                 catch (PayException ex)
                 {
                     // catch all PayExceptions and write to console
                     _logger.Error(ex);
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
                    WtmMessageListener msgList = new WtmMessageListener(lbl_Status, btn_OK);

                    session.Listener = msgList;

                    if (!session.LoggedIn)
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

                        PayTransaction transaction = CreatePayTransaction(media);

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
                    _logger.Error(ex);
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
                    WtmMessageListener msgList = new WtmMessageListener(lbl_Status, btn_OK);

                    session.Listener = msgList;

                    // login (this is always the first communication to the EFT)
                    _terminal = session.login(config);

                    // logout at last
                    session.logout();

                }
                catch (PayException ex)
                {
                    // catch all PayExceptions and write to console
                    _logger.Error(ex);
                }
            });
            t.Start();
        }

        private void STOrno() //wird noch nicht verwendet
        {

            //NOCH ZU TESTEN!!!
            //terminal.reversal(_amount, payType, media); //Storno
        }

        private PayTransaction CreatePayTransaction(PayMedia media)
        {
            short payType = 0; //0 = alle Zahlarten zulassen
            _logger.Debug($"Start transaction with terminal: [{_amount}], [{payType}], [{media}]");
            PayTransaction transaction = _terminal.payment(_amount, payType, 30, new ProductCategory[] { }, media);
            _logger.Debug($"Transaction created [IsOpen: {transaction?.Open}; IsCommitted: {transaction?.Committed}].");

            return transaction;
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
                    WtmMessageListener msgList = new WtmMessageListener(lbl_Status, btn_OK);

                    session.Listener = msgList;

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
                    _logger.Error(ex);
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

            _logger.Debug("Close ZVT");
        }
    }

    //////////////////////////////////////
    //Listenerklasse für Anzeige der Statusmeldung
    //////////////////////////////////



    class WtmMessageListener : PayMessageListener
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger(nameof(WtmMessageListener));

        private Label lbl_Status;
        private Button btn_OK;

        public string IntermediateMessage
        {
            set
            {
                _logger.Info("+++++intermediate message+++++");
                _logger.Info(value);
                _logger.Info("-----intermediate message-----");
            }
        }
        public string FinalMessage
        {
            set
            {
                _logger.Info("+++++final message+++++");
                _logger.Info(value);
                _logger.Info("-----final message-----");
            }
        }

        public WtmMessageListener(Label lbl, Button OK)
        {
            lbl_Status = lbl;
            btn_OK = OK;
        }

        public void setReceiptMessage(string message, short receiptType)
        {
            switch (receiptType)
            {
                case (short)ReceiptType.CUSTOMER:
                    _logger.Info("+++++customer receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----customer receipt-----");
                    break;
                case (short)ReceiptType.MERCHANT:
                    _logger.Info("+++++merchant receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----merchant receipt-----");
                    break;
                case (short)ReceiptType.END_OF_DAY:
                    _logger.Info("+++++end of day receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----end of day receipt-----");
                    break;
                case (short)ReceiptType.JOURNAL:
                    _logger.Info("+++++journal receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----journal receipt-----");
                    break;
                case (short)ReceiptType.LAST:
                    _logger.Info("+++++last receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----last receipt-----");
                    break;
                case (short)ReceiptType.RECONCILIATION:
                    _logger.Info("+++++reconciliation receipt+++++");
                    _logger.Info(message);
                    _logger.Info("-----reconciliation receipt-----");
                    break;
            }
        }

        public void setDisplayMessage(string message, int code)
        {
            _logger.Info("+++++display message (" + code + ")+++++");
            _logger.Info(message);
            _logger.Info("-----display message (" + code + ")-----");

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
