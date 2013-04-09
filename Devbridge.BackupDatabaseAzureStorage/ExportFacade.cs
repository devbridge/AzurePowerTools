using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Devbridge.BackupDatabaseAzureStorage.Properties;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Devbridge.BackupDatabaseAzureStorage
{
    public class ExportFacade
    {      
        private readonly ILog logger = LogManager.GetCurrentClassLogger();        

        public void Export(string databaseName)
        {
            ImportExportHelper ieHelper = new ImportExportHelper();

            //Set Inputs to the REST Requests in the helper class         
            ieHelper.EndPointUri = Properties.Settings.Default.DACWebServiceUrl;
            ieHelper.ServerName = Properties.Settings.Default.DatabaseServerName;
            ieHelper.StorageKey = Properties.Settings.Default.StorageAccessKey;
            ieHelper.DatabaseName = databaseName;
            ieHelper.UserName = Properties.Settings.Default.DatabaseUserName;
            ieHelper.Password = Properties.Settings.Default.DatabaseUserPassword;

            //Do Export operation
            string exportBlobPath = ieHelper.DoExport(
                String.Format(
                    Properties.Settings.Default.ExportFileNameFormat,
                    ieHelper.DatabaseName,
                    DateTime.Now,
                    Properties.Settings.Default.StorageName,
                    Properties.Settings.Default.BackupsContainer
                    ));
        }      

        public void ExportDatabases(IList<string> allDatabases)
        {
            foreach (var dbName in allDatabases)
            {
                logger.InfoFormat("Export of database '{0}' started", dbName);
                try
                {
                    Export(dbName);
                    logger.InfoFormat("Export of database '{0}' finished succesfully", dbName);
                }
                catch (Exception ex)
                {
                    logger.ErrorFormat("Error occured while exporting database '{0}'", ex, dbName);
                }
            }
        }

        public void CleanupOlderBackups()
        {
            logger.Info("CleanupOlderBackups started");
            var storageAccount = new CloudStorageAccount(
                new StorageCredentials(Properties.Settings.Default.StorageName, Properties.Settings.Default.StorageAccessKey),
                true);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(Properties.Settings.Default.BackupsContainer);
            var filesToKeep = Settings.Default.BackupFilesToKeep;
            try
            {
                var databaseDirs = container.ListBlobs(null, false);
                foreach (IListBlobItem item in databaseDirs)
                {
                    if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                        CloudBlobDirectory directory = (CloudBlobDirectory)item;
                        var databaseBackups = directory.ListBlobs();

                        var filesToDelete = databaseBackups.Where(b=>b is CloudBlockBlob).Cast<CloudBlockBlob>()
                            .OrderByDescending(fi => fi.Properties.LastModified).Skip(filesToKeep);
                        foreach (var fileToDelete in filesToDelete)
                        {
                            try
                            {
                                fileToDelete.Delete();
                            }
                            catch (Exception ex)
                            {
                                logger.ErrorFormat("Error occured while deleting file '{0}'", ex, fileToDelete.Uri);
                            }
                        }                       
                    }
                }

                //var backupDir = Settings.Default.BackupsDir;
                //var filesToKeep = Settings.Default.BackupFilesToKeep;

                //var dbSubfolders = Directory.GetDirectories(backupDir);
                //foreach (var dbDir in dbSubfolders)
                //{
                //    var di = new DirectoryInfo(dbDir);
                //    var backupFileInfos = di.GetFileSystemInfos("*." + BackupFileExtension);
                //    if (backupFileInfos.Length > filesToKeep)
                //    {
                //        var filesToDelete = backupFileInfos.OrderByDescending(fi => fi.CreationTime).Skip(filesToKeep);
                //        foreach (var fileToDelete in filesToDelete)
                //        {
                //            try
                //            {
                //                File.Delete(fileToDelete.FullName);
                //            }
                //            catch (Exception ex)
                //            {
                //                logger.ErrorFormat("Error occured while deleting file '{0}'", ex, fileToDelete.FullName);
                //            }
                //        }
                //    }
                //}

                logger.Info("CleanupOlderBackups finished");
            }
            catch (Exception ex)
            {
                logger.Error("Error occured in CleanupOlderBackups", ex);
                throw ex;
            }
        }      
    }
}
