using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devbridge.BackupDatabaseAzureStorage.Properties;

namespace Devbridge.BackupDatabaseAzureStorage
{
    class Program
    {
        static void Main(string[] args)
        {
            var exportFacade = new ExportFacade();
            exportFacade.ExportDatabases(Settings.Default.BackupedDatabases.Cast<string>().ToList());
            exportFacade.CleanupOlderBackups();
        }
    }
}
