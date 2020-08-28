import de.luis.kioskComponents.ePayment.PayConfiguration;
import de.luis.kioskComponents.ePayment.PayMedia;
import de.luis.kioskComponents.ePayment.PayResult;
import de.luis.kioskComponents.ePayment.PaySession;
import de.luis.kioskComponents.ePayment.PayTerminal;
import de.luis.kioskComponents.ePayment.PayTransaction;
import de.luis.kioskComponents.ePayment.exception.ConfigException;
import de.luis.kioskComponents.ePayment.exception.PayException;


public class Sample
{
   private static PayConfiguration createConfigFromFile(String filename) throws ConfigException
   {
      // create/read configuration from a configuration file
      PayConfiguration config = new PayConfiguration(filename);
      return config;
   }
   
   
   private static PayConfiguration createConfigManually() throws ConfigException
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
      config.setLANConnection    ("117.111.131.51", "5000", "117.111.131.51", "5000"); // Verifone: 50, Ingenico: 52, CCV: 51
      config.setLanIPLocal       ("192.168.32.99");
      config.setLanPortLocal     (22000);    // Verifone: 22000, Ingenico: 5577
      config.setLanGateway       ("192.168.1.1");
   
      // set log4j config file (optional, default = "./LUIS_ePayment.xml")
      config.setLogfile          ("../../etc/LUIS_ePayment.xml");
   
      return config;
   }
   

   public static void main(String[] args)
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
         System.out.println("Version=" + session.getVersion());

         // login (this is always the first communication to the EFT)
         PayTerminal terminal = session.login(config);

         try
         {

            // let's have a look at the settings within the EFT
            PayResult result = terminal.settings();
            System.out.println(result.toString());

            // do some selftest
            result = terminal.selftest();
            System.out.println(result.toString());

            // try to connect the network provider and get the limits for a payment
            result = terminal.diagnose(PayTerminal.DIAGNOSE_EXTENDED);
            System.out.println(result.toString());


            // Now we start a payment of 1 cent
            
            // First we create the result object PayMedia
            PayMedia media = new PayMedia();
            
            // Then we start the authorisation of the card
            int cents = 1;
            short payType = PayTerminal.PAY_TYPE_AUTOMATIC;
            PayTransaction transaction = terminal.payment(cents, payType, media);
            System.out.println(media.toString());
            
            // When we are here, the given card was accepted. We commit the transaction.
            // If transaction is null, the device doesn't support commit and we are finished.
            if (transaction!=null)
            {
               transaction.commit(media);
               System.out.println(media.toString());
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
         System.out.println(x.toString());
      }

   }

}
