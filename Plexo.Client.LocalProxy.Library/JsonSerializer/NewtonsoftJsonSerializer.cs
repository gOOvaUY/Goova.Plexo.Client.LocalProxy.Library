using Newtonsoft.Json;
using RestSharp.Serializers;
using System.IO;
using RestSharp;
using RestSharp.Deserializers;

namespace Plexo.Client.LocalProxy.Library.JsonSerializer
{
    public class NewtonsoftJsonSerializer : ISerializer, IDeserializer
    {
        private Newtonsoft.Json.JsonSerializer serializer;

        public static RestClient CreateClient(string baseUrl)
        {
            var client = new RestClient(baseUrl);
            NewtonsoftJsonSerializer def= new NewtonsoftJsonSerializer(new Newtonsoft.Json.JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
            client.AddHandler("application/json", def);
            client.AddHandler("text/json", def);
            client.AddHandler("text/x-json", def);
            client.AddHandler("text/javascript", def);
            client.AddHandler("*+json", def);
            return client;
        }
        public NewtonsoftJsonSerializer(Newtonsoft.Json.JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        public string ContentType
        {
            get { return "application/json"; } // Probably used for Serialization?
            set { }
        }

        public string DateFormat { get; set; }

        public string Namespace { get; set; }

        public string RootElement { get; set; }

        public string Serialize(object obj)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var jsonTextWriter = new JsonTextWriter(stringWriter))
                {
                    serializer.Serialize(jsonTextWriter, obj);

                    return stringWriter.ToString();
                }
            }
        }

        public T Deserialize<T>(RestSharp.IRestResponse response)
        {
            var content = response.Content;

            using (var stringReader = new StringReader(content))
            {
                using (var jsonTextReader = new JsonTextReader(stringReader))
                {
                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
        }


    }
    public static class Extensions
    {
        public static void SetJsonContent(this RestRequest request, object obj)
        {
            request.RequestFormat = DataFormat.Json;
            request.JsonSerializer = new NewtonsoftJsonSerializer(new Newtonsoft.Json.JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
            });
            request.AddJsonBody(obj);
        }
    }
}
