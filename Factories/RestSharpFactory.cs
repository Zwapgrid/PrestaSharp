using Bukimedia.PrestaSharp.Deserializers;
using Bukimedia.PrestaSharp.Lib;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using RestSharp.Authenticators;

namespace Bukimedia.PrestaSharp.Factories
{
    public abstract class RestSharpFactory
    {
        protected string BaseUrl{get;set;}
        protected string Account{get;set;}
        protected string Password{get;set;}

        protected RestSharpFactory(string BaseUrl, string Account, string Password)
        {
            this.BaseUrl = BaseUrl;
            this.Account = Account;
            this.Password = Password;
        }

        protected virtual RestClient GetRestClient(RestRequest request, bool addHandlerforGet = true)
        {
            var client = new RestClient();
            client.ClearHandlers();
            client.AddHandler("text/html", new PrestaSharpTextErrorDeserializer());
            client.BaseUrl = new Uri(this.BaseUrl);
            //client.Authenticator = new HttpBasicAuthenticator(this.Account, this.Password);
            request.AddParameter("ws_key", this.Account, ParameterType.QueryString); // used on every request
            if (addHandlerforGet && request.Method == Method.GET)
            {
                client.AddHandler("text/xml", new Bukimedia.PrestaSharp.Deserializers.PrestaSharpDeserializer());
            }
            return client;
        }

        protected virtual void ProcessResponseErrors(IRestRequest request, IRestResponse response)
        {
            if (response.IsSuccessful)
                return;
            else
            {
                var exception = CreateException(request, response);
                throw exception;
            }
        }

        protected virtual Exception CreateException(IRestRequest request, IRestResponse response)
        {
            string RequestParameters = Environment.NewLine;
            foreach (RestSharp.Parameter Parameter in request.Parameters)
            {
                RequestParameters += Parameter.Value + Environment.NewLine + Environment.NewLine;
            }
            return new PrestaSharpException(RequestParameters + response.Content, response.ErrorMessage, response.StatusCode, response.ErrorException);
        }

        protected T Execute<T>(RestRequest request) where T : new()
        {
            var client = GetRestClient(request);

            var response = client.Execute<T>(request);

            ProcessResponseErrors(request, response);

            return response.Data;
        }

        protected async Task<T> ExecuteAsync<T>(RestRequest request) where T : new()
        {
            var client = GetRestClient(request);

            var response = await client.ExecuteTaskAsync<T>(request);

            ProcessResponseErrors(request, response);

            return response.Data;
        }

        protected List<long> ExecuteForGetIds<T>(RestRequest request, string RootElement) where T : new()
        {
            var client = GetRestClient(request, false);

            var response = client.Execute<T>(request);

            ProcessResponseErrors(request, response);

            return GetIdsFromContent(response.Content, RootElement);
        }

        protected async Task<List<long>> ExecuteForGetIdsAsync<T>(RestRequest request, string RootElement) where T : new()
        {
            var client = GetRestClient(request, false);

            var response = await client.ExecuteTaskAsync<T>(request);

            ProcessResponseErrors(request, response);

            return GetIdsFromContent(response.Content, RootElement);
        }

        private List<long> GetIdsFromContent(string content, string rootElement)
        {
            XDocument xDcoument = XDocument.Parse(content);
            var ids = (from doc in xDcoument.Descendants(rootElement)
                select long.Parse(doc.Attribute("id").Value)).ToList();
            return ids;
        }

        protected byte[] ExecuteForImage(RestRequest request)
        {
            var client = GetRestClient(request, false);

            var response = client.Execute(request);

            ProcessResponseErrors(request, response);

            return response.RawBytes;
        }

        protected RestRequest RequestForGet(string Resource, string Id, string RootElement)
        {
            var request = new RestRequest();
            request.Resource = Resource + "/" + Id;
            request.RootElement = RootElement;
            return request;
        }

        protected RestRequest RequestForAdd(string Resource, List<Entities.PrestaShopEntity> Entities)
        {
            var request = new RestRequest();
            request.Resource = Resource;
            request.Method = Method.POST;
            request.RequestFormat = DataFormat.Xml;
            //Hack implementation in PrestaSharpSerializer to serialize PrestaSharp.Entities.AuxEntities.language
            request.XmlSerializer = new Serializers.PrestaSharpSerializer();
            string serialized = "";
            foreach (Entities.PrestaShopEntity Entity in Entities)
            {
                serialized += ((Serializers.PrestaSharpSerializer)request.XmlSerializer).PrestaSharpSerialize(Entity);
            }
            serialized = "<prestashop>\n" + serialized + "\n</prestashop>";
            request.AddParameter("application/xml", serialized, ParameterType.RequestBody);
            return request;
        }

        /// <summary>
        /// More information about image management: http://doc.prestashop.com/display/PS15/Chapter+9+-+Image+management
        /// </summary>
        /// <param name="Resource"></param>
        /// <param name="Id"></param>
        /// <param name="ImagePath"></param>
        /// <returns></returns>
        protected RestRequest RequestForAddImage(string Resource, long? Id, string ImagePath)
        {
            if (Id == null)
            {
                throw new ApplicationException("The Id field cannot be null.");
            }
            var request = new RestRequest();
            request.Resource = "/images/" + Resource + "/" + Id;
            request.Method = Method.POST;
            request.RequestFormat = DataFormat.Xml;
            request.AddFile("image", ImagePath);
            return request;
        }
        
        /// <summary>
        /// More information about image management: http://doc.prestashop.com/display/PS15/Chapter+9+-+Image+management
        /// </summary>
        /// <param name="Resource"></param>
        /// <param name="Id"></param>
        /// <param name="Image"></param>
        /// <returns></returns>
        protected RestRequest RequestForAddImage(string Resource, long? Id, byte[] Image)
        {
            if (Id == null)
            {
                throw new ApplicationException("The Id field cannot be null.");
            }
            var request = new RestRequest();
            request.Resource = "/images/" + Resource + "/" + Id;
            request.Method = Method.POST;
            request.RequestFormat = DataFormat.Xml;
            request.AddFile("image", Image, "dummy.png");
            return request;
        }

        /// <summary>
        /// More information about image management: http://doc.prestashop.com/display/PS15/Chapter+9+-+Image+management
        /// </summary>
        /// <param name="Resource"></param>
        /// <param name="Id"></param>
        /// <param name="ImagePath"></param>
        /// <returns></returns>
        protected RestRequest RequestForUpdateImage(string Resource, long Id, string ImagePath)
        {
            var request = new RestRequest();
            request.Resource = "/images/" + Resource + "/" + Id;

            // BUG

            request.Method = Method.PUT;
            request.RequestFormat = DataFormat.Xml;
            request.AddFile("image", ImagePath);
            return request;
        }

        protected RestRequest RequestForUpdate(string Resource, long? Id, Entities.PrestaShopEntity PrestashopEntity)
        {
            if (Id == null)
            {
                throw new ApplicationException("Id is required to update something.");
            }
            var request = new RestRequest();
            request.RootElement = "prestashop";
            request.Resource = Resource;
            request.AddParameter("id", Id, ParameterType.UrlSegment);
            request.Method = Method.PUT;
            request.RequestFormat = DataFormat.Xml;
            request.XmlSerializer = new RestSharp.Serializers.DotNetXmlSerializer();
            request.AddBody(PrestashopEntity);
            //issue #81, #54 fixed
            request.Parameters[1].Value = Functions.ReplaceFirstOccurrence(request.Parameters[1].Value.ToString(), "<" + PrestashopEntity.GetType().Name + ">", "<prestashop>\n<" + PrestashopEntity.GetType().Name + ">");
            request.Parameters[1].Value = Functions.ReplaceLastOccurrence(request.Parameters[1].Value.ToString(), "</" + PrestashopEntity.GetType().Name + ">", "</" + PrestashopEntity.GetType().Name + ">\n</prestashop>");
            //issue #36 fixed
            request.Parameters[1].Value = request.Parameters[1].Value.ToString().Replace(" xmlns=\"Bukimedia/PrestaSharp/Entities\"", "");// "xmlns=\"\"");
            request.Parameters[1].Value = request.Parameters[1].Value.ToString().Replace(" xmlns=\"Bukimedia/PrestaSharp/Entities/AuxEntities\"", "");// "xmlns=\"\"");
            return request;
        }
       // For Update List Of Products - start
        protected RestRequest RequestForUpdateList(string Resource, List<Entities.PrestaShopEntity> Entities)
        {
            var request = new RestRequest();
            request.Resource = Resource;
            request.Method = Method.PUT;
            request.RequestFormat = DataFormat.Xml;
            request.XmlSerializer = new Serializers.PrestaSharpSerializer();
            string serialized = "";
            foreach (Entities.PrestaShopEntity Entity in Entities)
            {
                serialized += ((Serializers.PrestaSharpSerializer)request.XmlSerializer).PrestaSharpSerialize(Entity);
            }
            serialized = "<prestashop>\n" + serialized + "\n</prestashop>";
            request.AddParameter("application/xml", serialized, ParameterType.RequestBody);
            return request;
        }
        // For Update List Of Products - end
        protected RestRequest RequestForDeleteImage(string Resource, long? ResourceId, long? ImageId)
        {
            if (ResourceId == null)
            {
                throw new ApplicationException("Id is required to delete something.");
            }
            var request = new RestRequest();
            request.RootElement = "prestashop";
            request.Resource = "/images/" + Resource + "/" + ResourceId;
            if (ImageId != null)
            {
                request.Resource += "/" + ImageId;
            }
            request.Method = Method.DELETE;
            request.RequestFormat = DataFormat.Xml;
            return request;
        }

        protected RestRequest RequestForDelete(string Resource, long? Id)
        {
            if (Id == null)
            {
                throw new ApplicationException("Id is required to delete something.");
            }
            var request = new RestRequest();
            request.RootElement = "prestashop";
            request.Resource = Resource + "/" + Id;
            request.Method = Method.DELETE;
            request.RequestFormat = DataFormat.Xml;
            return request;
        }

        /// <summary>
        /// More information about filtering: http://doc.prestashop.com/display/PS14/Chapter+8+-+Advanced+Use
        /// </summary>
        /// <param name="Resource"></param>
        /// <param name="Display"></param>
        /// <param name="Filter"></param>
        /// <param name="Sort"></param>
        /// <param name="Limit"></param>
        /// <param name="RootElement"></param>
        /// <returns></returns>
        protected RestRequest RequestForFilter(string Resource, string Display, Dictionary<string,string> Filter = null, string Sort = null, string Limit = null, string RootElement = null)
        {
            var request = new RestRequest();
            request.Resource = Resource;
            request.RootElement = RootElement;
            if (Display != null)
            {
                request.AddParameter("display", Display);
            }
            if (Filter != null)
            {
                foreach (string Key in Filter.Keys)
                {
                    request.AddParameter("filter[" + Key + "]", Filter[Key]);
                }
            }
            if (!string.IsNullOrEmpty(Sort))
            {
                request.AddParameter("sort", Sort);
            }
            if (!string.IsNullOrEmpty(Limit))
            {
                request.AddParameter("limit", Limit);
            }
            // Support for filter by date range
            request.AddParameter("date", "1");
            return request;
        }

        protected RestRequest RequestForAddOrderHistory(string Resource, List<Entities.PrestaShopEntity> Entities)
        {
            var request = new RestRequest();
            request.Resource = Resource;
            request.Method = Method.POST;
            request.RequestFormat = DataFormat.Xml;
            request.XmlSerializer = new Serializers.PrestaSharpSerializer();
            string serialized = "";
            foreach (Entities.PrestaShopEntity Entity in Entities)
            {
                serialized += ((Serializers.PrestaSharpSerializer)request.XmlSerializer).PrestaSharpSerialize(Entity);
            }
            serialized = "<prestashop>\n" + serialized + "\n</prestashop>";
            request.AddParameter("application/xml", serialized);
            request.AddParameter("sendemail", 1);
            return request;
        }

        public static byte[] ImageToBinary(string imagePath)
        {
            FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[fileStream.Length];
            fileStream.Read(buffer, 0, (int)fileStream.Length);
            fileStream.Close();
            return buffer;
        }
    }
}
