using osuRequestor.Exceptions;

namespace osuRequestor.ExceptionHandler.Exception;

public class NotFoundException : ApiException
{

   public NotFoundException(string message) : base(message)
   {
   }
}