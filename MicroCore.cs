﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAPbouiCOM;
using SAPbobsCOM;
using System.IO;
using AddOne.Framework.Service;
using Castle.Core.Logging;
using AddOne.Framework.Model;
using AddOne.Framework.Factory;

namespace AddOne.Framework
{
    public class MicroCore
    {
        private SAPbobsCOM.Company company;
        private DatabaseConfiguration dbConf;
        private AssemblyManager assemblyLoader;
        private MicroCoreEventDispatcher dispatcher;
        private MicroBoot microBoot;

        public ILogger Logger { get; set; }

        public MicroCore(DatabaseConfiguration dbConf, SAPbobsCOM.Company company, AssemblyManager assemblyLoader,
            MicroCoreEventDispatcher dispatcher, MicroBoot microBoot)
        {
            this.microBoot = microBoot;
            this.company = company;
            this.dbConf = dbConf;
            this.assemblyLoader = assemblyLoader;
            this.dispatcher = dispatcher;
        }

        public void PrepareFramework()
        {
            try
            {
                Logger.Debug(Messages.PreparingFramework);
                dbConf.PrepareDatabase();

                if (InsideInception())
                    return;

                string appFolder = CheckAppFolder();
                Logger.Debug(String.Format(Messages.CreatedAppFolder, appFolder));

                assemblyLoader.UpdateAssemblies(AssemblySource.Core, appFolder);
                assemblyLoader.UpdateAssemblies(AssemblySource.AddIn, appFolder);
                CopyInstallResources(appFolder, Environment.CurrentDirectory);

                dispatcher.RegisterEvents();

                microBoot.AppFolder = appFolder;
                microBoot.StartInception();
                dispatcher.RegisterInception(microBoot.Inception);
                microBoot.Boot();
                System.Windows.Forms.Application.Run();
            }
            catch (Exception e)
            {
                Logger.Fatal(String.Format(Messages.GeneralError, e.Message), e);
                Environment.Exit(10);
            }
        }

        private void CopyInstallResources(string appFolder, string sourceFolder)
        {
            string source, destination;
            source = Path.Combine(sourceFolder, "AddOneInception.config");
            destination = Path.Combine(appFolder, "AddOne.config");
            if (!File.Exists(destination) && File.Exists(source))
            {
                File.Copy(source, destination);
            }

            source = Path.Combine(sourceFolder, "gweadded.jpg");
            destination = Path.Combine(appFolder, "gweadded.jpg");
            if (!File.Exists(destination) && File.Exists(source))
            {
                File.Copy(source, destination);
            }

            source = Path.Combine(sourceFolder, "AddOneAddin.config");
            destination = Path.Combine(appFolder, "AddOneAddin.config");
            if (!File.Exists(destination) && File.Exists(source))
            {
                File.Copy(source, destination);
            }

        }

        private string CheckAppFolder()
        {
            string appFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\AddOne";
            CreateIfNotExists(appFolder);
            appFolder = Path.Combine(appFolder, company.Server + "-" + company.CompanyDB);
            CreateIfNotExists(appFolder);
            return appFolder;
        }

        private void CreateIfNotExists(string appFolder)
        {
            if (System.IO.Directory.Exists(appFolder) == false)
            {
                System.IO.Directory.CreateDirectory(appFolder);
            }
        }

        private bool InsideInception()
        {
            return AppDomain.CurrentDomain.FriendlyName == "AddOne.Inception"
                || AppDomain.CurrentDomain.FriendlyName == "AddOne.AddIn";
        }
    }
}
