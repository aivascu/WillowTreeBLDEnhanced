using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Autofac;
using NLog;
using WillowTree.Common;
using WillowTree.Presenters;
using WillowTree.Services.DataAccess;

namespace WillowTree
{
    internal static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main()
        {
            try
            {
                logger.Info("Starting application...");
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                Application.ThreadException += ApplicationThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                WillowSaveGameSerializer.SetKVFilePath(Path.Combine(Constants.DataPath, "KV.bin"));
                GameData.Initialize(Constants.DataPath);

                var builder = new ContainerBuilder();
                builder.RegisterModule<FileSystemModule>();
                builder.RegisterModule<ServicesModule>();
                builder.RegisterModule<PresentationModule>();
                var container = builder.Build();

                var aboutPresenter = container.Resolve<AboutViewPresenter>();
                var mainForm = container.Resolve<WillowTreeMain>();

                Application.Run(mainForm);
            }
            catch (Exception e)
            {
                logger.Error(e, "Application failed while starting");
                throw;
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error("Current domain exception {0}", e.ExceptionObject);
        }

        private static void ApplicationThreadException(object sender, ThreadExceptionEventArgs e)
        {
            logger.Error(e.Exception, "Application thread exception");
        }
    }
}
