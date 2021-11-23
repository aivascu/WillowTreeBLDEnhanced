﻿using NLog;
using System;
using System.Threading;
using System.Windows.Forms;
using System.IO.Abstractions;

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


                FileSystem fileSystem = new FileSystem();
                Application.Run(new WillowTreeMain(new FileWrapper(fileSystem), new DirectoryWrapper(fileSystem)));
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
