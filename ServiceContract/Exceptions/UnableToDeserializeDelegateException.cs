using System;

namespace N17Solutions.Microphobia.ServiceContract.Exceptions
{
    public class UnableToDeserializeDelegateException : Exception
    {
        private new const string Message = "The enqueued delegate was unable to be deserialized.\n" +
                                       "This usually happens when you've enqueued an unsupported method type.";
                               
        public UnableToDeserializeDelegateException() : base(Message) {}
    }
}