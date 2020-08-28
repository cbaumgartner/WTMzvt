//--------------------
// Programm zum Ansteuern der LUIS-ePayment.dll von LUTZ GmbH
// Getestet für das Gerät VeriFone H5000
// Date: 18.7.2017
// Author: Romana Pollak
//--------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

//using System.Threading.Tasks;
using System.Threading;

using de.luis.kioskComponents.ePayment;
using de.luis.kioskComponents.ePayment.exception;


namespace ZVT
{
    public partial class Statusmeldung : Form
    {
        //OFF = Offline     -> Login, Betrag, Logout                -> Terminal wieder offline
        //ONL = Online      -> Login (falls offline), Betrag        -> Terminal betriebsbereit, manuelles ausloggen
        //OUT = Ausloggen   -> nur Ausloggen                        -> Terminal wieder offline
        //STO = Strono      -> Login (falls offline), StornoBetrag  -> Für Stornieren von Belegen VOR Kassaschnitt
        //ABR = Abrechnung  -> Kassaschnitt                         -> stoßt nur Kassaabrechnung am Terminal an

        public enum Funktionstyp
        {
            OFF, ONL, OUT, STO, ABR
        };

        private int mBetrag;
        private string mPfad;
        public Funktionstyp mOperation;
        private PayTerminal mTerminal;

        //Einloggen, Ausloggen, Betrag_Übermitteln, Betrag_Stornieren, Kassenabschluss

        public Statusmeldung(int _betrag)
        {
            InitializeComponent();
            mBetrag = _betrag;
            mPfad = "zvtLANH5000.cfg";
            mOperation = 0; //Default Offline
        }

        public Statusmeldung(int _betrag, string _pfad, string _opteration = "OFF") //Default Offline
        {
            InitializeComponent();
            mBetrag = _betrag;
            mPfad = _pfad;
            Enum.TryParse(_opteration, out mOperation);
        }

        private void Statusmeldung_Load(object sender, EventArgs e)
        {

            lbl_Status.Text = "Verbindung zum EC-Terminal wird aufgebaut...";
            btn_OK.Enabled = false;
            switch (mOperation)
            {
                case Funktionstyp.OFF:
                    //lbl_Status.Text = "FUNKTION OFFLINE!!!";
                    OFFline();
                    break;

                case Funktionstyp.ONL:
                    //lbl_Status.Text = "FUNKTION ONLINE!!!";
                    ONLine();
                    break;

                case Funktionstyp.OUT:
                    //lbl_Status.Text = "FUNKTION LOGOUT!!!";
                    LogOUT();
                    lbl_Status.Text = "manuelles Logout!";
                    btn_OK.Enabled = true;
                    break;

                case Funktionstyp.STO:
                    //lbl_Status.Text = "FUNKTION STORNO!!!";
                    STOrno();
                    break;

                case Funktionstyp.ABR:
                    //lbl_Status.Text = "FUNKTION ABRECHNUNG!!!";
                    ABRechnung();
                    break;



                default:
                    lbl_Status.Text = "FUNKTION DEFAULT!!!";
                    break;
            }
        }

        static PayConfiguration createConfigFromFile(String filename)
        {
            if (File.Exists(filename))
            {
                //für Debug
                //MessageBox.Show("Config-Datei passt");
            }
            else
            {
                MessageBox.Show("Config-Datei fehlt oder falscher Pfad!");
            }

            // create/read configuration from a configuration file
            PayConfiguration config = new PayConfiguration(filename);
            return config;
        }


        /////////////////////////////////////////////
        // Funktionen 

        //Einloggen, Zahlung abwickeln, ausloggen -> Funktionalität wie bei Penz
        private bool OFFline()
        {
            Thread t = new Thread(() =>
             {
                 try
                 {

                     //--------------
                     // create/read configuration from a configuration file
                     PayConfiguration config = createConfigFromFile(mPfad);

                     // start a new session
                     PaySession session = new PaySession();

                     // we define a message listener for events (optional)
                     MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                     session.setListener(msgList);

                     // login (this is always the first communication to the EFT)
                     mTerminal = session.login(config);

                     try
                     {
                         // Now we start a payment of 1 cent

                         // First we create the result object PayMedia
                         PayMedia media = new PayMedia();

                         // Then we start the authorisation of the card

                         short payType = PayTerminal.__Fields.PAY_TYPE_AUTOMATIC;
                         PayTransaction transaction = mTerminal.payment(mBetrag, payType, media);



                         // When we are here, the given card was accepted. We commit the transaction.
                         // If transaction is null, the device doesn't support commit and we are finished.
                         if (transaction != null)
                         {
                             transaction.commit(media);
                         }
                     }
                     finally
                     {
                         PayResult result = new PayResult();
                         
                         string Custreceitpt = result.getCustomerReceipt();

                         // logout at last
                         session.logout();
                     }
                 }
                 catch (PayException x)
                 {
                     // catch all PayExceptions and write to console
                     Console.WriteLine(x.toString());
                 }
             });
            t.Start();
            return true;
        }

        private bool ONLine()
        {
            Thread t = new Thread(() =>
            {
                try
                {

                    //--------------
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(mPfad);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    if (!session.isLoggedIn())
                    {
                        // login (this is always the first communication to the EFT)
                        mTerminal = session.login(config);
                    }


                    try
                    {
                        // Now we start a payment of 1 cent

                        // First we create the result object PayMedia
                        PayMedia media = new PayMedia();

                        // Then we start the authorisation of the card

                        short payType = PayTerminal.__Fields.PAY_TYPE_AUTOMATIC;
                        PayTransaction transaction = mTerminal.payment(mBetrag, payType, media);

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
                catch (PayException x)
                {
                    // catch all PayExceptions and write to console
                    Console.WriteLine(x.toString());
                }
            });
            t.Start();
            return true;
        }

        private bool LogOUT()
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(mPfad);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    // login (this is always the first communication to the EFT)
                    mTerminal = session.login(config);

                    // logout at last
                    session.logout();

                }
                catch (PayException x)
                {
                    // catch all PayExceptions and write to console
                    Console.WriteLine(x.toString());
                }
            });
            t.Start();
            return true;
        }

        private bool STOrno() //wird noch nicht verwendet
        {

            //NOCH ZU TESTEN!!!
            //terminal.reversal(mBetrag, payType, media); //Storno
            return true;
        }
        private bool ABRechnung()
        {

            Thread t = new Thread(() =>
            {
                try
                {
                    // create/read configuration from a configuration file
                    PayConfiguration config = createConfigFromFile(mPfad);

                    // start a new session
                    PaySession session = new PaySession();

                    // we define a message listener for events (optional)
                    MyMessageListener msgList = new MyMessageListener(lbl_Status, btn_OK);

                    session.setListener(msgList);

                    // login (this is always the first communication to the EFT)
                    mTerminal = session.login(config);

                    //NOCH ZU TESTEN!!!

                    mTerminal.reconciliation(); //Tageslosung/Kasseschnitt

                    // logout at last
                    session.logout();
                }
                catch (PayException x)
                {
                    // catch all PayExceptions and write to console
                    Console.WriteLine(x.toString());
                }
            });
            t.Start();
            return true;
        }


        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
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

            //string caption = "Meldung vom EC-Terminal - setDisplayMessage";
            //DialogResult result;

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
