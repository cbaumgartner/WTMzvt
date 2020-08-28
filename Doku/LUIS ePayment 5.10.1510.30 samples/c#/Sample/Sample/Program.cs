using System;
using System.Collections.Generic;
using System.Text;


using de.luis.kioskComponents.ePayment;
using de.luis.kioskComponents.ePayment.exception;


namespace Sample
{
   class Program
   {

      static PayConfiguration createConfigFromFile(String filename)
      {
         // create/read configuration from a configuration file
         PayConfiguration config = new PayConfiguration(filename);
         return config;
      }


      static PayConfiguration createConfigManually()
      {
         // create/read configuration manually (without a file)
         PayConfiguration config = new PayConfiguration();

         // set all mandatory fields
         config.setDevice           ("H5000");
         config.setDeviceConnector  ("LAN");
         config.setDeviceProtocol   ("ZVT");
         config.setTerminalID       ("12345678");
         config.setMerchantPIN      ("000000");   // Verifone: 000000, Ingenico: 00000, CCV: 000000
         config.setServicePIN       ("111111");   // Verifone: 111111, Ingenico: 11599, CCV: 003563
         config.setLicenseFile      ("../../etc/LUIS_ePaymentLicense.dat");

         // mandatory, if networkConnection=ISDN or =ISDNviaSerial
         //config.setNetworkConnection("ISDN");
         //config.setPhoneConnection(datex1, phone1, datex2, phone2, prefix);

         // mandatory, if networkConnection=LAN
         config.setNetworkConnection("LAN");
         config.setLANConnection    ("213.095.152.80", "5000", "217.111.131.50", "5000"); //cardTech-1: 213.095.152.80:5000, cardTech-2: 217.111.131.50:5000
         config.setLanIPLocal       ("192.168.1.99");
         config.setLanPortLocal     (22000);    // Verifone: 22000, Ingenico: 5577
         config.setLanGateway       ("192.168.1.1");

         // set log4j config file (optional, default = "./LUIS_ePayment.xml")
         config.setLogfile          ("../../etc/LUIS_ePayment.xml");

         return config;
      }



      static void Main(string[] args)
      {
         try
         {
            // create/read configuration from a configuration file
            PayConfiguration config = createConfigFromFile("../../etc/zvt.cfg");

            // start a new session
            PaySession session = new PaySession();

            // we define a message listener for events (optional)
            session.setListener(new MyMessageListener());

            // get the software version of LUIS ePayment
            Console.WriteLine("Version=" + session.getVersion());

            // login (this is always the first communication to the EFT)
            PayTerminal terminal = session.login(config);

            try
            {

               // let's have a look at the settings within the EFT
               PayResult result = terminal.settings();
               Console.WriteLine(result.toString());

               // do some selftest
               result = terminal.selftest();
               Console.WriteLine(result.toString());


               // try to connect the network provider and get the limits for a payment
               result = terminal.diagnose(PayTerminal.__Fields.DIAGNOSE_EXTENDED);
               Console.WriteLine(result.toString());


               // Now we start a payment of 1 cent
               
               // First we create the result object PayMedia
               PayMedia media = new PayMedia();
               
               // Then we start the authorisation of the card
               int cents = 1;
               short payType = PayTerminal.__Fields.PAY_TYPE_AUTOMATIC;
               PayTransaction transaction = terminal.payment(cents, payType, media);
               Console.WriteLine(media.toString());
               
               // When we are here, the given card was accepted. We commit the transaction.
               // If transaction is null, the device doesn't support commit and we are finished.
               if (transaction!=null)
               {
                  transaction.commit(media);
                  Console.WriteLine(media.toString());
               }
               
            }
            finally
            {
               // logout at last
               session.logout();
            }

         }
         catch (PayException x)
         {
            // catch all PayExceptions and write to console
            Console.WriteLine(x.toString());
         }
      }
   }






   //
   // Class to receive events asynchronously
   //
   class MyMessageListener : PayMessageListener
   {

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
            case PayTerminal.__Fields.RECEIPT_TYPE_CUSTOMER:      Console.WriteLine("+++++customer receipt+++++");
                                                                  Console.WriteLine(message);
                                                                  Console.WriteLine("-----customer receipt-----");
                                                                  break;
            case PayTerminal.__Fields.RECEIPT_TYPE_MERCHANT:      Console.WriteLine("+++++merchant receipt+++++");
                                                                  Console.WriteLine(message);
                                                                  Console.WriteLine("-----merchant receipt-----");
                                                                  break;
            case PayTerminal.__Fields.RECEIPT_TYPE_ADMINISTRATOR: Console.WriteLine("+++++administrator receipt+++++");
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
      }

   }

}
