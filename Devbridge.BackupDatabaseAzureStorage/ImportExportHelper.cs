using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Web;
using System.Xml;
using Common.Logging;
using Devbridge.BackupDatabaseAzureStorage.DACWebService;

namespace Devbridge.BackupDatabaseAzureStorage
{
    public class ImportExportHelper
    {
        private readonly ILog logger = LogManager.GetCurrentClassLogger(); 

        public string EndPointUri { get; set; }
        public string StorageKey { get; set; }
        public string BlobUri { get; set; }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public ImportExportHelper()
        {
            EndPointUri = "";
            ServerName = "";
            StorageKey = "";
            DatabaseName = "";
            UserName = "";
            Password = "";
        }

        public string DoExport(string blobUri)
        {
            logger.Info(String.Format("Starting Export Operation - {0}\n\r", DateTime.Now));
            string requestGuid = null;
            bool exportComplete = false;
            string exportedBlobPath = null;

            //Setup Web Request for Export Operation
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + @"/Export");
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = @"application/xml";

            //Create Web Request Inputs - Blob Storage Credentials and Server Connection Info
            ExportInput exportInputs = new ExportInput
            {
                BlobCredentials = new BlobStorageAccessKeyCredentials
                {
                    StorageAccessKey = this.StorageKey,
                    Uri = String.Format(blobUri, this.DatabaseName, DateTime.UtcNow.Ticks.ToString())
                },
                ConnectionInfo = new ConnectionInfo
                {
                    ServerName = this.ServerName,
                    DatabaseName = this.DatabaseName,
                    UserName = this.UserName,
                    Password = this.Password
                }
            };

            //Perform Web Request
            logger.Info("Making Web Request For Export Operation...");
            Stream webRequestStream = webRequest.GetRequestStream();
            DataContractSerializer dataContractSerializer = new DataContractSerializer(exportInputs.GetType());
            dataContractSerializer.WriteObject(webRequestStream, exportInputs);
            webRequestStream.Close();

            //Get Response and Extract Request Identifier
            logger.Info("Reading Response and extracting Export Request Identifier...");
            WebResponse webResponse = null;
            XmlReader xmlStreamReader = null;

            try
            {
                //Initialize the WebResponse to the response from the WebRequest
                webResponse = webRequest.GetResponse();

                xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
                xmlStreamReader.ReadToFollowing("guid");
                requestGuid = xmlStreamReader.ReadElementContentAsString();
                logger.Info(String.Format("Your Export Request Guid is: {0}", requestGuid));

                //Get Export Operation Status
                while (!exportComplete)
                {
                    logger.Info("Checking export status...");
                    List<StatusInfo> statusInfoList = CheckRequestStatus(requestGuid);
                    StatusInfo statusInfo = statusInfoList.FirstOrDefault();
                    logger.Info(statusInfo.Status);

                    if (statusInfo.Status == "Failed")
                    {
                        logger.Info(String.Format("Database export failed: {0}", statusInfo.ErrorMessage));
                        exportComplete = true;
                    }

                    if (statusInfo.Status == "Completed")
                    {
                        exportedBlobPath = statusInfo.BlobUri;
                        logger.Info(String.Format("Export Complete - Database exported to: {0}\n\r", exportedBlobPath));
                        exportComplete = true;
                    }

                    if (!exportComplete)
                    {
                        Thread.Sleep(3000);
                    }
                }

                return exportedBlobPath;
            }
            catch (WebException responseException)
            {
                logger.ErrorFormat("Request Falied:{0}", responseException,responseException.Message);
                if (responseException.Response != null)
                {
                    logger.ErrorFormat("Status Code: {0}", ((HttpWebResponse)responseException.Response).StatusCode);
                    logger.ErrorFormat("Status Description: {0}\n\r", ((HttpWebResponse)responseException.Response).StatusDescription);
                }
                return null;
            }
        }

        public bool DoImport(string blobUri)
        {
            logger.Info(String.Format("Starting Import Operation - {0}\n\r", DateTime.Now));
            string requestGuid = null;
            bool importComplete = false;

            //Setup Web Request for Import Operation
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + @"/Import");
            webRequest.Method = WebRequestMethods.Http.Post;
            webRequest.ContentType = @"application/xml";

            //Create Web Request Inputs - Database Size & Edition, Blob Store Credentials and Server Connection Info
            ImportInput importInputs = new ImportInput
            {
                AzureEdition = "Web",
                DatabaseSizeInGB = 1,
                BlobCredentials = new BlobStorageAccessKeyCredentials
                {
                    StorageAccessKey = this.StorageKey,
                    Uri = String.Format(blobUri, this.DatabaseName, DateTime.UtcNow.Ticks.ToString())
                },
                ConnectionInfo = new ConnectionInfo
                {
                    ServerName = this.ServerName,
                    DatabaseName = this.DatabaseName,
                    UserName = this.UserName,
                    Password = this.Password
                }
            };

            //Perform Web Request
            logger.Info("Making Web Request for Import Operation...");
            Stream webRequestStream = webRequest.GetRequestStream();
            DataContractSerializer dataContractSerializer = new DataContractSerializer(importInputs.GetType());
            dataContractSerializer.WriteObject(webRequestStream, importInputs);
            webRequestStream.Close();

            //Get Response and Extract Request Identifier
            logger.Info("Serializing response and extracting guid...");
            WebResponse webResponse = null;
            XmlReader xmlStreamReader = null;

            try
            {
                //Initialize the WebResponse to the response from the WebRequest
                webResponse = webRequest.GetResponse();

                xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
                xmlStreamReader.ReadToFollowing("guid");
                requestGuid = xmlStreamReader.ReadElementContentAsString();
                logger.Info(String.Format("Request Guid: {0}", requestGuid));

                //Get Status of Import Operation
                while (!importComplete)
                {
                    logger.Info("Checking status of Import...");
                    List<StatusInfo> statusInfoList = CheckRequestStatus(requestGuid);
                    StatusInfo statusInfo = statusInfoList.FirstOrDefault();
                    logger.Info(statusInfo.Status);

                    if (statusInfo.Status == "Failed")
                    {
                        logger.Info(String.Format("Database import failed: {0}", statusInfo.ErrorMessage));
                        importComplete = true;
                    }

                    if (statusInfo.Status == "Completed")
                    {
                        logger.Info(String.Format("Import Complete - Database imported to: {0}\n\r", statusInfo.DatabaseName));
                        importComplete = true;
                    }
                }

                return importComplete;
            }
            catch (WebException responseException)
            {
                logger.ErrorFormat("Request Falied: {0}",responseException, responseException.Message);
                {
                    logger.ErrorFormat("Status Code: {0}", ((HttpWebResponse)responseException.Response).StatusCode);
                    logger.ErrorFormat("Status Description: {0}\n\r", ((HttpWebResponse)responseException.Response).StatusDescription);
                }

                return importComplete;
            }
        }

        public List<StatusInfo> CheckRequestStatus(string requestGuid)
        {
            WebRequest webRequest = WebRequest.Create(this.EndPointUri + string.Format("/Status?servername={0}&username={1}&password={2}&reqId={3}",
                    HttpUtility.UrlEncode(this.ServerName),
                    HttpUtility.UrlEncode(this.UserName),
                    HttpUtility.UrlEncode(this.Password),
                    HttpUtility.UrlEncode(requestGuid)));

            webRequest.Method = WebRequestMethods.Http.Get;
            webRequest.ContentType = @"application/xml";
            WebResponse webResponse = webRequest.GetResponse();
            XmlReader xmlStreamReader = XmlReader.Create(webResponse.GetResponseStream());
            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(List<StatusInfo>));

            return (List<StatusInfo>)dataContractSerializer.ReadObject(xmlStreamReader, true);
        }
    }
}
