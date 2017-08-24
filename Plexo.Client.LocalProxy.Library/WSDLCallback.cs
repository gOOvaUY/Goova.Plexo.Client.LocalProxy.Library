using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Plexo.Client.SDK.Logging;

namespace Plexo.Client.LocalProxy.Library
{
    public class WSDLCallback : ClientBase<ICallback>, ICallback
    {
        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();
        private WSDLCallback(ServiceEndpoint enp) : base(enp)
        {
        }
        public Task<ClientResponse> Instrument(IntrumentCallback instrument)
        {
            return Channel.Instrument(instrument);
        }

        public Task<ClientResponse> Payment(TransactionCallback transaction)
        {
            return Channel.Payment(transaction);
        }

        public static WSDLCallback Create()
        {

            try
            {
                int timeout = Properties.Settings.Default.Timeout;
                WebHttpBinding binding = new WebHttpBinding();
                binding.OpenTimeout = TimeSpan.FromSeconds(timeout);
                binding.CloseTimeout = TimeSpan.FromSeconds(timeout);
                binding.SendTimeout = TimeSpan.FromSeconds(timeout);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(timeout);
                if (Properties.Settings.Default.CallbackUrl.StartsWith("https"))
                    binding.Security.Mode = WebHttpSecurityMode.Transport;

                ServiceEndpoint svc = new ServiceEndpoint(ContractDescription.GetContract(typeof(ICallback)),
                    binding, new EndpointAddress(Properties.Settings.Default.CallbackUrl));
                WebHttpBehavior behavior = new WebHttpBehavior
                {
                    DefaultBodyStyle = WebMessageBodyStyle.Bare,
                    DefaultOutgoingRequestFormat = WebMessageFormat.Json,
                    DefaultOutgoingResponseFormat = WebMessageFormat.Json,
                    HelpEnabled = true
                };
                svc.Behaviors.Add(behavior);
                return new WSDLCallback(svc);
            }
            catch (Exception e)
            {
                Logger.ErrorException("Unable to create WSDLCallbackClient", e);
                throw;
            }

        }
    }
}