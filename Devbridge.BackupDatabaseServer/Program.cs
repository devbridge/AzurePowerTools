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
            if (Settings.Default.BackupedDatabases != null && Settings.Default.BackupedDatabases.Count > 0)
            {
                exportFacade.ExportDatabases(Settings.Default.BackupedDatabases.Cast<string>().ToList());
            }
            else
            {
                exportFacade.ExportAllDatabases();
            }

            exportFacade.CleanupOlderBackups();

          
        }
    }
}
