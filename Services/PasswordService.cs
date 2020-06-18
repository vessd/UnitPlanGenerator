using Meziantou.Framework.Win32;
using System;
using System.Linq;
using System.Reflection;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class PasswordService : IPasswordService
    {
        private readonly string _appName;

        public PasswordService()
        {
            _appName = Assembly
                .GetEntryAssembly()
                .GetCustomAttributes(typeof(AssemblyProductAttribute))
                .OfType<AssemblyProductAttribute>()
                .FirstOrDefault()
                .Product;
        }

        public void RemoveAll()
        {
            try
            {
                CredentialManager.DeleteCredential(_appName);
            }
            catch (System.ComponentModel.Win32Exception) { }
        }

        public void SavePassword(string userName, string password)
        {
            CredentialManager.WriteCredential(_appName, userName, password, CredentialPersistence.LocalMachine);
        }

        public bool TryGetPassword(string userName, out string password)
        {
            password = null;
            if(CredentialManager.ReadCredential(_appName) is Credential credential)
            {
                if (credential.UserName == userName)
                {
                    password = credential.Password;
                    return true;
                }
            }

            return false;
        }
    }
}
