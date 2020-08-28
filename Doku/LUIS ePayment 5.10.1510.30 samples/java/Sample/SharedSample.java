import de.luis.kioskComponents.ePayment.PayConfiguration;
import de.luis.kioskComponents.ePayment.PayMedia;
import de.luis.kioskComponents.ePayment.PayResult;
import de.luis.kioskComponents.ePayment.PaySharedSession;
import de.luis.kioskComponents.ePayment.PayTerminal;
import de.luis.kioskComponents.ePayment.PayTransaction;
import de.luis.kioskComponents.ePayment.exception.ConfigException;
import de.luis.kioskComponents.ePayment.exception.PayException;


public class SharedSample
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
      config.setSharedTerminalIP ("192.168.1.100");

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

         // the tid of the shared EFT device
         String terminalID = "123456";
         
         // start a new session
         // Use a PaySharedSession instead of a PaySession instance in LUIS ePayment SharedTerminalServer environment
         PaySharedSession session = new PaySharedSession();

         // we define a message listener for events (optional)
         session.setListener(new MyMessageListener());

         // get the software version of LUIS ePayment
         System.out.println("Version=" + session.getVersion());

         // login (this is always the first communication to the EFT)
         // Use the additional terminalID parameter in a LUIS ePayment SharedTerminalServer environment
         PayTerminal terminal = session.login(config, terminalID);

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
