using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Devbridge.BackupDatabaseServer.Properties;

namespace Devbridge.BackupDatabaseServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var exportFacade = new ExportFacade(Settings.Default.ConnectionString);
            exportFacade.ExportAllDatabases();
            exportFacade.CleanupOlderBackups();
        }
    }
}
