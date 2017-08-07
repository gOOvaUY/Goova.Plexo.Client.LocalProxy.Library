using System;
using System.Threading.Tasks;
using Plexo.Client.LocalProxy.Library.JsonSerializer;
using Plexo.Client.LocalProxy.Library.Logging;
using RestSharp;

// ReSharper disable InconsistentNaming

namespace Plexo.Client.LocalProxy.Library
{
    internal class Callback : ICallback //This will be loaded/injected by reflection
    {
        private readonly WSDLCallback _wsdlclient;
        private readonly RestClient _restclient;

        private static readonly ILog Logger = LogProvider.GetCurrentClassLogger();


        public Callback()
        {
            if (Properties.Settings.Default.CallbackType == 0) //webservice based callback
            {
                _wsdlclient = WSDLCallback.Create();
                _restclient = null;
            }
            else //Post based callback
            {
                _wsdlclient = null;
                _restclient=NewtonsoftJsonSerializer.CreateClient(Properties.Settings.Default.CallbackUrl);

            }

        }

        public async Task<ClientResponse> Instrument(IntrumentCallback instrument)
        {
            try
            {

                if (_wsdlclient != null)
                {
                    return await _wsdlclient.Instrument(instrument);
                }
                if (_restclient != null)
                {
                    RestRequest req = new RestRequest(Method.POST);
                    req.SetJsonContent(instrument);
                    IRestResponse<ClientResponse> resp = await _restclient.ExecuteTaskAsync<ClientResponse>(req);
                    if (resp.Data == null)
                        return new ClientResponse {ErrorMessage = "Error executing callback: " + (resp.ErrorMessage ?? "Unknown Error"), ResultCode = ResultCodes.ClientServerError};
                    return resp.Data;
                }
                return new ClientResponse {ErrorMessage = "Error executing callback, callback is not configured properly", ResultCode = ResultCodes.SystemError};
            }
            catch (Exception e)
            {
                Logger.ErrorException("Error executing callback", e);
                return new ClientResponse { ErrorMessage = "Internal Error executing callback", ResultCode = ResultCodes.SystemError };
            }
        }

        public async Task<ClientResponse> Payment(TransactionCallback transaction)
        {
            try
            {

                if (_wsdlclient != null)
                {
                    return await _wsdlclient.Payment(transaction);
                }
                if (_restclient != null)
                {
                    RestRequest req = new RestRequest(Method.POST);
                    req.SetJsonContent(transaction);
                    IRestResponse<ClientResponse> resp = await _restclient.ExecuteTaskAsync<ClientResponse>(req);
                    if (resp.Data == null)
                        return new ClientResponse { ErrorMessage = "Error executing callback: " + (resp.ErrorMessage ?? "Unknown Error"), ResultCode = ResultCodes.ClientServerError };
                    return resp.Data;
                }
                return new ClientResponse { ErrorMessage = "Error executing callback, callback is not configured properly", ResultCode = ResultCodes.SystemError };
            }
            catch (Exception e)
            {
                Logger.ErrorException("Error executing callback", e);
                return new ClientResponse { ErrorMessage = "Internal Error executing callback", ResultCode = ResultCodes.SystemError };
            }
        }
    }
}