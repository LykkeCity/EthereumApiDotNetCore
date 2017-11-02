using System.Threading.Tasks;

namespace Services.Signature
{
    public interface ITransactionRouter
    {
        Task<string> GetNextSenderAddressAsync();
    }
}