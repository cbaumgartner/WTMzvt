import de.luis.kioskComponents.ePayment.PayTerminal;
import de.luis.kioskComponents.ePayment.PayMessageListener;


public class MyMessageListener implements PayMessageListener
{
   public void setIntermediateMessage(String message)
   {
      System.out.println("+++++intermediate message+++++");
      System.out.println(message);
      System.out.println("-----intermediate message-----");
   }

   public void setFinalMessage(String message)
   {
      System.out.println("+++++final message+++++");
      System.out.println(message);
      System.out.println("-----final message-----");
   }

   public void setReceiptMessage(String message, short receiptType)
   {
      switch (receiptType)
      {
         case PayTerminal.RECEIPT_TYPE_CUSTOMER :
            System.out.println("+++++customer receipt+++++");
            System.out.println(message);
            System.out.println("-----customer receipt-----");
            break;
         case PayTerminal.RECEIPT_TYPE_MERCHANT :
            System.out.println("+++++merchant receipt+++++");
            System.out.println(message);
            System.out.println("-----merchant receipt-----");
            break;
         case PayTerminal.RECEIPT_TYPE_ADMINISTRATOR :
            System.out.println("+++++administrator receipt+++++");
            System.out.println(message);
            System.out.println("-----administrator receipt-----");
            break;
      }
   }

   public void setDisplayMessage(String message, int code)
   {
      System.out.println("+++++display message (" + code + ")+++++");
      System.out.println(message);
      System.out.println("-----display message (" + code + ")-----");
   }

}
