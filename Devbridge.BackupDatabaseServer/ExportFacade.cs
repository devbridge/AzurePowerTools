using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Devbridge.BackupDatabaseServer.Properties;
using Microsoft.SqlServer.Dac;

namespace Devbridge.BackupDatabaseServer
{
    public class ExportFacade
    {
        private SqlConnection connection;
        private string connectionString;
        private readonly ILog logger = LogManager.GetCurrentClassLogger();
        private const string BackupFileExtension = "bacpac";

        public ExportFacade(string connectionString)
        {
            this.connectionString = connectionString;
            this.connection = new SqlConnection(connectionString);
        }

        public void Export(string databaseName, string targetFilename)
        {
            DacServices dacServices = new DacServices(connectionString);

            dacServices.ExportBacpac(targetFilename, databaseName);
        }

        public void ExportAllDatabases()
        {
            logger.Info("ExportAllDatabases started");
            try
            {
                IList<string> allDatabases = null;
                OpenConnection();
                try
                {
                    allDatabases = GetServerDatabases();
                }
                finally
                {
                    CloseConnection();
                }

                allDatabases = allDatabases.Where(db => db == "TestBackup").ToList();

                foreach (var dbName in allDatabases)
                {
                    string backupDir = Path.Combine(Settings.Default.BackupsDir, dbName);
                    if (!Directory.Exists(backupDir))
                    {
                        Directory.CreateDirectory(backupDir);
                    }

                    string fileName = Path.Combine(backupDir, string.Format(Settings.Default.FileNameFormat, dbName, DateTime.Now) + "." + BackupFileExtension);

                    logger.InfoFormat("Export of database '{0}' started", dbName);
                    try
                    {
                        Export(dbName, fileName);
                        logger.InfoFormat("Export of database '{0}' finished succesfully", dbName);
                    }
                    catch (Exception ex)
                    {
                        logger.ErrorFormat("Error occured while exporting database '{0}'", ex, dbName);
                    }
                }

                logger.Info("ExportAllDatabases finished");
            }
            catch (Exception ex)
            {
                logger.Error("Error occured in ExportAllDatabases", ex);
                throw ex;
            }
        }

        public void CleanupOlderBackups()
        {
            logger.Info("CleanupOlderBackups started");
            try
            {
                var backupDir = Settings.Default.BackupsDir;
                var filesToKeep = Settings.Default.BackupFilesToKeep;

                var dbSubfolders = Directory.GetDirectories(backupDir);
                foreach (var dbDir in dbSubfolders)
                {
                    var di = new DirectoryInfo(dbDir);
                    var backupFileInfos = di.GetFileSystemInfos("*." + BackupFileExtension);
                    if (backupFileInfos.Length > filesToKeep)
                    {
                        var filesToDelete = backupFileInfos.OrderByDescending(fi => fi.CreationTime).Skip(filesToKeep);
                        foreach (var fileToDelete in filesToDelete)
                        {
                            try
                            {
                                File.Delete(fileToDelete.FullName);
                            }
                            catch (Exception ex)
                            {
                                logger.ErrorFormat("Error occured while deleting file '{0}'", ex, fileToDelete.FullName);
                            }
                        }
                    }
                }

                logger.Info("CleanupOlderBackups finished");
            }
            catch (Exception ex)
            {
                logger.Error("Error occured in CleanupOlderBackups", ex);
                throw ex;
            }
        }

        private void OpenConnection()
        {
            this.connection.Open();
        }

        private void CloseConnection()
        {
            this.connection.Close();
        }

        private IList<string> GetServerDatabases()
        {
            SqlDataAdapter adapter = new SqlDataAdapter("select name from sys.databases where state_desc='ONLINE' and name<>'master'", connection);

            DataTable dt = new DataTable();
            adapter.Fill(dt);

            return dt.AsEnumerable().Select(row => row[0] as string).ToList();
        }

        private void BackupDatabase(string sourceDatabaseName, string targetDatabaseName)
        {
            SqlCommand backupCommand = new SqlCommand("CREATE DATABASE @targetDB AS COPY OF @sourceDB", connection);
            backupCommand.Parameters.AddWithValue("@sourceDB", sourceDatabaseName);
            backupCommand.Parameters.AddWithValue("@targetDB", targetDatabaseName);
            backupCommand.ExecuteNonQuery();
        }


        //public bool CheckIfBackupDatabaseFinishedSuccessfully(string databaseName)
        //{
        //    SqlCommand backupCommand = new SqlCommand("SELECT state_desc FROM sys.databases WHERE name = @db", connection);
        //    backupCommand.Parameters.AddWithValue("@db", databaseName);            
        //    backupCommand.ExecuteNonQuery();
        //}
    }
}
